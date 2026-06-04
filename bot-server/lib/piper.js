const { httpPost } = require("./http");

const PIPER_URL = process.env.PIPER_URL || "http://piper-server:9092";
const TTS_LENGTH = parseFloat(process.env.TTS_LENGTH || "0.85");

function speak(text, lengthScale) {
  return httpPost(PIPER_URL, "/speak", {
    text,
    length_scale: lengthScale ?? TTS_LENGTH,
  });
}

function ping() {
  return httpPost(PIPER_URL, "/ping");
}

module.exports = { speak, ping, TTS_LENGTH };
