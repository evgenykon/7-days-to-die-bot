const http = require("http");
const { parseBody, json, httpPost } = require("./lib/http");
const { callLLM } = require("./lib/llm");
const { sendChat, sendPlayWav, sendFollow, sendStop, getHealth, getStatus } = require("./lib/game");
const { speak } = require("./lib/piper");
const rel = require("./lib/relationship");
const mem = require("./lib/memory");
const prompt = require("./lib/prompt");

const PIPER_URL = process.env.PIPER_URL || "http://piper-server:9092";
const TTS_LENGTH = parseFloat(process.env.TTS_LENGTH || "0.85");

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
      const data = await getHealth();
      json(res, {
        ok: true,
        game: data,
        relationship: { level: rel.getLevelName(), sentiment: rel.getSentiment(), messages: rel.getMessageCount() },
        memory: require("./lib/memory").getFacts().length,
      });
    } else if (path === "/status" && method === "GET") {
      const data = await getStatus();
      json(res, { ok: true, game: data });

    } else if (path === "/chat" && method === "POST") {
      const body = await parseBody(req);
      if (!body.message) {
        json(res, { error: "message is required" }, 400);
        return;
      }

      log(`Chat from ${body.sender || "?"}: "${body.message}"`);

      rel.processMessage(body.message);

      const history = mem.getHistory();
      const messages = [prompt.build(), ...history, { role: "user", content: body.message }];

      callLLM(messages).then((raw) => {
        const facts = raw.match(/\[ФАКТ:\s*([^=]+)=([^\]]+)\]/g);
        let reply = raw;
        if (facts) {
          facts.forEach((f) => {
            const m = f.match(/\[ФАКТ:\s*([^=]+)=([^\]]+)\]/);
            if (m) mem.addFact(m[1].trim(), m[2].trim());
          });
          reply = raw.replace(/\[ФАКТ:[^\]]*\]\s*/g, "").trim();
        }
        mem.addMessage("user", body.message);
        mem.addMessage("assistant", reply);
        log(`LLM reply: "${reply}" (level=${rel.getLevelName()}, sentiment=${rel.getSentiment()})`);

        sendChat("Quinn", reply).then((r) => {
          log(`Reply sent to game: ${JSON.stringify(r)}`);
        }).catch((e) => {
          log(`Reply send error: ${e.message}`);
        });

        speak(reply).then((tts) => {
          log(`TTS generated (${tts?.wav?.length || 0} chars)`);
          if (tts?.wav) {
            sendPlayWav(tts.wav).then((r) => {
              log(`Game play: ${JSON.stringify(r)}`);
            }).catch((e) => {
              log(`Game play error: ${e.message}`);
            });
          }
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
      const tts = await speak(body.text, body.length_scale).catch((e) => ({ error: e.message }));
      json(res, tts || { error: "no response" });

    } else if (path === "/send" && method === "POST") {
      const body = await parseBody(req);
      if (!body.message) {
        json(res, { error: "message is required" }, 400);
        return;
      }
      log(`Send: ${body.sender || "System"} -> "${body.message}"`);
      const data = await sendChat(body.sender || "System", body.message);
      json(res, data);

    } else if (path === "/follow" && method === "POST") {
      const data = await sendFollow();
      json(res, data);

    } else if (path === "/stop" && method === "POST") {
      const data = await sendStop();
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
  console.log(`[Server] Game mod URL: ${process.env.BOT_URL || "http://localhost:9090"}`);
  console.log(`[Server] LLM URL: ${process.env.LLM_URL || "http://host.docker.internal:1234"}`);
  console.log(`[Server] Piper URL: ${PIPER_URL} (speed: ${TTS_LENGTH})`);
  console.log(`[Server] Relationship: ${rel.getLevelName()}, ${rel.getMessageCount()} msgs`);
});
