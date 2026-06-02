# Session: CompanionBot v3 — финальная рабочая версия

## Архитектура

**Base class:** `EntityAlive` (vanilla) — полный пайплайн движения, без AI
**Mesh:** `@:Entities/Traders/Prefabs/TraderJoel.prefab` (AvatarNpcController)
**Движение:** `OnUpdateLive()` → `motion` + `SmoothFactor Lerp` + `OnUpdatePosition()` → `DefaultMoveEntity()`
**Поворот:** `Mathf.Atan2(motion.x, motion.z)` в `OnUpdatePosition()`
**Команды:** `spawncompanion`, `scc`, `sc`

## Почему не EntityTrader / EntityZombie / SCore / NPCCore

### EntityTrader
- `MoveEntityHeaded()` пустой — не двигается без SCore-патча
- Требует NPCInfo (иначе NRE в PostInit)
- SCore чинит, но тянет зависимости

### EntityZombie
- EModelBase NRE с non-zombie мешем
- Требует zombie-префаб

### SCore (0-SCore)
- EntityNPC / EntityAliveSDX — потенциально рабочие, но Harmony-патчи SCore конфликтуют
- SCore 2.6.58.744 + NPCCore 2.6.0 + Civilians 2.0.1.0 — entity classes не регистрируются (extends chain рвётся)
- **Решение:** не используем SCore вообще

### Мужской бот, не женский
- Все NPC-трейдеры с `AvatarNpcController` — мужские (Joel, Rekt, Bob, Hugh)
- `TraderJen` — женская, но использует `AvatarSDCSController` → краши с EntityAlive
- `playerFemale` — тоже SDCS
- **Женского бота с AvatarNpcController в ванилле нет**

## Файлы проекта

- `src/CompanionBot/CompanionEntity.cs` — `EntityAlive`, движение в `OnUpdateLive`/`OnUpdatePosition`
- `src/CompanionBot/ConsoleCommands.cs` — `spawncompanion` / `scc` / `sc` + `scc kill`
- `src/CompanionBot/CompanionBot.csproj` — .NET 4.8, ссылки на Assembly-CSharp + UnityEngine
- `src/CompanionBot/Config/entityclasses.xml` — `companionBot`, `extends="npcTraderTemplate"`
- `src/CompanionBot/Config/entitygroups.xml` — группа `CompanionBot`
- `src/CompanionBot/ModInfo.xml` — регистрация
- `Directory.Build.props` — путь к игре (GamePath)

## Статус

- ✅ **EntityAlive** — стабильно работает, двигается, поворачивается
- ✅ **TraderJoel** — модель загружается, AvatarNpcController анимации
- ✅ **scc / sc / spawncompanion** — спавн рядом с игроком
- ✅ **scc kill** — удаление всех ботов
- ⚠️ **Без SCore** — SCore + NPCCore несовместимы (entity classes не грузятся)
- ⚠️ **Мужской бот** — женской модели с AvatarNpcController нет
- ❌ **NPC в мире** — не используются (SCore удалён)

## Команды

```
scc                  — заспавнить бота
sc                   — то же самое
spawncompanion       — то же самое
scc kill / sc k      — убить всех ботов
```

## Сборка и деплой

```
dotnet build src\CompanionBot\CompanionBot.csproj
```
Автоматически копирует DLL + Config в `Mods/CompanionBot/`
