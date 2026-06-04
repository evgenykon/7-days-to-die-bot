const http = require("http");
const { spawn } = require("child_process");
const fs = require("fs");
const path = require("path");
const os = require("os");

const MODEL = process.env.MODEL || "/app/voice.onnx";
const ESpeak = process.env.ESPEAK || "/app/espeak-ng-data";
const PORT = parseInt(process.env.PORT || "9092");

function log(msg) {
  console.log(`[${new Date().toISOString()}] ${msg}`);
}

function json(res, data, status = 200) {
  const body = JSON.stringify(data);
  res.writeHead(status, { "Content-Type": "application/json", "Content-Length": Buffer.byteLength(body) });
  res.end(body);
}

function parseBody(req) {
  return new Promise((resolve) => {
    let data = "";
    req.on("data", (c) => (data += c));
    req.on("end", () => { try { resolve(JSON.parse(data)); } catch { resolve({}); } });
  });
}

const server = http.createServer(async (req, res) => {
  try {
    const method = req.method.toUpperCase();
    const url = new URL(req.url, `http://${req.headers.host || "localhost"}`);
    const pname = url.pathname.replace(/\/$/, "") || "/";

    if (pname === "/ping" && method === "GET") {
      return json(res, { ok: true });
    }

    if (pname !== "/speak" || method !== "POST") {
      return json(res, { error: "use POST /speak" }, 404);
    }

    const body = await parseBody(req);
    if (!body.text) {
      return json(res, { error: "text is required" }, 400);
    }

    log(`Speak: "${body.text}"`);

    const ls = body.length_scale ?? 1.0;
    const ns = body.noise_scale ?? 0.667;
    const nw = body.noise_w ?? 0.8;
    const tmp = path.join(os.tmpdir(), `piper_${Date.now()}_${Math.random().toString(36).slice(2)}.wav`);

    const args = [
      "--model", MODEL,
      "--output-file", tmp,
      "--length_scale", String(ls),
      "--noise_scale", String(ns),
      "--noise_w", String(nw),
      "--espeak_data", ESpeak,
    ];

    const piper = spawn("/opt/piper/piper", args, { stdio: ["pipe", "ignore", "inherit"] });
    piper.stdin.end(Buffer.from(body.text, "utf-8"));

    let timedOut = false;
    const timer = setTimeout(() => { timedOut = true; piper.kill(); }, 30000);

    piper.on("exit", (code) => {
      clearTimeout(timer);
      if (timedOut) return json(res, { error: "piper timed out" }, 500);
      if (code !== 0) return json(res, { error: `piper exit code ${code}` }, 500);

      try {
        const wav = fs.readFileSync(tmp);
        const b64 = wav.toString("base64");
        fs.unlinkSync(tmp);
        log(`WAV ${wav.length} bytes`);
        json(res, { ok: true, wav: b64, format: "wav", sample_rate: 22050 });
      } catch (e) {
        json(res, { error: e.message }, 500);
      }
    });

    piper.on("error", (e) => {
      clearTimeout(timer);
      json(res, { error: e.message }, 500);
    });
  } catch (e) {
    log(`Error: ${e.message}`);
    json(res, { error: e.message }, 500);
  }
});

server.listen(PORT, () => {
  console.log(`[Piper] Server on port ${PORT}`);
  console.log(`[Piper] Model: ${MODEL}`);
});
