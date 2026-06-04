import { Hono } from "hono";

const app = new Hono();

const BOT_URL = process.env.BOT_URL || "http://host.docker.internal:8080";
const LLM_API_URL =
  process.env.LLM_API_URL || "https://api.openai.com/v1/chat/completions";
const LLM_API_KEY = process.env.LLM_API_KEY || "";
const LLM_MODEL = process.env.LLM_MODEL || "gpt-4o-mini";

interface ChatMessage {
  role: "system" | "user" | "assistant";
  content: string;
}

const conversationHistory: ChatMessage[] = [
  {
    role: "system",
    content: `You are Quinn, a companion character in a zombie survival game (7 Days to Die). 
You follow the player and help them survive. You're witty, caring, and resourceful.
Keep responses short (1-2 sentences max). You can comment on the environment, give tips, or just chat.
You see what the player does in the game through the messages they send you.`,
  },
];

async function sendToBot(sender: string, message: string): Promise<any> {
  const res = await fetch(`${BOT_URL}/chat`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ sender, message }),
  });
  return res.json();
}

async function callLLM(userMessage: string): Promise<string> {
  conversationHistory.push({ role: "user", content: userMessage });

  if (conversationHistory.length > 20) {
    conversationHistory.splice(1, conversationHistory.length - 20);
  }

  const res = await fetch(LLM_API_URL, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${LLM_API_KEY}`,
    },
    body: JSON.stringify({
      model: LLM_MODEL,
      messages: conversationHistory,
      max_tokens: 150,
      temperature: 0.8,
    }),
  });

  if (!res.ok) {
    throw new Error(`LLM API error ${res.status}: ${await res.text()}`);
  }

  const data = await res.json();
  const reply: string = data.choices[0].message.content.trim();
  conversationHistory.push({ role: "assistant", content: reply });
  return reply;
}

app.post("/chat", async (c) => {
  const body = await c.req.json<{ message?: string }>();
  const message = body.message;

  if (!message) {
    return c.json({ error: "message is required" }, 400);
  }

  console.log(`[Chat] Player: ${message}`);

  try {
    const reply = await callLLM(message);
    console.log(`[Chat] Quinn: ${reply}`);

    await sendToBot("Quinn", reply);

    return c.json({ ok: true, reply });
  } catch (err: any) {
    console.error("[Chat] Error:", err.message);

    const fallback = "I'm having trouble thinking right now...";
    try {
      await sendToBot("Quinn", fallback);
    } catch {}

    return c.json({ error: err.message, fallback }, 500);
  }
});

app.post("/send", async (c) => {
  const body = await c.req.json<{ sender?: string; message?: string }>();
  const message = body.message;

  if (!message) {
    return c.json({ error: "message is required" }, 400);
  }

  try {
    await sendToBot(body.sender || "System", message);
    return c.json({ ok: true });
  } catch (err: any) {
    return c.json({ error: err.message }, 500);
  }
});

app.get("/health", async (c) => {
  try {
    const res = await fetch(`${BOT_URL}/health`);
    const data = await res.json();
    return c.json({ ok: true, game: data });
  } catch (err: any) {
    return c.json({ ok: false, game: "disconnected", error: err.message });
  }
});

app.get("/status", async (c) => {
  try {
    const res = await fetch(`${BOT_URL}/status`);
    const data = await res.json();
    return c.json({ ok: true, game: data, model: LLM_MODEL });
  } catch (err: any) {
    return c.json({ ok: false, game: "disconnected", error: err.message });
  }
});

const PORT = parseInt(process.env.PORT || "3000");

console.log(`[Server] Bot server running on port ${PORT}`);
console.log(`[Server] Game mod URL: ${BOT_URL}`);
console.log(`[Server] LLM API: ${LLM_API_URL}`);
console.log(`[Server] Model: ${LLM_MODEL}`);
console.log(`[Server] POST /chat { "message": "hello" } - chat via LLM`);
console.log(`[Server] POST /send { "sender": "Quinn", "message": "hi" } - direct`);

export default {
  port: PORT,
  fetch: app.fetch,
};
