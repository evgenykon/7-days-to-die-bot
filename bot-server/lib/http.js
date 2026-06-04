const http = require("http");
const https = require("https");

function httpPost(baseUrl, path, body, extraHeaders) {
  return new Promise((resolve, reject) => {
    const bodyStr = body ? JSON.stringify(body) : "";
    const relPath = path.startsWith("/") ? "." + path : path;
    const url = new URL(relPath, baseUrl.replace(/\/?$/, "/"));
    const isHttps = url.protocol === "https:";
    const mod = isHttps ? https : http;
    const options = {
      hostname: url.hostname,
      port: url.port || (isHttps ? 443 : 80),
      path: url.pathname + url.search,
      method: "POST",
      headers: Object.assign({
        "Content-Type": "application/json",
        "Content-Length": Buffer.byteLength(bodyStr),
      }, extraHeaders || {}),
    };
    const req = mod.request(options, (res) => {
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

function parseBody(req) {
  return new Promise((resolve) => {
    let data = "";
    req.on("data", (chunk) => (data += chunk));
    req.on("end", () => {
      try { resolve(JSON.parse(data)); } catch { resolve({}); }
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

module.exports = { httpPost, parseBody, json };
