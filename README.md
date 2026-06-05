# CompanionBot

AI Companion for 7 Days to Die.

## Зависимости

- **7 Days to Die** V2.6 (Steam, `F:\SteamLibrary\steamapps\common\7 Days To Die`)
- **.NET Framework 4.8 SDK** (для сборки C# мода)
- **.NET 8 SDK** (для bot-server, опционально)
- **Docker Desktop** (для bot-server + Piper TTS, опционально)

## Сборка C# мода

```powershell
dotnet build src\CompanionBot\CompanionBot.csproj
```

`Directory.Build.props` содержит путь к игре (`GamePath`). Если игра не в стандартной папке — скопируй `Directory.Build.props.example` и поправь путь.

После сборки DLL + Config автоматически копируются в `Mods/CompanionBot/`.

## Запуск

1. Запустить игру без EAC (через Steam с флагом `-noeac` или через `launch.bat`)
2. В консоли (F1): `scc` — заспавнить бота

## Команды в игре

```
scc / sc / spawncompanion  — заспавнить бота рядом с игроком
scc kill / sc k            — удалить всех ботов
```

## Архитектура

- **C# мод** (.NET Framework 4.8) — `src/CompanionBot/`
- `EntityAlive` — базовый класс (vanilla, без AI)
- `TraderJoel.prefab` — модель с `AvatarNpcController`
- Движение: `motion` + `DefaultMoveEntity()` в `OnUpdatePosition()`
- Никаких внешних зависимостей (SCore, Harmony)

## Структура проекта

```
Directory.Build.props   — путь к игре
AGENTS.md               — история: что пробовали, что не сработало
src/CompanionBot/       — C# мод (.NET 4.8)
bot-server/             — Node.js прокси + Docker + Piper TTS
v1/ v2/                 — удачные попытки (архив)
related_mods/           — SCore/NPCCore (не используются)
deploy.ps1              — деплой
start-all.bat           — запуск bot-server + Piper
