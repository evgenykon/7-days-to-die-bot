import * as fs from "fs";
import * as path from "path";

const STATE_FILE = "/data/relationship.json";
const LEVELS = ["незнакомы", "tentative", "trusting", "friendly"] as const;
const SENTIMENTS = ["loyal", "angry", "rejecting"] as const;

type Level = number;
type Sentiment = typeof SENTIMENTS[number];

interface RelationshipState {
  level: Level;
  sentiment: Sentiment;
  messageCount: number;
  lastUpdate: number;
}

function defaultState(): RelationshipState {
  return {
    level: 0,
    sentiment: "loyal",
    messageCount: 0,
    lastUpdate: Date.now(),
  };
}

let state: RelationshipState = defaultState();

function load(): void {
  try {
    if (fs.existsSync(STATE_FILE)) {
      state = JSON.parse(fs.readFileSync(STATE_FILE, "utf-8")) as RelationshipState;
      return;
    }
  } catch {}
  state = defaultState();
}

function save(): void {
  try {
    const dir = path.dirname(STATE_FILE);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    state.lastUpdate = Date.now();
    fs.writeFileSync(STATE_FILE, JSON.stringify(state, null, 2));
  } catch (e: any) {
    console.error(`[Rel] Save error: ${e.message}`);
  }
}

export function getLevel(): Level {
  return state.level;
}

export function getLevelName(): string {
  return LEVELS[state.level] || "cold";
}

export function getSentiment(): Sentiment {
  return state.sentiment;
}

export function getMessageCount(): number {
  return state.messageCount;
}

function adjustSentiment(playerMessage: string): Sentiment {
  const lower = (playerMessage || "").toLowerCase();
  const angryWords = ["отстань", "заткнись", "иди нахуй", "не лезь", "отвали", "заебал"];
  const kindWords = ["спасибо", "хорошо", "да", "расскажи", "помоги", "друг", "милая", "умница"];

  if (angryWords.some((w) => lower.includes(w))) {
    if (state.sentiment === "loyal") state.sentiment = "angry";
    return "angry";
  }
  if (kindWords.some((w) => lower.includes(w))) {
    state.sentiment = "loyal";
    return "loyal";
  }
  return state.sentiment;
}

function advanceLevel(): boolean {
  if (state.level < LEVELS.length - 1) {
    state.level++;
    save();
    return true;
  }
  return false;
}

export function reset(): void {
  state = defaultState();
  save();
}

export function processMessage(playerMessage: string): void {
  state.messageCount++;
  adjustSentiment(playerMessage);

  if (state.sentiment === "loyal") {
    const thresholds = [3, 10, 25];
    if (state.messageCount >= thresholds[state.level]) {
      advanceLevel();
    }
  } else if (state.sentiment === "rejecting") {
    if (state.level > 0) state.level = 0;
  }

  save();
}

export function getDescription(): string {
  const descs = [
    "Вы незнакомы. Путница сдержанна и осторожна, говорит коротко.",
    "Путница начинает привыкать, намекает на совместное выживание в этом мёртвом мире.",
    "Путница доверяет, делится страхами. Ей важно твоё мнение.",
    "Путница заботится о тебе, спрашивает о жизни, готова говорить о прошлом и будущем.",
  ];
  return descs[state.level] || descs[0];
}

load();
