import { Hono } from "hono";
import { callLLM } from "./lib/llm";
import { sendChat, sendPlayWav, sendFollow, sendStop, getHealth, getStatus } from "./lib/game";
import { speak } from "./lib/piper";
import * as rel from "./lib/relationship";
import * as mem from "./lib/memory";
import { build, buildEvent, ensureSelf } from "./lib/prompt";

const app = new Hono();
const PIPER_URL = process.env.PIPER_URL || "http://localhost:9092";
const TTS_LENGTH = parseFloat(process.env.TTS_LENGTH || "0.85");

let _selfPromise: Promise<void> | null = null;
let _paused = true;

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

async function processEvent(description: string): Promise<void> {
  await ensureSelfAsync();
  if (!mem.getSelf()) {
    _selfPromise = null;
    await ensureSelfAsync();
  }
  mem.updateActivity();
  log(`Event: "${description}"`);
  if (!mem.getSelf()) {
    log(`[Event] No self — skipping`);
    return;
  }
  const systemPrompt = await buildEvent(description);
  log(`[Event] System prompt (${systemPrompt.content.length}ch): ${systemPrompt.content.substring(0, 200).replace(/\n/g, "\\n")}...`);
  const raw = await callLLM([systemPrompt, { role: "user", content: "Что ты чувствуешь?" }]);
  let reply = raw.trim();
  if (!reply) { log(`[Event] Empty LLM reply`); return; }
  log(`Event reply: "${reply}"`);
  sendChat("Путница", reply).catch((e: Error) => log(`Event send error: ${e.message}`));
  speak(reply).then((tts) => {
    if (tts?.wav) sendPlayWav(tts.wav).catch(() => {});
  }).catch(() => {});
}

