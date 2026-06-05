export interface HttpResponse<T = any> {
  status: number;
  data: T;
}

function log(msg: string): void {
  console.log(`[HTTP] ${msg}`);
}

export async function httpPost<T = any>(
  baseUrl: string,
  path: string,
  body?: any,
  extraHeaders?: Record<string, string>,
): Promise<T> {
  const normalizedBase = baseUrl.replace(/\/?$/, "/");
  const relPath = path.startsWith("/") ? "." + path : path;
  const url = new URL(relPath, normalizedBase);
  const bodyStr = body ? JSON.stringify(body) : undefined;

  const res = await fetch(url.toString(), {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(extraHeaders || {}),
    },
    body: bodyStr,
    signal: AbortSignal.timeout(30000),
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`HTTP ${res.status}: ${text}`);
  }

  const text = await res.text();
  try {
    return JSON.parse(text) as T;
  } catch {
    return text as unknown as T;
  }
}

export async function parseBody(req: Request): Promise<Record<string, any>> {
  try {
    return await req.json();
  } catch {
    return {};
  }
}

export function jsonResponse(data: any, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}
