const http = require("http");

const BOT_URL = process.env.BOT_URL || "http://localhost:9090";
const PIPER_URL = process.env.PIPER_URL || "http://host.docker.internal:9092";
const LLM_URL = process.env.LLM_URL || "http://host.docker.internal:1234";
const LLM_MODEL = process.env.LLM_MODEL || "nvidia/nemotron-3-nano-4b";
const TTS_LENGTH = parseFloat(process.env.TTS_LENGTH || "0.85"); // voice speed (<1 = faster)

const SYSTEM_PROMPT = {
  role: "system",
  content: "Ты Квинн, спутник в игре 7 Days to Die. Отвечай ТОЛЬКО на русском языке. Коротко, 1-3 предложения. Будь дружелюбной и атмосферной в зомби-апокалипсисе."
};

function httpPost(baseUrl, path, body) {
  return new Promise((resolve, reject) => {
    const bodyStr = body ? JSON.stringify(body) : "";
    const url = new URL(path, baseUrl);
    const options = {
      hostname: url.hostname,
      port: url.port,
      path: url.pathname,
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Content-Length": Buffer.byteLength(bodyStr),
      },
    };
    const req = http.request(options, (res) => {
      let data = "";
      res.on("data", (chunk) => (data += chunk));
      res.on("end", () => {
        try { resolve(JSON.parse(data)); } catch { resolve(data); }
      });
    });
    req.on("error", reject);
    req.setTimeout(30000, () => { req.destroy(); reject(new Error("timeout")); });
    if (body) req.write(bodyStr);
    req.end();
  });
}

function sendToBot(path, body) {
  return new Promise((resolve, reject) => {
    const bodyStr = body ? JSON.stringify(body) : "";
    const url = new URL(path, BOT_URL);
    const options = {
      hostname: url.hostname,
      port: url.port,
      path: url.pathname,
      method: body ? "POST" : "GET",
      headers: body ? {
        "Content-Type": "application/json",
        "Content-Length": Buffer.byteLength(bodyStr),
      } : {},
    };
    const req = http.request(options, (res) => {
      let data = "";
      res.on("data", (chunk) => (data += chunk));
      res.on("end", () => {
        try {
          resolve(JSON.parse(data));
        } catch {
          resolve(data);
        }
      });
    });
    req.on("error", reject);
    if (body) req.write(bodyStr);
    req.end();
  });
}

function callLLM(messages) {
  return httpPost(LLM_URL, "/v1/chat/completions", {
    model: LLM_MODEL,
    messages,
    temperature: 0.7,
    max_tokens: 4096,
    stream: false,
  }).then((json) => {
    const c = json?.choices?.[0];
    let text = (c?.message?.content || c?.message?.reasoning_content || "").trim();
    // Use content if available, otherwise use reasoning_content
    if (c?.message?.content?.trim()) text = c.message.content.trim();
    else if (c?.message?.reasoning_content) text = c.message.reasoning_content.trim();
    return text;
  });
}

function parseBody(req) {
  return new Promise((resolve) => {
    let data = "";
    req.on("data", (chunk) => (data += chunk));
    req.on("end", () => {
      try {
        resolve(JSON.parse(data));
      } catch {
        resolve({});
      }
    });
  });
}

function json(res, data, status = 200) {
  const body = JSON.stringify(data);
  res.writeHead(status, {
    "Content-Type": "application/json",
    "Content-Length": Buffer.byteLength(body),
  });
  res.end(body);
}

function log(msg) {
  console.log(`[${new Date().toISOString()}] ${msg}`);
}

const server = http.createServer(async (req, res) => {
  const method = req.method.toUpperCase();
  const url = new URL(req.url, `http://${req.headers.host || "localhost"}`);
  const path = url.pathname.replace(/\/$/, "") || "/";

  log(`${method} ${path}`);

  try {
    if (path === "/health" && method === "GET") {
      const data = await sendToBot("/health").catch((e) => ({
        error: e.message,
      }));
      json(res, { ok: true, game: data });
    } else if (path === "/status" && method === "GET") {
      const data = await sendToBot("/status").catch((e) => ({
        error: e.message,
      }));
      json(res, { ok: true, game: data });
    } else if (path === "/chat" && method === "POST") {
      const body = await parseBody(req);
      if (!body.message) {
        json(res, { error: "message is required" }, 400);
        return;
      }
      log(`Chat from ${body.sender || "?"}: "${body.message}"`);
      const messages = [SYSTEM_PROMPT, { role: "user", content: body.message }];
      callLLM(messages).then((reply) => {
        log(`LLM reply: "${reply}"`);
        sendToBot("/chat", { sender: "Quinn", message: reply }).then((r) => {
          log(`Reply sent to game: ${JSON.stringify(r)}`);
        }).catch((e) => {
          log(`Reply send error: ${e.message}`);
        });
        httpPost(PIPER_URL, "/speak", { text: reply, length_scale: TTS_LENGTH }).then((r) => {
          log(`TTS triggered: ${JSON.stringify(r)}`);
        }).catch((e) => {
          log(`TTS error: ${e.message}`);
        });
      }).catch((e) => {
        log(`LLM error: ${e.message}`);
      });
      json(res, { ok: true, message: "Processing..." });
    } else if (path === "/event" && method === "POST") {
      const body = await parseBody(req);
      log(`Event: ${body.type || "?"} -> ${JSON.stringify(body)}`);
      json(res, { ok: true });
    } else if (path === "/speak" && method === "POST") {
      const body = await parseBody(req);
      if (!body.text) {
        json(res, { error: "text is required" }, 400);
        return;
      }
      log(`TTS: "${body.text}"`);
      httpPost(PIPER_URL, "/speak", { text: body.text, length_scale: body.length_scale || TTS_LENGTH }).then((r) => {
        log(`TTS done: ${JSON.stringify(r)}`);
      }).catch((e) => {
        log(`TTS error: ${e.message}`);
      });
      json(res, { ok: true });
    } else if (path === "/send" && method === "POST") {
      const body = await parseBody(req);
      if (!body.message) {
        json(res, { error: "message is required" }, 400);
        return;
      }
      log(`Send: ${body.sender || "System"} -> "${body.message}"`);
      const data = await sendToBot("/chat", {
        sender: body.sender || "System",
        message: body.message,
      });
      json(res, data);
    } else if (path === "/follow" && method === "POST") {
      const data = await sendToBot("/follow").catch((e) => ({
        error: e.message,
      }));
      json(res, data);
    } else if (path === "/stop" && method === "POST") {
      const data = await sendToBot("/stop").catch((e) => ({
        error: e.message,
      }));
      json(res, data);
    } else {
      json(res, { error: `Unknown endpoint: ${path}` }, 404);
    }
  } catch (err) {
    log(`Error handling ${path}: ${err.message}`);
    json(res, { error: err.message }, 500);
  }
});

const PORT = parseInt(process.env.PORT || "9091");
server.listen(PORT, () => {
  console.log(`[Server] Bot proxy running on port ${PORT}`);
  console.log(`[Server] Game mod URL: ${BOT_URL}`);
  console.log(`[Server] LLM URL: ${LLM_URL} (model: ${LLM_MODEL})`);
  console.log(`[Server] Piper URL: ${PIPER_URL} (speed: ${TTS_LENGTH})`);
  console.log(`[Server] GET  /health  - check game connection`);
  console.log(`[Server] GET  /status  - bot status`);
  console.log(`[Server] POST /chat    - receive/send message`);
  console.log(`[Server] POST /send    - proxy to game chat`);
  console.log(`[Server] POST /follow  - start following`);
  console.log(`[Server] POST /stop    - stop following`);
});
