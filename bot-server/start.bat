@echo off
cd /d "%~dp0"
echo Starting Companion Bot Server (Docker)...
docker compose up --build
