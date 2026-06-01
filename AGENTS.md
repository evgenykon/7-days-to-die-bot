# AGENTS.md - AI Assistant Guide

## Project Overview

**CompanionBot** — a mod for 7 Days to Die (v1.0 stable) that adds AI companion NPCs. The companion follows the player, attacks zombies and hostile animals, and teleports back when too far away.

**Killer feature:** Companion communicates with player via local LLM (LM Studio). Supportive, respectful tone. Configurable gender. RAG builds knowledge base from gameplay events.

Built as a HarmonyX mod using C# (.NET 4.8) with XML entity configuration.

## Tech Stack

- **Language:** C# (.NET Framework 4.8)
- **Modding framework:** HarmonyX (bundled with game at `Mods/0_TFP_Harmony/`)
- **Game engine:** Unity 2022.3.62f2
- **Build tool:** dotnet CLI
- **LLM:** LM Studio (local, OpenAI-compatible API)
- **RAG:** Local vector store (JSON + embeddings via LM Studio)

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
    │   ├── entityclasses.xml      # NPC entity definitions (XML patching)
    │   └── llm_config.json        # LLM endpoint, model, personality settings
    ├── Scripts/
    │   ├── CompanionBot.cs        # Harmony patches + main mod entry
    │   ├── CompanionManager.cs    # Companion state management (Follow/Stay/Guard)
    │   ├── ConsoleCommands.cs     # Console commands (cb spawn/follow/stay/guard/dismiss/status/heal/equip/stats)
    │   ├── CombatSystem.cs        # Combat AI (target priority, retreat, line-of-fire checks, melee combos, dodge/strafe, ammo management, damage feedback, statistics)
    │   ├── SaveSystem.cs          # Save/load companions across game sessions
    │   ├── LLMClient.cs           # HTTP client for LM Studio API
    │   ├── ChatSystem.cs          # Context-aware messaging
    │   ├── RAGSystem.cs           # Vector store + semantic search
    │   └── MemoryLogger.cs        # Game event capture for RAG
    ├── Data/
    │   ├── memories.json          # Persistent RAG memory (gitignored)
    │   └── companions_save.json   # Companion save data (gitignored)
    ├── CompanionBot.csproj        # Build project
    ├── build.bat                  # Build script
    ├── install.bat                # Install mod to game directory
    └── README.md
```

## Build Commands

```powershell
# Build the mod
dotnet build CompanionBot/CompanionBot.csproj -c Release

# Install mod to game directory (reads GamePath from Directory.Build.props)
CompanionBot\install.bat

# Or use the batch script (build + install)
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

## LLM Integration Architecture

### Overview
Companion uses local LLM via LM Studio (OpenAI-compatible API) to generate contextual, supportive messages. RAG system builds knowledge base from gameplay events.

### Components

**LLMClient.cs**
- HTTP client for LM Studio API (`http://localhost:1234/v1/`)
- Endpoints: `/chat/completions`, `/embeddings`
- Async requests (don't block game thread)
- Request queue with rate limiting
- Error handling and fallback

**ChatSystem.cs**
- Generates messages based on game context
- System prompt with personality, gender, tone
- Gender-aware speech (Russian: "сделал"/"сделала")
- Context injection (current event, relevant memories from RAG)
- Message delivery via game chat (`SdtdConsole.Instance.Output()`)

**RAGSystem.cs**
- Local vector store (JSON file: `Data/memories.json`)
- Embedding generation via LM Studio `/v1/embeddings`
- Semantic search for relevant memories
- Memory categories: combat, exploration, crafting, relationships
- Memory decay (older = less relevant)
- Persistent across game sessions

**MemoryLogger.cs**
- Captures game events via Harmony patches
- Event types: kills, deaths, crafting, building, loot, horde nights
- Feeds events to RAGSystem for indexing
- Batches events to reduce API calls

### Configuration (llm_config.json)
```json
{
  "endpoint": "http://localhost:1234/v1",
  "model": "local-model",
  "temperature": 0.7,
  "max_tokens": 150,
  "personality": {
    "tone": "supportive, respectful, encouraging",
    "verbosity": "normal",
    "humor": "contextual"
  },
  "rate_limit": {
    "messages_per_minute": 3,
    "cooldown_seconds": 20
  }
}
```

### Gender Configuration
Set in XML entity (`entityclasses.xml`):
```xml
<property name="Gender" value="female"/>
```

Affects system prompt and speech patterns.

### Communication Flow
1. Game event occurs (e.g., player kills zombie)
2. MemoryLogger captures event
3. RAGSystem indexes event (generates embedding, stores)
4. ChatSystem checks if message should be sent
5. RAGSystem retrieves relevant memories
6. LLMClient generates response with context
7. Message sent to player via game chat

### Tone Guidelines
- Always supportive and respectful
- No rudeness, insults, or toxic behavior
- Encouraging during failures
- Celebrating successes
- Contextual humor (appropriate, not offensive)
- Build relationship over time (remember player preferences)
