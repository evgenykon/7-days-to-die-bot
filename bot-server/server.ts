import { Hono } from "hono";

const app = new Hono();

const BOT_URL = process.env.BOT_URL || "http://localhost:9090";

async function sendToBot(path: string, body?: any): Promise<any> {
  const res = await fetch(`${BOT_URL}${path}`, {
    method: body ? "POST" : "GET",
    headers: body ? { "Content-Type": "application/json" } : {},
    body: body ? JSON.stringify(body) : undefined,
  });
  return res.json();
}

app.get("/health", async (c) => {
  try {
    const data = await sendToBot("/health");
    return c.json({ ok: true, game: data });
  } catch (err: any) {
    return c.json({ ok: false, game: "disconnected", error: err.message });
  }
});

app.get("/status", async (c) => {
  try {
    const data = await sendToBot("/status");
    return c.json({ ok: true, game: data });
  } catch (err: any) {
    return c.json({ ok: false, game: "disconnected", error: err.message });
  }
});

app.post("/chat", async (c) => {
  const body = await c.req.json<{ message?: string }>();
  if (!body.message) {
    return c.json({ error: "message is required" }, 400);
  }
  return c.json({ ok: true, message: body.message });
});

app.post("/send", async (c) => {
  const body = await c.req.json<{ sender?: string; message?: string }>();
  if (!body.message) {
    return c.json({ error: "message is required" }, 400);
  }
  try {
    const data = await sendToBot("/chat", { sender: body.sender || "System", message: body.message });
    return c.json(data);
  } catch (err: any) {
    return c.json({ error: err.message }, 500);
  }
});

app.post("/follow", async (c) => {
  try {
    const data = await sendToBot("/follow");
    return c.json(data);
  } catch (err: any) {
    return c.json({ error: err.message }, 500);
  }
});

app.post("/stop", async (c) => {
  try {
    const data = await sendToBot("/stop");
    return c.json(data);
  } catch (err: any) {
    return c.json({ error: err.message }, 500);
  }
});

const PORT = parseInt(process.env.PORT || "9091");

console.log(`[Server] Bot proxy running on port ${PORT}`);
console.log(`[Server] Game mod URL: ${BOT_URL}`);
console.log(`[Server] GET  /health  - check game connection`);
console.log(`[Server] GET  /status  - bot status`);
console.log(`[Server] POST /chat    - send message`);
console.log(`[Server] POST /send    - send message (alias)`);
console.log(`[Server] POST /follow  - start following`);
console.log(`[Server] POST /stop    - stop following`);

export default {
  port: PORT,
  fetch: app.fetch,
};
