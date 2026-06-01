# TODO - CompanionBot Development Plan

## KILLER FEATURE: LLM-Powered Communication

Companion communicates with player via local LLM (LM Studio). Supportive, respectful tone. Configurable gender. RAG builds knowledge base from gameplay.

### Core LLM Integration
- [ ] LLMClient — HTTP client for LM Studio OpenAI-compatible API
- [ ] Config file (llm_config.json) — endpoint, model, temperature, max_tokens
- [ ] System prompt with personality, gender, tone guidelines
- [ ] ChatSystem — context-aware messages to player via game chat
- [ ] Gender configuration in XML entity (`Gender` property: male/female)
- [ ] Gender-aware speech patterns (Russian: "сделал"/"сделала", etc.)
- [ ] Rate limiting (don't spam player, cooldown between messages)
- [ ] Fallback phrases when LLM is unavailable

### RAG System (Retrieval-Augmented Generation)
- [ ] MemoryLogger — capture game events (kills, deaths, crafting, loot, locations)
- [ ] Event types: combat, exploration, crafting, building, trading, horde nights
- [ ] Vector store (local JSON file with embeddings)
- [ ] Embedding generation via LM Studio `/v1/embeddings` endpoint
- [ ] Semantic search for relevant memories
- [ ] Memory decay (older memories less relevant)
- [ ] Memory summarization (compress old events into summaries)
- [ ] Persistent memory across game sessions (save/load)
- [ ] Memory categories (combat, relationships, locations, items)

### Communication Triggers
- [ ] On player kill (praise, encouragement)
- [ ] On player death (comfort, support)
- [ ] On companion kill (modest pride, teamwork)
- [ ] On horde night start (alert, encouragement)
- [ ] On horde night end (celebration, relief)
- [ ] On finding rare loot (excitement, congratulations)
- [ ] On crafting something (interest, admiration)
- [ ] On building/upgrading base (approval, suggestions)
- [ ] On low HP (concern, offer help)
- [ ] On player low HP (urgency, care)
- [ ] On blood moon (determination, solidarity)
- [ ] On day start (greeting, plans for the day)
- [ ] On night start (caution, readiness)
- [ ] Idle chatter (random supportive comments, observations)
- [ ] Player-initiated dialogue (respond to player chat messages)

### Tone & Personality
- [ ] Always supportive and respectful
- [ ] No rudeness, insults, or toxic behavior
- [ ] Encouraging during failures
- [ ] Celebrating successes
- [ ] Contextual humor (appropriate, not offensive)
- [ ] Personality traits configurable (brave, cautious, cheerful, serious)
- [ ] Relationship building (remember player preferences, playstyle)

### Technical
- [ ] Async HTTP requests (don't block game thread)
- [ ] Request queue (batch messages if LLM is slow)
- [ ] Error handling (LLM unavailable, timeout, malformed response)
- [ ] Logging all LLM interactions for debugging
- [ ] Performance monitoring (latency, token usage)
- [ ] Configurable verbosity (silent, normal, chatty)

## Phase 1: Core Stability (COMPLETE)

- [x] Basic companion entity (XML definition)
- [x] Follow player behavior
- [x] Attack zombies and hostile animals
- [x] Teleport when too far from player
- [x] Health regeneration
- [x] Armed companion variant
- [x] Configurable game path
- [x] Fix potential null reference on entity death cleanup
- [x] Handle companion persistence across game saves
- [x] Support multiple companions for different players
- [x] Verify companion doesn't aggro on friendly players

## Phase 2: Player Commands (COMPLETE)

- [x] `cb spawn` — spawn companion at player position (with type selection)
- [x] `cb dismiss` — remove companion
- [x] `cb stay` — companion holds position
- [x] `cb follow` — companion resumes following
- [x] `cb guard` — companion patrols a small area (configurable radius)
- [x] `cb status` — show companion HP, weapon, distance, state
- [x] `cb heal` — use medkit on companion
- [x] `cb equip <item>` — give weapon/armor to companion
- [x] `cb stats` — show combat statistics
- [ ] Keybind support for quick commands

## Phase 3: Combat Improvements (COMPLETE)

- [x] Target priority system (closest threat, strongest threat, player's target)
- [x] Retreat behavior when low HP
- [x] Area-of-effect awareness (don't shoot through player)
- [x] Kill tracking / statistics
- [x] Ranged attack AI (ammo checking, reload detection)
- [x] Melee attack combos and stagger
- [x] Dodge/strafe behavior during combat
- [x] Companion damage feedback (logging + stats tracking)

## Phase 4: Inventory & Equipment (COMPLETE)

- [x] Companion inventory (backpack)
- [x] Equip/unequip weapons via UI or commands
- [x] Armor slots (head, chest, legs, feet)
- [x] Ammo management (consume ammo from inventory)
- [x] Auto-use healing items when injured
- [x] Loot pickup (optional, configurable)
- [x] Item durability tracking

## Phase 5: Advanced AI Behaviors

- [ ] Pathfinding improvements (ladder climbing, door opening)
- [ ] Patrol mode (configurable waypoints)
- [ ] Guard mode (defend specific area/block)
- [ ] Escort mode (stay close, prioritize player safety)
- [ ] Scout mode (explore ahead, report enemies)
- [ ] Day/night behavior (rest during day, alert at night)
- [ ] Horde night behavior (defensive positioning)
- [ ] Reaction to blood moon (aggressive defense)

## Phase 6: Companion Customization

- [ ] Multiple companion archetypes (melee, ranged, medic, engineer)
- [ ] Custom names
- [ ] Appearance selection (model, clothing)
- [ ] Skill tree / leveling system
- [ ] XP gain from kills
- [ ] Perk system (faster reload, more HP, better aim)
- [ ] Companion traits (brave, cautious, aggressive, passive)

## Phase 7: UI & HUD

- [ ] Companion health bar on HUD
- [ ] Minimap icon for companion
- [ ] Companion status panel (HP, ammo, buffs/debuffs)
- [ ] Radial menu for quick commands
- [ ] Notification system (companion under attack, low HP, out of ammo)
- [ ] XUi window integration for inventory management

## Phase 8: Multi-Companion Support

- [ ] Spawn multiple companions
- [ ] Squad management (assign roles)
- [ ] Formation system (line, wedge, circle)
- [ ] Companion-to-companion interaction
- [ ] Shared inventory pool
- [ ] Squad commands (all follow, all guard, all attack)

## Phase 9: Integration & Polish

- [ ] Save/load companion state with world save
- [ ] Companion death consequences (configurable: respawn, permadeath, cooldown)
- [ ] Compatibility with popular mods (Darkness Falls, Undead Legacy)
- [ ] Config file for all tunable parameters
- [ ] Localization support (EN, RU)
- [ ] Performance optimization (LOD for AI updates)
- [ ] Comprehensive error handling and logging
- [ ] Unit tests for AI logic

## Phase 10: Stretch Goals

- [ ] Companion dialogue system (contextual voice lines)
- [ ] Quest system (companion gives quests)
- [ ] Companion crafting (auto-craft basic items)
- [ ] Base building assistance (auto-repair, auto-upgrade)
- [ ] Farming automation (plant, water, harvest)
- [ ] Vehicle interaction (ride along, drive)
- [ ] Animal companion variant (dog, wolf)
- [ ] Drone companion variant (flying, ranged support)
