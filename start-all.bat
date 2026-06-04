@echo off
set ROOT=%~dp0
echo === Starting Bot Server (Docker) ===
cd /d "%ROOT%bot-server"
docker compose up -d
echo Bot Server running on port 9091

echo === Starting PiperServer (TTS) ===
start /B /MIN "" "%ROOT%piper-tts\PiperServer.exe" > "%ROOT%piper-tts\piper-server.log" 2>&1
echo PiperServer started on port 9092

echo === All services started ===
