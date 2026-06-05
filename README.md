# CompanionBot

AI Companion for 7 Days to Die.

## Установка

1. `dotnet build src\CompanionBot\CompanionBot.csproj` — сборка + автодеплой в `Mods/CompanionBot/`
2. Запустить игру без EAC

## Команды

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

## Почему не...?

См. `AGENTS.md` — список того, что пробовали и что не сработало.
