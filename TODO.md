# TODO - CompanionBot Development Plan

## Phase 1: Core Stability (Current)

- [x] Basic companion entity (XML definition)
- [x] Follow player behavior
- [x] Attack zombies and hostile animals
- [x] Teleport when too far from player
- [x] Health regeneration
- [x] Armed companion variant
- [x] Configurable game path
- [ ] Fix potential null reference on entity death cleanup
- [ ] Handle companion persistence across game saves
- [ ] Test with multiple players on private server
- [ ] Verify companion doesn't aggro on friendly players

## Phase 2: Player Commands

- [ ] `cb spawn` — spawn companion at player position
- [ ] `cb dismiss` — remove companion
- [ ] `cb stay` — companion holds position
- [ ] `cb follow` — companion resumes following
- [ ] `cb guard` — companion patrols a small area
- [ ] `cb heal` — use medkit on companion
- [ ] `cb equip <item>` — give weapon/armor to companion
- [ ] `cb status` — show companion HP, weapon, distance
- [ ] Keybind support for quick commands

## Phase 3: Combat Improvements

- [ ] Ranged attack AI (proper gun usage, aiming, reloading)
- [ ] Melee attack combos and stagger
- [ ] Dodge/strafe behavior during combat
- [ ] Target priority system (closest threat, strongest threat, player's target)
- [ ] Retreat behavior when low HP
- [ ] Area-of-effect awareness (don't shoot through player)
- [ ] Companion damage feedback (visual + sound)
- [ ] Kill tracking / statistics

## Phase 4: Inventory & Equipment

- [ ] Companion inventory (backpack)
- [ ] Equip/unequip weapons via UI or commands
- [ ] Armor slots (head, chest, legs, feet)
- [ ] Ammo management (consume ammo from inventory)
- [ ] Auto-use healing items when injured
- [ ] Loot pickup (optional, configurable)
- [ ] Item durability tracking

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
