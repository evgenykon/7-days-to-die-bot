FROM node:22-bookworm-slim
RUN apt-get update && apt-get install -y --no-install-recommends libgcc-s1 libstdc++6 && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Piper binary (pre-downloaded on host)
COPY piper-linux/piper/piper /opt/piper/piper
COPY piper-linux/piper/piper_phonemize /opt/piper/piper_phonemize
COPY piper-linux/piper/libonnxruntime.so.1.14.1 /opt/piper/
COPY piper-linux/piper/libespeak-ng.so.1.52.0.1 /opt/piper/
COPY piper-linux/piper/libpiper_phonemize.so.1.2.0 /opt/piper/
COPY piper-linux/piper/libtashkeel_model.ort /opt/piper/
RUN ln -s libespeak-ng.so.1.52.0.1 /opt/piper/libespeak-ng.so.1 && \
    ln -s libespeak-ng.so.1 /opt/piper/libespeak-ng.so && \
    ln -s libpiper_phonemize.so.1.2.0 /opt/piper/libpiper_phonemize.so.1 && \
    ln -s libpiper_phonemize.so.1 /opt/piper/libpiper_phonemize.so && \
    ln -s libonnxruntime.so.1.14.1 /opt/piper/libonnxruntime.so

ENV LD_LIBRARY_PATH=/opt/piper

# espeak data
COPY espeak-ng-data /app/espeak-ng-data

# Voice model
COPY voice.onnx /app/voice.onnx
COPY voice.onnx.json /app/voice.onnx.json

# Server
COPY server.js /app/server.js
EXPOSE 9092
CMD ["node", "/app/server.js"]
