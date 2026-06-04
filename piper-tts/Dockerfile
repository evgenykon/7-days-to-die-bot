FROM node:22-alpine AS base
RUN apk add --no-cache wget tar gzip libgcc libstdc++

# Download piper Linux binary
RUN wget -qO /tmp/piper.tar.gz https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_linux_x86_64.tar.gz \
 && tar -xzf /tmp/piper.tar.gz -C /opt \
 && rm /tmp/piper.tar.gz

FROM node:22-alpine
WORKDIR /app

# Copy piper + espeak
COPY --from=base /opt/piper /opt/piper
ENV PATH="/opt/piper:${PATH}"

# Voice model
COPY voice.onnx /app/voice.onnx
COPY voice.onnx.json /app/voice.onnx.json
COPY espeak-ng-data /app/espeak-ng-data

# Server
COPY server.js /app/server.js
EXPOSE 9092
CMD ["node", "/app/server.js"]
