FROM oven/bun:alpine
WORKDIR /app
COPY package.json bun.lock* ./
RUN bun install
COPY *.ts ./
COPY tsconfig.json ./
EXPOSE 9091
CMD ["bun", "run", "server.ts"]
