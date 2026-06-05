const STATE_FILE = "/data/memory.json";
const HISTORY_FILE = "/data/history.json";
const SELF_FILE = "/data/self.json";
const MAX_HISTORY = 20;

let facts = [];
let history = [];
let selfInfo = null;

function load() {
  try {
    const fs = require("fs");
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

function save() {
  try {
    const fs = require("fs");
    const dir = require("path").dirname(STATE_FILE);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(STATE_FILE, JSON.stringify(facts, null, 2));
    fs.writeFileSync(HISTORY_FILE, JSON.stringify(history, null, 2));
    fs.writeFileSync(SELF_FILE, JSON.stringify(selfInfo, null, 2));
  } catch (e) {
    console.error(`[Mem] Save error: ${e.message}`);
  }
}

function addFact(key, value) {
  const existing = facts.find((f) => f.key === key);
  if (existing) {
    existing.value = value;
    existing.updated = Date.now();
  } else {
    facts.push({ key, value, created: Date.now(), updated: Date.now() });
  }
  save();
}

function getFacts() {
  return [...facts];
}

function getContextString() {
  if (facts.length === 0) return "";
  return facts.map((f) => `- ${f.key}: ${f.value}`).join("\n");
}

function addMessage(role, content) {
  history.push({ role, content });
  if (history.length > MAX_HISTORY) {
    history = history.slice(history.length - MAX_HISTORY);
  }
  save();
}

function getHistory() {
  return [...history];
}

function setSelf(info) {
  selfInfo = { text: info, created: Date.now() };
  save();
}

function getSelf() {
  return selfInfo?.text || null;
}

function clearSelf() {
  selfInfo = null;
  save();
}

function resetAll() {
  facts = [];
  history = [];
  selfInfo = null;
  save();
}

load();

module.exports = { addFact, getFacts, getContextString, addMessage, getHistory, setSelf, getSelf, clearSelf, resetAll };
