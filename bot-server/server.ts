import { Hono } from "hono";
import { callLLM } from "./lib/llm";
import { sendChat, sendPlayWav, sendFollow, sendStop, getHealth, getStatus } from "./lib/game";
import { speak } from "./lib/piper";
import * as rel from "./lib/relationship";
import * as mem from "./lib/memory";
import { build, ensureSelf } from "./lib/prompt";

const app = new Hono();
const PIPER_URL = process.env.PIPER_URL || "http://localhost:9092";
const TTS_LENGTH = parseFloat(process.env.TTS_LENGTH || "0.85");

let _selfPromise: Promise<void> | null = null;

function log(msg: string): void {
  console.log(`[${new Date().toISOString()}] ${msg}`);
}

function ensureSelfAsync(): Promise<void> {
  if (!_selfPromise) {
    if (!mem.getSelf()) {
      _selfPromise = ensureSelf(callLLM).finally(() => { _selfPromise = null; });
    } else {
      _selfPromise = Promise.resolve();
    }
  }
  return _selfPromise;
}

async function processChat(sender: string, message: string): Promise<void> {
  await ensureSelfAsync();
  log(`Chat from ${sender}: "${message}"`);
  rel.processMessage(message);

  const history = mem.getHistory();
  const systemPrompt = await build();
  const llmMsgs = [systemPrompt, ...history.map(h => ({ role: h.role as "user" | "assistant", content: h.content })), { role: "user" as const, content: message }];

  const raw = await callLLM(llmMsgs);
  const facts = raw.match(/\[ФАКТ:\s*([^=]+)=([^\]]+)\]/g);
  let reply = raw;
  if (facts) {
    facts.forEach((f) => {
      const m = f.match(/\[ФАКТ:\s*([^=]+)=([^\]]+)\]/);
      if (m) mem.addFact(m[1].trim(), m[2].trim());
    });
    reply = raw.replace(/\[ФАКТ:[^\]]*\]\s*/g, "").trim();
  }
  mem.addMessage("user", message);
  mem.addMessage("assistant", reply);
  log(`LLM reply: "${reply}" (level=${rel.getLevelName()}, sentiment=${rel.getSentiment()})`);

  sendChat("Путница", reply).then((r) => {
    log(`Reply sent to game: ${JSON.stringify(r)}`);
  }).catch((e: Error) => {
    log(`Reply send error: ${e.message}`);
  });

  speak(reply).then((tts) => {
    log(`TTS generated (${tts?.wav?.length || 0} chars)`);
    if (tts?.wav) {
      sendPlayWav(tts.wav).then((r) => {
        log(`Game play: ${JSON.stringify(r)}`);
      }).catch((e: Error) => {
        log(`Game play error: ${e.message}`);
      });
    }
  }).catch((e: Error) => {
    log(`TTS error: ${e.message}`);
  });
}

app.get("/health", async (c) => {
  try {
    const data = await getHealth();
    return c.json({
      ok: true,
      game: data,
      relationship: { level: rel.getLevelName(), sentiment: rel.getSentiment(), messages: rel.getMessageCount() },
      memory: mem.getFacts().length,
      self: !!mem.getSelf(),
    });
  } catch (err: any) {
    return c.json({ ok: false, game: "disconnected", error: err.message });
  }
});

app.get("/status", async (c) => {
  try {
    const data = await getStatus();
    return c.json({ ok: true, game: data });
  } catch (err: any) {
    return c.json({ ok: false, game: "disconnected", error: err.message });
  }
});

app.post("/chat", async (c) => {
  const body = await c.req.json<{ sender?: string; message?: string }>();
  if (!body.message) {
    return c.json({ error: "message is required" }, 400);
  }
  processChat(body.sender || "?", body.message);
  return c.json({ ok: true, message: "Processing..." });
});

app.post("/event", async (c) => {
  const body = await c.req.json<{
    type?: string; sender?: string; message?: string;
    damage?: number; health?: number; maxHealth?: number;
    killed?: string; killer?: string;
  }>();
  log(`Event: ${body.type || "?"}`);

  if (body.type === "chat" && body.message) {
    processChat(body.sender || "?", body.message);
  }
  if (body.type === "bot_damaged") {
    processChat("System", `Путница получает урон: ${body.damage || "?"} хп, осталось ${body.health || "?"}/${body.maxHealth || "?"}`);
  }
  if (body.type === "player_damaged") {
    processChat("System", `Игрок получает урон: ${body.damage || "?"} хп, осталось ${body.health || "?"}/${body.maxHealth || "?"}`);
  }
  if (body.type === "entity_killed") {
    if (body.killed === "companionBot") {
      log("[Event] Бот убит — очищаю самосознание");
      mem.clearSelf();
      processChat("System", "Путница умирает...");
    } else if (body.killer === "player" && body.killed && body.killed !== "companionBot") {
      processChat("System", `Игрок убил ${body.killed}`);
    }
  }

  return c.json({ ok: true });
});

app.post("/send", async (c) => {
  const body = await c.req.json<{ sender?: string; message?: string }>();
  if (!body.message) {
    return c.json({ error: "message is required" }, 400);
  }
  log(`Send: ${body.sender || "System"} -> "${body.message}"`);
  const data = await sendChat(body.sender || "System", body.message);
  return c.json(data);
});

app.post("/follow", async (c) => {
  const data = await sendFollow();
  return c.json(data);
});

app.post("/stop", async (c) => {
  const data = await sendStop();
  return c.json(data);
});

app.get("/self", (c) => {
  return c.json({ ok: true, self: mem.getSelf() });
});

app.post("/reset", async (c) => {
  mem.resetAll();
  _selfPromise = null;
  log("[Reset] Память очищена — бот пересоздан");
  await ensureSelfAsync();
  return c.json({ ok: true });
});

app.post("/speak", async (c) => {
  const body = await c.req.json<{ text?: string; length_scale?: number }>();
  if (!body.text) {
    return c.json({ error: "text is required" }, 400);
  }
  log(`TTS: "${body.text}"`);
  const tts = await speak(body.text, body.length_scale).catch((e: Error) => ({ error: e.message }));
  return c.json(tts || { error: "no response" });
});

const PORT = parseInt(process.env.PORT || "9091");

console.log(`[Server] Bot proxy running on port ${PORT}`);
console.log(`[Server] Game mod URL: ${process.env.BOT_URL || "http://localhost:9090"}`);
console.log(`[Server] LLM URL: ${process.env.LLM_URL || "http://host.docker.internal:1234"}`);
console.log(`[Server] Piper URL: ${PIPER_URL} (speed: ${TTS_LENGTH})`);
console.log(`[Server] Relationship: ${rel.getLevelName()}, ${rel.getMessageCount()} msgs`);

ensureSelfAsync().then(() => {
  if (mem.getSelf()) {
    console.log(`[Server] Самосознание: "${mem.getSelf()}"`);
  }
});

export default {
  port: PORT,
  fetch: app.fetch,
};
