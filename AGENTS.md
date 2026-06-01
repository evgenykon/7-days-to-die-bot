# AGENTS.md - AI Assistant Guide

## Project Overview

**CompanionBot** — a mod for 7 Days to Die (v1.0 stable) that adds AI companion NPCs. The companion follows the player, attacks zombies and hostile animals, and teleports back when too far away.

Built as a HarmonyX mod using C# (.NET 4.8) with XML entity configuration.

## Tech Stack

- **Language:** C# (.NET Framework 4.8)
- **Modding framework:** HarmonyX (bundled with game at `Mods/0_TFP_Harmony/`)
- **Game engine:** Unity 2022.3.62f2
- **Build tool:** dotnet CLI

## Project Structure

```
ai-7d2d/
├── Directory.Build.props          # Game path config (gitignored)
├── Directory.Build.props.example  # Template for Directory.Build.props
├── .gitignore
├── README.md
├── AGENTS.md
├── TODO.md
└── CompanionBot/
    ├── ModInfo.xml                # Mod metadata
    ├── Config/
    │   └── entityclasses.xml      # NPC entity definitions (XML patching)
    ├── Scripts/
    │   └── CompanionBot.cs        # Harmony patches + AI logic
    ├── CompanionBot.csproj        # Build project
    ├── build.bat                  # Build + install script
    └── README.md
```

## Build Commands

```powershell
# Build and install mod to game directory
dotnet build CompanionBot/CompanionBot.csproj -c Release

# Or use the batch script (reads GamePath from Directory.Build.props)
CompanionBot\build.bat
```

No linter or type checker is configured yet. The project uses standard C# conventions.

## Game Path Configuration

The path to 7 Days to Die is configured in `Directory.Build.props` (root of repo). This file is **gitignored**. Each developer copies `Directory.Build.props.example` and sets their own path:

```xml
<GamePath>F:\SteamLibrary\steamapps\common\7 Days To Die</GamePath>
```

All references in `.csproj` and `build.bat` use `$(GamePath)`.

## Key Game Paths (relative to GamePath)

| Resource | Path |
|----------|------|
| Game assemblies | `7DaysToDie_Data\Managed\` |
| Harmony | `Mods\0_TFP_Harmony\` |
| XML configs | `Data\Config\` |
| Entity classes | `Data\Config\entityclasses.xml` |
| Entity groups | `Data\Config\entitygroups.xml` |
| Utility AI | `Data\Config\utilityai.xml` |
| Items | `Data\Config\items.xml` |
| Mods directory | `Mods\` |

## Modding Conventions

### XML Patching
- Use `<configs>` root with `<append>`, `<set>`, `<remove>` operations
- XPath selectors: `xpath="/entity_classes"` etc.
- Entity classes use `extends` for inheritance
- Properties with `^` prefix are replaced via `<replace_properties>`

### C# / Harmony
- Implement `IModApi` as entry point (`InitMod` method)
- Use `[HarmonyPatch]` attributes for patching game classes
- Key game classes: `EntityAlive`, `EntityPlayer`, `EntityZombie`, `EntityNPC`, `EntityEnemyAnimal`, `GameManager`, `SdtdConsole`
- Logging: `Log.Out()`, `Log.Error()`
- Console output: `SdtdConsole.Instance.Output()`
- Entity spawning: `EntityFactory.CreateEntity()`

### Entity AI Tasks
- `AITask-N`: Behavior tasks (ApproachAndAttackTarget, Wander, Look, etc.)
- `AITarget-N`: Target selection (SetNearestEntityAsTarget, SetAsTargetIfHurt, etc.)
- Target data format: `class=EntityZombie,hearDist,seeDist`

## Code Style

- No comments unless explicitly requested
- Standard C# naming conventions (PascalCase for types/methods, camelCase for locals)
- Namespace: `CompanionBot`
- Harmony patch ID: `com.ai7d2d.companionbot`

## Important Notes

- EAC (Easy Anti-Cheat) must be disabled for mods to work
- The mod uses `SkipWithAntiCheat=true` in ModInfo.xml
- Game uses Unity's coordinate system (Y is up)
- Entity IDs are assigned by the game engine, not by the mod
- The mod currently supports single-player and private servers only
