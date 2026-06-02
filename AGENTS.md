# AGENTS.md — AI Assistant Guide

## Обещание

Если EntityZombie с `moveHelper.SetMoveTo()` не двигается — считай что меня больше нет.

## Мои ошибки (не повторять)

- Нельзя писать XML не заглянув в ванильные файлы игры — `Data/Config/` показывает реальные entity class names и xpath
- Нельзя гадать xpath — в entitygroups.xml корень `/entitygroups`, а не `/entity_groups`
- Нельзя предлагать флаги запуска не прочитав логи — игра висела на EOS, а не на Discord
- Нельзя делать всё сразу — начинать надо с малого и проверять каждый шаг
- Нельзя гадать и пересобирать по 10 минут — надо сначала читать ванильные файлы/код игры
- Нельзя запускать игру из PowerShell — сбивается рабочая директория, бандлы не грузятся. Запускать только через Steam или launch.bat

## Правила проекта

- **Никакого Harmony** — ни одного `[HarmonyPatch]`
- Базовая сущность — кастомный класс, наследующий `EntityZombie` (не `EntityPlayer`)
- Движение через `moveHelper.SetMoveTo()` — стандартный механизм зомби
- Регистрация entity в XML: `<property name="Class" value="..." />`
- Console commands через `ConsoleCmdAbstract`
- Node.js бот подключается к C# моду через TCP/JSON
- Файлы проекта — в `src/`
- C# .NET Framework 4.8, сборка через `dotnet build`