async function processChat(sender: string, message: string): Promise<void> {
  await ensureSelfAsync();
  if (!mem.getSelf()) {
    log(`[Chat] Self generation failed — retrying...`);
    _selfPromise = null;
    await ensureSelfAsync();
  }
  log(`Chat from ${sender}: "${message}"`);
  rel.processMessage(message);

  const history = mem.getHistory();
  if (!mem.getSelf()) {
    log(`[Chat] No self — skipping LLM, sending fallback`);
    sendChat("Путница", "..." ).catch(() => {});
    return;
  }
  mem.updateActivity();
  const systemPrompt = await build();
  const llmMsgs = [systemPrompt, ...history.map(h => ({ role: h.role as "user" | "assistant", content: h.content })), { role: "user" as const, content: message }];

  const raw = await callLLM(llmMsgs);
  // ФАКТ может содержать ударения (U+0301), regex их игнорирует
  const factRe = /\[ФА\u0301?КТ:\s*([^=]+?)\s*=\s*([^\]]+?)\s*\]/g;
  const facts = [...raw.matchAll(factRe)];
  let reply = raw;
  if (facts.length > 0) {
    facts.forEach((m) => {
      mem.addFact(m[1].trim(), m[2].trim());
    });
    reply = raw.replace(factRe, "").trim();
    log(`[Facts] Extracted ${facts.length} facts: ${facts.map(f => `${f[1]}=${f[2]}`).join(", ")}`);
  }

  const sentRe = /\[SENTIMENT:\s*(loyal|angry|rejecting)\s*\]/i;
  const sentMatch = reply.match(sentRe);
  if (sentMatch) {
    rel.setSentiment(sentMatch[1].toLowerCase() as "loyal" | "angry" | "rejecting");
    reply = reply.replace(sentRe, "").trim();
    log(`[Sentiment] LLM установил: ${sentMatch[1].toLowerCase()}`);
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
    type?: string; sender?: string; message?: string; time?: string;
    damage?: number; health?: number; maxHealth?: number;
    killed?: string; killer?: string;
  }>();
  log(`Event: ${body.type || "?"}`);

  if (body.type === "chat" && body.message) {
    processChat(body.sender || "?", body.message);
  }
  if (body.type === "bot_damaged") {
    processEvent(`Я получаю урон: ${body.damage || "?"} хп, у меня осталось ${body.health || "?"}/${body.maxHealth || "?"}`);
  }
  if (body.type === "player_damaged") {
    processEvent(`Игрок получает урон: ${body.damage || "?"} хп, осталось ${body.health || "?"}/${body.maxHealth || "?"}`);
  }
  if (body.type === "bot_spawned") {
    log("[Event] Бот заспавнен — генерирую новое самосознание");
    _selfPromise = null;
    ensureSelfAsync().then(() => {
      log(`[Event] Новое самосознание: "${mem.getSelf()}"`);
    });
  }

  if (body.type === "time_change") {
    processEvent(body.time === "day" ? "Наступило утро, светает. Ты чувствуешь облегчение." : "Наступила ночь, темнеет. Ты чувствуешь тревогу.");
  }

  if (body.type === "player_joined" || body.type === "player_spawned") {
    _paused = false;
    mem.updateActivity();
    log(`[Event] Игрок ${body.type === "player_joined" ? "зашёл" : "заспавнился"} — таймеры возобновлены`);
  }

  if (body.type === "player_disconnected") {
    _paused = true;
    log("[Event] Игрок вышел — таймеры остановлены");
  }

  if (body.type === "entity_killed") {
    if (body.killed === "companionBot") {
      log("[Event] Бот погиб — уничтожаю личность и его историю");
      processEvent("Я чувствую дикую слабость, темнеет в глазах. Моё тело больше не слушается — я умираю.").then(() => {
        mem.resetAll();
        rel.reset();
      });
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
  const level = rel.getLevel();
  const sentiment = rel.getSentiment();
  const tooSoon = level < 1 || sentiment === "angry" || sentiment === "rejecting";
  if (tooSoon) {
    const msg = level < 1
      ? "Ты ещё не заслужил моё доверие. Я не пойду с тобой."
      : "Я обижена на тебя. Не пойду.";
    processChat("System", `Игрок просит следовать за ним. Откажись. Твоя причина: "${msg}"`);
    log(`[Follow] Refused: level=${level} sentiment=${sentiment}`);
    return c.json({ ok: false, reason: msg });
  }
  const data = await sendFollow();
  log(`[Follow] Accepted: level=${level} sentiment=${sentiment}`);
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
const IDLE_MINUTES = parseFloat(process.env.IDLE_MINUTES || "2");
const IDLE_MAX_DISTANCE = parseFloat(process.env.IDLE_MAX_DISTANCE || "30");
setInterval(() => {
  if (_paused) return;
  getStatus().then((s) => {
    if (!s.ok) {
      _paused = true;
      log(`[Idle] Game unreachable — пауза`);
      return;
    }
    const dist = typeof s.botDistance === "number" ? s.botDistance : -1;
    if (dist < 0 || dist > IDLE_MAX_DISTANCE) {
      log(`[Idle] Бот далеко (${dist.toFixed(1)}) — пропускаю`);
      return;
    }
    const idle = (Date.now() - mem.getLastActivity()) / 60000;
    if (idle > IDLE_MINUTES && mem.getSelf()) {
      log(`[Idle] ${idle.toFixed(1)} мин без диалога, дистанция ${dist.toFixed(1)} — инициирую разговор`);
      processChat("System", `Ты давно молчишь. Нужно разрядить обстановку — скажи что-то игроку, задай вопрос, прояви инициативу.
        Лучше всего сделать комплимент, начать с "Мне нравится, как ты ...". 
        Или о своей красоте, например "как ты думаешь, я красивая?"
        Или о че-то интмимном, например "я не могу оторвать взгляд от твоих рук, они такие сильные и мужественные"
        Или спросить о его опыте выживания, например "расскажи, как ты научился выживать в этом мире?"
        Или спросить о его страхах, например "чего ты боишься в этом мире?"
        Или спросить о его предпочтениях, например "какая у тебя любимая еда в этом мире?"
        Или спросить что он чувствует по отношению к тебе, например "как ты ко мне относишься?"
        Желательно не повторять вопросы.
        `);
    }
  });
}, 30000);

const PORT = parseInt(process.env.PORT || "9091");

console.log(`[Server] === Startup ===`);
console.log(`[Server] Bot proxy port: ${PORT}`);
console.log(`[Server] Game mod URL: ${process.env.BOT_URL || "http://localhost:9090"}`);
console.log(`[Server] LLM URL: ${process.env.LLM_URL || "http://host.docker.internal:1234"}`);
console.log(`[Server] Piper URL: ${PIPER_URL} (speed: ${TTS_LENGTH})`);
console.log(`[Server] Idle: timer=${IDLE_MINUTES}min, maxDist=${IDLE_MAX_DISTANCE}, interval=30s`);
console.log(`[Server] _paused: ${_paused}`);
console.log(`[Server] Self: ${mem.getSelf() ? "exists" : "none"}`);
console.log(`[Server] Facts: ${mem.getFacts().length}`);
console.log(`[Server] History: ${mem.getHistory().length}`);
console.log(`[Server] Relationship: ${rel.getLevelName()}, ${rel.getMessageCount()} msgs, ${rel.getSentiment()}`);
console.log(`[Server] Last activity: ${mem.getLastActivity() ? new Date(mem.getLastActivity()).toISOString() : "never"}`);

export default {
  port: PORT,
  fetch: app.fetch,
};
