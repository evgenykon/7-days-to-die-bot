const LEVELS = ["незнакомы", "tentative", "trusting", "friendly"];
const SENTIMENTS = ["loyal", "angry", "rejecting"];

const STATE_FILE = "/data/relationship.json";

function defaultState() {
  return {
    level: 0,
    sentiment: "loyal",
    messageCount: 0,
    lastUpdate: Date.now(),
  };
}

let state = null;

function load() {
  try {
    const fs = require("fs");
    if (fs.existsSync(STATE_FILE)) {
      state = JSON.parse(fs.readFileSync(STATE_FILE, "utf-8"));
      return;
    }
  } catch {}
  state = defaultState();
}

function save() {
  try {
    const fs = require("fs");
    const dir = require("path").dirname(STATE_FILE);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    state.lastUpdate = Date.now();
    fs.writeFileSync(STATE_FILE, JSON.stringify(state, null, 2));
  } catch (e) {
    console.error(`[Rel] Save error: ${e.message}`);
  }
}

function getLevel() { return state.level; }
function getLevelName() { return LEVELS[state.level] || "cold"; }
function getSentiment() { return state.sentiment; }
function getMessageCount() { return state.messageCount; }

function adjustSentiment(playerMessage) {
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

function advanceLevel() {
  if (state.level < LEVELS.length - 1) {
    state.level++;
    save();
    return true;
  }
  return false;
}

function processMessage(playerMessage) {
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

function getDescription() {
  const descs = [
    "Вы незнакомы. Квинн сдержанна и осторожна, говорит коротко.",
    "Квинн начинает привыкать, намекает на совместное выживание в этом мёртвом мире.",
    "Квинн доверяет, делится страхами. Ей важно твоё мнение.",
    "Квинн заботится о тебе, спрашивает о жизни, готова говорить о прошлом и будущем.",
  ];
  return descs[state.level] || descs[0];
}

load();

module.exports = {
  getLevel, getLevelName, getSentiment, getMessageCount,
  processMessage, getDescription,
  LEVELS, SENTIMENTS,
};
