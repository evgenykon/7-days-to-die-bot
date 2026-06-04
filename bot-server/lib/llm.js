const { httpPost } = require("./http");

const LLM_URL = (process.env.LLM_URL || "http://host.docker.internal:1234/v1").replace(/\/?$/, "/");
const LLM_MODEL = process.env.LLM_MODEL || "nvidia/nemotron-3-nano-4b";
const OPENROUTER_KEY = process.env.OPENROUTER_API_KEY || "";

function isOpenRouter() {
  return LLM_URL.includes("openrouter.ai");
}

function getHeaders() {
  if (OPENROUTER_KEY) {
    return {
      "Authorization": `Bearer ${OPENROUTER_KEY}`,
      "HTTP-Referer": "https://github.com/ai-7d2d",
      "X-Title": "7D2D Companion Quinn",
    };
  }
  return {};
}

function callLLM(messages) {
  const headers = getHeaders();
  return httpPost(LLM_URL, "/chat/completions", {
    model: LLM_MODEL,
    messages,
    temperature: 0.7,
    max_tokens: 512,
    stream: false,
  }, headers).then((json) => {
    if (typeof json === "string") { console.log("[LLM] Raw response:", json.substring(0, 200)); return ""; }
    if (!json) { console.log("[LLM] Empty response"); return ""; }
    if (json.error) { console.log("[LLM] API error:", JSON.stringify(json.error)); return ""; }
    const c = json?.choices?.[0];
    let text = (c?.message?.content || c?.message?.reasoning_content || "").trim();
    if (c?.message?.content?.trim()) text = c.message.content.trim();
    else if (c?.message?.reasoning_content) text = c.message.reasoning_content.trim();
    return text;
  }).catch((e) => {
    console.log("[LLM] Request error:", e.message);
    return "";
  });
}

module.exports = { callLLM, LLM_URL, LLM_MODEL };
