@echo off
set ROOT=%~dp0
echo === Starting Bot Server + Piper Server (Docker) ===
cd /d "%ROOT%bot-server"
docker compose up -d
echo Bot Server running on port 9091
echo Piper Server running on port 9092
echo === All services started ===