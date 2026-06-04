const { httpPost } = require("./http");

const BOT_URL = process.env.BOT_URL || "http://localhost:9090";

function sendChat(sender, message) {
  return httpPost(BOT_URL, "/chat", { sender, message });
}

function sendPlayWav(wav) {
  return httpPost(BOT_URL, "/play-wav", { wav });
}

function sendFollow() {
  return httpPost(BOT_URL, "/follow");
}

function sendStop() {
  return httpPost(BOT_URL, "/stop");
}

function getHealth() {
  return httpPost(BOT_URL, "/health").catch((e) => ({ error: e.message }));
}

function getStatus() {
  return httpPost(BOT_URL, "/status").catch((e) => ({ error: e.message }));
}

module.exports = { sendChat, sendPlayWav, sendFollow, sendStop, getHealth, getStatus };
