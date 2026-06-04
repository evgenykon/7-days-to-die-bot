@echo off
cd /d "%~dp0"
echo Starting Companion Bot Server...
bun install
bun run server.ts
