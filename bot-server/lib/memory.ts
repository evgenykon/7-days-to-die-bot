import * as fs from "fs";
import * as path from "path";

const STATE_FILE = "/data/memory.json";
const HISTORY_FILE = "/data/history.json";
const SELF_FILE = "/data/self.json";
const MAX_HISTORY = 20;

interface FactEntry {
  key: string;
  value: string;
  created: number;
  updated: number;
}

interface HistoryEntry {
  role: string;
  content: string;
}

interface SelfEntry {
  text: string;
  created: number;
}

let facts: FactEntry[] = [];
let history: HistoryEntry[] = [];
let selfInfo: SelfEntry | null = null;
let lastActivity: number = Date.now();

function load(): void {
  try {
    if (fs.existsSync(STATE_FILE)) {
      facts = JSON.parse(fs.readFileSync(STATE_FILE, "utf-8"));
    }
    if (fs.existsSync(HISTORY_FILE)) {
      history = JSON.parse(fs.readFileSync(HISTORY_FILE, "utf-8"));
    }
    if (fs.existsSync(SELF_FILE)) {
      selfInfo = JSON.parse(fs.readFileSync(SELF_FILE, "utf-8"));
    }
  } catch {}
}

function save(): void {
  try {
    const dir = path.dirname(STATE_FILE);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(STATE_FILE, JSON.stringify(facts, null, 2));
    fs.writeFileSync(HISTORY_FILE, JSON.stringify(history, null, 2));
    fs.writeFileSync(SELF_FILE, JSON.stringify(selfInfo, null, 2));
  } catch (e: any) {
    console.error(`[Mem] Save error: ${e.message}`);
  }
}

export function addFact(key: string, value: string): void {
  const existing = facts.find((f) => f.key === key);
  if (existing) {
    existing.value = value;
    existing.updated = Date.now();
  } else {
    facts.push({ key, value, created: Date.now(), updated: Date.now() });
  }
  save();
}

export function getFacts(): FactEntry[] {
  return [...facts];
}

export function getContextString(): string {
  if (facts.length === 0) return "";
  return facts.map((f) => `- ${f.key}: ${f.value}`).join("\n");
}

export function addMessage(role: string, content: string): void {
  history.push({ role, content });
  if (history.length > MAX_HISTORY) {
    history = history.slice(history.length - MAX_HISTORY);
  }
  save();
}

export function getHistory(): HistoryEntry[] {
  return [...history];
}

export function setSelf(info: string): void {
  selfInfo = { text: info, created: Date.now() };
  save();
}

export function getSelf(): string | null {
  return selfInfo?.text || null;
}

export function clearSelf(): void {
  selfInfo = null;
  save();
}

export function updateActivity(): void {
  lastActivity = Date.now();
}

export function getLastActivity(): number {
  return lastActivity;
}

export function resetAll(): void {
  facts = [];
  history = [];
  selfInfo = null;
  save();
}

load();
