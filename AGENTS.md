# AGENTS.md — AI Assistant Guide

## Обещание

Если EntityZombie с `moveHelper.SetMoveTo()` не двигается — я больше не скачиваю SCore с GitHub.

## Что пробовал — не сработало

### EntityZombie
- NRE в EModelBase — zombie model нужен, для humanoid не подходит
- `moveHelper.SetMoveTo()` не двигает без AI-менеджера (EntityZombie его не вызывает)

### EntityTrader
- `MoveEntityHeaded()` пустой — не двигается без Harmony-патча
- `PostInit()` требует NPCInfo — NRE если нет
- SCore чинит через Harmony (копирует `DefaultMoveEntity` в `MoveEntityHeaded`), но тянет зависимости
- **Вердикт:** без SCore не работает

### SCore (0-SCore) + NPCCore (0-XNPCCore) + Civilians pack
- SCore 2.6.58.744 — установлен, Harmony-патчи работают
- NPCCore 2.6.0 — XML не в Config/ лежал (починили), но entity classes всё равно не регистрируются
- Civilians 2.0.1.0 — extends="npcAdvancedClubTemplate" не находит предка (цепочка рвётся)
- **Вердикт:** несовместимость версий SCore / NPCCore / game. Удалены.

### Женский бот
- TraderJen — AvatarSDCSController, краши с EntityAlive (требует EModelSDCS)
- playerFemale — тоже AvatarSDCSController
- Других женских моделей с AvatarNpcController в ванилле нет
- **Вердикт:** женского бота не сделать без кастомной модели

### Анимация ходьбы
- TraderJoel — AvatarNpcController, но нет locomotion в Animator Controller (только idle/talk)
- Zombie модели — есть анимация, но EModelBase NRE
- **Вердикт:** ходьба без анимации, фикс только через замену модели

## Что работает

- **EntityAlive** — стабильный базовый класс, полный пайплайн движения
- **TraderJoel.prefab** — загружается, AvatarNpcController, без анимации ходьбы
- **motion + DefaultMoveEntity** — надёжное движение без moveHelper/AI
- **scc / sc / spawncompanion** — спавн рядом с игроком
- **scc kill** — удаление всех ботов
- **IModApi (BotModInit)** — автозапуск HTTP сервера при старте мода
- **HTTP сервер** (порт 9090) — /health, /status, /chat, /follow, /stop
- **Bot Server** (порт 9091) — Node.js прокси к игре, Docker
- **PiperServer** (порт 9092) — C# .NET 4.8, TTS через piper.exe, запуск через run.bat
- **stop.bat / taskkill** — остановка PiperServer

## Первый запуск (setup)

1. `powershell -ExecutionPolicy Bypass -File deploy.ps1`
2. Запускать игру — HTTP сервер стартует автоматически (IModApi)
   - Игра слушает `0.0.0.0:9090` (нативно через TcpListener, не HttpListener)
3. `.\start-all.bat` — запускает Bot Server (Docker) + PiperServer
   - Bot Server: порт 9091
   - PiperServer: порт 9092

## Архитектура

```
Игра (C# мод, порт 9090, TcpListener) ← Docker → Bot Server (Node.js, порт 9091) ← → LLM API
   ↑
   └── PiperServer (C#, порт 9092) → piper.exe → TTS (колонки)
```

## API (игра, порт 9090)

```bash
# Отправить сообщение в чат (sender=имя, message=текст)
curl -X POST http://localhost:9090/chat -d '{"sender":"Quinn","message":"hello"}'

# Бот следует за игроком
curl -X POST http://localhost:9090/follow

# Бот стоп
curl -X POST http://localhost:9090/stop

# Статус (позиция игрока, кол-во ботов)
curl http://localhost:9090/status

# Здоровье
curl http://localhost:9090/health
```

## API (Bot Server, порт 9091)

```bash
# Прокси к игре
curl -X POST http://localhost:9091/chat -d '{"message":"hello"}'
curl -X POST http://localhost:9091/send -d '{"sender":"Quinn","message":"reply"}'
curl -X POST http://localhost:9091/follow
curl -X POST http://localhost:9091/stop
```

## API (PiperServer, порт 9092)

```bash
# Проверка здоровья
curl http://localhost:9092/ping

# Синтез речи (WAV → колонки)
curl -X POST http://localhost:9092/speak -d '{"text":"привет"}'

# Параметры голоса (опционально)
curl -X POST http://localhost:9092/speak -d '{"text":"hello","length_scale":1.2,"noise_scale":0.6,"noise_w":0.9}'

# Остановить PiperServer
taskkill /f /im PiperServer.exe
```

## Мои ошибки (не повторять)

- **ПРОВАЛ версии 2: потратил 30+ попыток и 2 часа на то что должно работать с первого раза.** Каждая версия мода что-то ломала — NRE, краш, спам в консоль. Надо было начинать с минимального работающего прототипа и НЕ менять entity class посредине.

