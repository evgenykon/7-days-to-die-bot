# AGENTS.md — AI Assistant Guide

## Правила проекта

- **Никакого Harmony** — ни одного `[HarmonyPatch]`
- Базовая сущность — кастомный класс, наследующий `EntityZombie` (не `EntityPlayer`)
- Движение через `moveHelper.SetMoveTo()` — стандартный механизм зомби
- Регистрация entity в XML: `<property name="Class" value="..." />`
- Console commands через `ConsoleCmdAbstract`
- Node.js бот подключается к C# моду через TCP/JSON
- Файлы проекта — в `src/`
- C# .NET Framework 4.8, сборка через `dotnet build`
