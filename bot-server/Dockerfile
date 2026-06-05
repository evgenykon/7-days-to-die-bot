FROM oven/bun:1-alpine
RUN mkdir -p /data
WORKDIR /app
COPY package.json ./
RUN bun install
COPY *.ts ./
COPY lib/ ./lib/
EXPOSE 9091
CMD ["bun", "--watch", "run", "server.ts"]
