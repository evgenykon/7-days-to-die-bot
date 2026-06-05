import { httpPost } from "./http";

const PIPER_URL = process.env.PIPER_URL || "http://localhost:9092";

export interface TTSResponse {
  wav?: string;
  error?: string;
}

export function speak(text: string, lengthScale?: number): Promise<TTSResponse> {
  const TTS_LENGTH = parseFloat(process.env.TTS_LENGTH || "0.85");
  return httpPost(PIPER_URL, "/speak", {
    text,
    length_scale: lengthScale ?? TTS_LENGTH,
  });
}

export function ping(): Promise<any> {
  return httpPost(PIPER_URL, "/ping");
}