- Нельзя писать XML не заглянув в ванильные файлы игры — `Data/Config/` показывает реальные entity class names и xpath
- Нельзя гадать xpath — в entitygroups.xml корень `/entitygroups`, а не `/entity_groups`
- Нельзя предлагать флаги запуска не прочитав логи — игра висела на EOS, а не на Discord
- Нельзя делать всё сразу — начинать надо с малого и проверять каждый шаг
- Нельзя гадать и пересобирать по 10 минут — надо сначала читать ванильные файлы/код игры
- Нельзя запускать игру из PowerShell — сбивается рабочая директория, бандлы не грузятся. Запускать только через Steam или launch.bat
- Нельзя убивать процесс игры без спроса — игрок может уже быть в игре. Всегда спрашивай.
- Нельзя фиксить код после "готово, запускай" — все правки должны быть сделаны ДО. Иначе убиваешь игру игрока.
- Нельзя скачивать SCore/NPCCore с GitHub не проверив совместимость версий — убитый вечер гарантирован

- **Не коммитить без разрешения** — ни один commit не делается без явной команды пользователя.

## Правила проекта

- **Никакого Harmony** — ни одного `[HarmonyPatch]`
- Базовая сущность — `EntityAlive` (не EntityZombie, не EntityTrader, не EntityNPC)
- Движение через `motion` + `DefaultMoveEntity()` в `OnUpdatePosition()` — без AI/мoveHelper
- Регистрация entity в XML: `<property name="Class" value="..." />`
- Console commands через `ConsoleCmdAbstract`
- Node.js бот подключается к C# моду через TCP/JSON
- Файлы проекта — в `src/`
- C# .NET Framework 4.8, сборка через `dotnet build`
- Никаких внешних зависимостей (SCore, Harmony) — только ванильные Assembly-CSharp и UnityEngine

## Сессия 2026-06-05 — полный рефакторинг

### Что сделано
- **Разбил server.js на модули**: `lib/http.js`, `lib/llm.js`, `lib/game.js`, `lib/piper.js`, `lib/relationship.js`, `lib/memory.js`, `lib/prompt.js`
- **Система отношений**: 4 уровня (`незнакомы` → `tentative` → `trusting` → `friendly`), авто-прогресс по количеству сообщений, сентимент (`loyal`/`angry`/`rejecting`)
- **RAG память**: факты об игроке извлекаются через `[ФАКТ: ключ=значение]` в ответах LLM, хранятся в `/data/memory.json`
- **История диалога**: последние 20 сообщений сохраняются в `/data/history.json` и отправляются с каждым запросом
- **Персистентность**: volume `./data:/data` в docker-compose.yml
- **Piper TTS в Docker**: Linux-контейнер (bookworm-slim, glibc), возвращает base64 WAV
- **OpenRouter**: заменил локальную LM Studio на API (gpt-4o-mini). Ключ в `.env`
- **HTTPS fix**: `http.js` теперь использует `https` модуль для https URL
- **URL fix**: `new URL("./path", base)` вместо `new URL("/path", base)` (абсолютный путь заменял base path)
- **Биография Квин**: архитектор-дизайнер, киберпанк, потеря группы, чувство вины, страх доверия, стеснение
- **События урона**: `bot_damaged` + `player_damaged` шлются на `/event`, триггерят LLM
- **Улучшенное движение**: прыжки через препятствия, спуск за игроком
- **Chat NRE fix**: `SendChatMessage` — безопасный null-check через ChatHistory (GameManager.GameMessage не существует в V2.6)
- **Чистка**: удалил C# PiperServer, Windows DLL/binary, дубликаты espeak-ng-data

### Архитектура
```
Игра (C# мод) → TCP/JSON → Bot Server (Node.js, Docker, порт 9091) → OpenRouter API (gpt-4o-mini)
                                      ↓
                              Piper Server (Docker, порт 9092) → piper Linux binary → base64 WAV
                                      ↓
                              Игра (C# мод) → /play-wav (base64) → SoundPlayer.PlaySync()
```

### Файловая структура bot-server
```
bot-server/
├── server.js          # HTTP роутер, processChat
├── Dockerfile         # node:22-alpine
├── docker-compose.yml # bot-server + piper-server
├── .env               # OPENROUTER_API_KEY, LLM_MODEL
├── .gitignore
├── lib/
│   ├── http.js        # httpPost (http + https), parseBody, json
│   ├── llm.js         # callLLM (OpenRouter / LM Studio)
│   ├── game.js        # sendChat, sendPlayWav и т.д.
│   ├── piper.js       # speak (Piper Docker)
│   ├── relationship.js # уровни отношений, сентимент
│   ├── memory.js      # RAG факты + история диалога
│   └── prompt.js      # динамический system prompt
└── data/              # volume, персистентные файлы
```

### Важные замечания
- `.env` в .gitignore — ключи не коммитятся
- `data/*.json` в .gitignore — состояние не коммитится
- Piper TTS только в Docker (Linux), Windows binaries не нужны
- При смене модели в OpenRouter — поменять `LLM_MODEL` в `.env`
- LN Studio больше не нужен (OpenRouter API вместо локальной модели)
- Для билда C# мода: `dotnet build src/CompanionBot -c Release` (игра должна быть закрыта, иначе DLL locked)
