FROM oven/bun:alpine
WORKDIR /app
COPY package.json bun.lock* ./
RUN bun install
COPY *.ts ./
COPY tsconfig.json ./
EXPOSE 3000
CMD ["bun", "run", "server.ts"]
