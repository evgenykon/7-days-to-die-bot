import { httpPost } from "./http";

const BOT_URL = process.env.BOT_URL || "http://localhost:9090";

export interface GameResponse {
  ok?: boolean;
  error?: string;
  [key: string]: any;
}

export function sendChat(sender: string, message: string): Promise<GameResponse> {
  return httpPost(BOT_URL, "/chat", { sender, message });
}

export function sendPlayWav(wav: string): Promise<GameResponse> {
  return httpPost(BOT_URL, "/play-wav", { wav });
}

export function sendFollow(): Promise<GameResponse> {
  return httpPost(BOT_URL, "/follow");
}

export function sendStop(): Promise<GameResponse> {
  return httpPost(BOT_URL, "/stop");
}

export function getHealth(): Promise<GameResponse> {
  return httpPost(BOT_URL, "/health").catch((e: Error) => ({ error: e.message }));
}

export function getStatus(): Promise<GameResponse> {
  return httpPost(BOT_URL, "/status").catch((e: Error) => ({ error: e.message }));
}
