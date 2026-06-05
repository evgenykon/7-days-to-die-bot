import { httpPost } from "./http";

const LLM_URL = (process.env.LLM_URL || "http://host.docker.internal:1234/v1").replace(/\/?$/, "/");
const LLM_MODEL = process.env.LLM_MODEL || "openai/gpt-4o-mini";
const OPENROUTER_KEY = process.env.OPENROUTER_API_KEY || "";

export interface LLMMessage {
  role: "system" | "user" | "assistant";
  content: string;
}

interface LLMResponse {
  choices?: Array<{
    message?: { content?: string; reasoning_content?: string };
  }>;
  error?: any;
}

function isOpenRouter(): boolean {
  return LLM_URL.includes("openrouter.ai");
}

function getHeaders(): Record<string, string> {
  if (OPENROUTER_KEY) {
    return {
      Authorization: `Bearer ${OPENROUTER_KEY}`,
      "HTTP-Referer": "https://github.com/ai-7d2d",
      "X-Title": "7D2D Companion Putnitsa",
    };
  }
  return {};
}

export async function callLLM(messages: LLMMessage[]): Promise<string> {
  const headers = getHeaders();
  const lastUser = messages.filter(m => m.role === "user").pop();
  const systemLen = messages.filter(m => m.role === "system").reduce((a, m) => a + m.content.length, 0);
  const sysPreview = messages.find(m => m.role === "system")?.content.substring(0, 120).replace(/\n/g, "\\n") || "";
  console.log(`[LLM] ${messages.length} msgs (system: ${systemLen}ch) | first system: "${sysPreview}..."`);
  if (lastUser) console.log(`[LLM] last user: "${lastUser.content.substring(0, 120)}"`);
  try {
    const json = await httpPost<LLMResponse>(LLM_URL, "/chat/completions", {
      model: LLM_MODEL,
      messages,
      temperature: 0.7,
      max_tokens: 512,
      stream: false,
    }, headers);

    if (typeof json === "string") {
      console.log("[LLM] Raw response:", json.substring(0, 200));
      return "";
    }
    if (!json) {
      console.log("[LLM] Empty response");
      return "";
    }
    if (json.error) {
      console.log("[LLM] API error:", JSON.stringify(json.error));
      return "";
    }
    const c = json.choices?.[0];
    let text = (c?.message?.content || c?.message?.reasoning_content || "").trim();
    if (c?.message?.content?.trim()) text = c.message.content.trim();
    else if (c?.message?.reasoning_content) text = c.message.reasoning_content.trim();
    return text;
  } catch (e: any) {
    console.log("[LLM] Request error:", e.message);
    return "";
  }
}

export { LLM_URL, LLM_MODEL };
