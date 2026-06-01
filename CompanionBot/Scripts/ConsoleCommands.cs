using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public class ConsoleCmdCB : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new string[] { "cb" };
        }

        public override string getDescription()
        {
            return "CompanionBot commands: cb spawn/follow/stay/guard/dismiss/status";
        }

        public override string GetHelp()
        {
            return @"Usage:
  cb spawn [type]     - Spawn companion (types: male, female, armed, armedfemale)
  cb follow           - Companion follows you
  cb stay             - Companion stays at current position
  cb guard [radius]   - Companion guards current area (default radius: 10m)
  cb dismiss          - Remove companion
  cb status           - Show companion status
  cb heal             - Use first aid kit on companion
  cb equip <item>     - Give weapon/item to companion
  cb unequip <slot>   - Unequip item from slot (weapon/head/chest/legs/feet)
  cb inventory        - Show companion inventory
  cb autopickup [on/off] - Toggle auto loot pickup
  cb stats            - Show combat statistics
  
Advanced AI (Phase 5):
  cb patrol add       - Add current position as patrol waypoint
  cb patrol clear     - Clear all patrol waypoints
  cb patrol start     - Start patrol mode
  cb escort [dist]    - Escort mode with distance (default: 5m)
  cb scout [radius]   - Scout mode with radius (default: 50m)
  cb horde            - Set horde defense position
  
Customization (Phase 6):
  cb name <name>      - Set companion name
  cb class <class>    - Set class (soldier/medic/engineer/scout/guardian)
  cb personality <trait> - Set personality (aggressive/defensive/balanced/cautious/brave)
  cb profile          - Show companion profile
  
Squad (Phase 7):
  cb squad add        - Add companion to squad
  cb squad remove     - Remove companion from squad
  cb squad formation <type> - Set formation (line/wedge/circle/column/free)
  cb squad all follow - All squad members follow
  cb squad all guard  - All squad members guard
  cb squad all attack - All squad members attack current target
  cb squad status     - Show squad status
  
Multi-Companion (Phase 8):
  cb role <role>      - Assign role (leader/assault/support/medic/sniper/tank/scout)
  cb shared [on/off]  - Toggle shared inventory
  cb distribute <type> - Distribute items (ammo/healing)
  
Integration (Phase 9):
  cb death <type>     - Set death consequence (respawn/permadeath/cooldown)
  cb config reset     - Reset configuration to defaults
  cb language <lang>  - Set language (en/ru)
  
Advanced Features (Phase 10):
  cb quest list       - List available and active quests
  cb quest accept <id> - Accept quest
  cb craft [item]     - Start crafting (no args = show recipes)
  cb build <action>   - Building tasks (repair/upgrade/cancel)
  cb animal spawn <type> - Spawn animal companion (dog/wolf/bear)
  cb animal feed <item> - Feed animal companion
  cb drone spawn      - Deploy drone
  cb drone mode <mode> - Set drone mode (follow/patrol/scout/attack/support)
  cb drone recharge   - Recharge drone battery";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count == 0)
            {
                OutputHelp(_senderInfo);
                return;
            }

            var command = _params[0].ToLower();
            var player = GetPlayer(_senderInfo);

            if (player == null)
            {
                Output(_senderInfo, "Error: Player not found");
                return;
            }

            switch (command)
            {
                case "spawn":
                    HandleSpawn(_params, _senderInfo, player);
                    break;
                case "follow":
                    HandleFollow(_senderInfo, player);
                    break;
                case "stay":
                    HandleStay(_senderInfo, player);
                    break;
                case "guard":
                    HandleGuard(_params, _senderInfo, player);
                    break;
                case "dismiss":
                    HandleDismiss(_senderInfo, player);
                    break;
                case "status":
                    HandleStatus(_senderInfo, player);
                    break;
                case "heal":
                    HandleHeal(_senderInfo, player);
                    break;
                case "equip":
                    HandleEquip(_params, _senderInfo, player);
                    break;
                case "unequip":
                    HandleUnequip(_params, _senderInfo, player);
                    break;
                case "inventory":
                    HandleInventory(_senderInfo, player);
                    break;
                case "autopickup":
                    HandleAutoPickup(_params, _senderInfo, player);
                    break;
                case "stats":
                    HandleStats(_senderInfo, player);
                    break;
                case "patrol":
                    HandlePatrol(_params, _senderInfo, player);
                    break;
                case "escort":
                    HandleEscort(_params, _senderInfo, player);
                    break;
                case "scout":
                    HandleScout(_params, _senderInfo, player);
                    break;
                case "horde":
                    HandleHorde(_senderInfo, player);
                    break;
                case "name":
                    HandleName(_params, _senderInfo, player);
                    break;
                case "class":
                    HandleClass(_params, _senderInfo, player);
                    break;
                case "personality":
                    HandlePersonality(_params, _senderInfo, player);
                    break;
                case "profile":
                    HandleProfile(_senderInfo, player);
                    break;
                case "squad":
                    HandleSquad(_params, _senderInfo, player);
                    break;
                case "role":
                    HandleRole(_params, _senderInfo, player);
                    break;
                case "shared":
                    HandleShared(_params, _senderInfo, player);
                    break;
                case "distribute":
                    HandleDistribute(_params, _senderInfo, player);
                    break;
                case "death":
                    HandleDeath(_params, _senderInfo, player);
                    break;
                case "config":
                    HandleConfig(_params, _senderInfo, player);
                    break;
                case "language":
                    HandleLanguage(_params, _senderInfo, player);
                    break;
                case "quest":
                    HandleQuest(_params, _senderInfo, player);
                    break;
                case "craft":
                    HandleCraft(_params, _senderInfo, player);
                    break;
                case "build":
                    HandleBuild(_params, _senderInfo, player);
                    break;
                case "animal":
                    HandleAnimal(_params, _senderInfo, player);
                    break;
                case "drone":
                    HandleDrone(_params, _senderInfo, player);
                    break;
                default:
                    Output(_senderInfo, $"Unknown command: {command}");
                    OutputHelp(_senderInfo);
                    break;
            }
        }

        private void HandleSpawn(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            string entityType = "companionbot";
            string gender = "male";

            if (_params.Count > 1)
            {
                var type = _params[1].ToLower();
                switch (type)
                {
                    case "male":
                        entityType = "companionbot";
                        gender = "male";
                        break;
                    case "female":
                        entityType = "companionbotfemale";
                        gender = "female";
                        break;
                    case "armed":
                        entityType = "companionbotarmed";
                        gender = "male";
                        break;
                    case "armedfemale":
                    case "femalearmed":
                        entityType = "companionbotfemalearmed";
                        gender = "female";
                        break;
                    default:
                        Output(_senderInfo, $"Unknown companion type: {type}");
                        return;
                }
            }

            var existingCompanion = CompanionManager.GetCompanionByOwner(player);
            if (existingCompanion != null && !existingCompanion.Entity.IsDead())
            {
                Output(_senderInfo, "You already have a companion. Use 'cb dismiss' first.");
                return;
            }

            try
            {
                var spawnPos = player.position + new Vector3(2, 0, 2);
                var entity = GameApi.CreateEntity(entityType, spawnPos);
                if (entity == null)
                {
                    Output(_senderInfo, $"Failed to create entity: {entityType}");
                    return;
                }
                int entityId = entity.entityId;

                if (entityId > 0)
                {
                    var companion = GameManager.Instance.World.GetEntity(entityId) as EntityAlive;
                    if (companion != null)
                    {
                        CompanionManager.RegisterCompanion(companion, player, gender);
                        Output(_senderInfo, $"Companion spawned! Type: {entityType}, ID: {entityId}");

                        if (ModMain.Chat != null)
                        {
                            _ = ModMain.Chat.SendMessage("spawn", "Компаньон появился рядом с игроком");
                        }
                    }
                }
                else
                {
                    Output(_senderInfo, "Failed to spawn companion");
                }
            }
            catch (Exception ex)
            {
                Output(_senderInfo, $"Error: {ex.Message}");
                Log.Error($"[CompanionBot] Spawn error: {ex.Message}");
            }
        }

        private void HandleFollow(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            CompanionManager.SetState(companion.Entity.entityId, CompanionState.Follow);
            Output(_senderInfo, "Companion will follow you");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("command_follow", "Компаньон получил команду следовать за игроком");
            }
        }

        private void HandleStay(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            CompanionManager.SetState(companion.Entity.entityId, CompanionState.Stay);
            Output(_senderInfo, $"Companion will stay at position {companion.Entity.position}");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("command_stay", "Компаньон получил команду оставаться на месте");
            }
        }

        private void HandleGuard(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            float radius = 10f;
            if (_params.Count > 1 && float.TryParse(_params[1], out float parsedRadius))
            {
                radius = Math.Max(5f, Math.Min(50f, parsedRadius));
            }

            var guardPos = companion.Entity.position;
            CompanionManager.SetGuardPosition(companion.Entity.entityId, guardPos, radius);
            Output(_senderInfo, $"Companion will guard area at {guardPos} with radius {radius}m");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("command_guard", $"Компаньон получил команду охранять территорию радиусом {radius}м");
            }
        }

        private void HandleDismiss(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            try
            {
                int entityId = companion.Entity.entityId;
                GameManager.Instance.World.RemoveEntity(entityId, (EnumRemoveEntityReason)2);
                CompanionManager.UnregisterCompanion(entityId);
                Output(_senderInfo, "Companion dismissed");

                if (ModMain.Chat != null)
                {
                    _ = ModMain.Chat.SendMessage("dismiss", "Компаньон покинул игрока");
                }
            }
            catch (Exception ex)
            {
                Output(_senderInfo, $"Error dismissing companion: {ex.Message}");
                Log.Error($"[CompanionBot] Dismiss error: {ex.Message}");
            }
        }

        private void HandleStatus(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            var entity = companion.Entity;
            var distance = Vector3.Distance(player.position, entity.position);
            var uptime = DateTime.Now - companion.SpawnTime;

            Output(_senderInfo, "=== Companion Status ===");
            Output(_senderInfo, $"ID: {entity.entityId}");
            Output(_senderInfo, $"Gender: {companion.Gender}");
            Output(_senderInfo, $"Health: {entity.Health}/{entity.GetMaxHealth()}");
            Output(_senderInfo, $"State: {companion.State}");
            Output(_senderInfo, $"Distance: {distance:F1}m");
            Output(_senderInfo, $"Position: {entity.position}");
            Output(_senderInfo, $"Uptime: {uptime.Hours}h {uptime.Minutes}m");

            if (companion.State == CompanionState.Guard)
            {
                Output(_senderInfo, $"Guard position: {companion.GuardPosition}");
                Output(_senderInfo, $"Guard radius: {companion.GuardRadius}m");
            }

            var stats = CombatSystem.GetStats(companion.Entity.entityId);
            if (stats != null)
            {
                Output(_senderInfo, $"Kills: {stats.TotalKills}");
            }
        }

        private void HandleHeal(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            var entity = companion.Entity;
            if (entity.Health >= entity.GetMaxHealth())
            {
                Output(_senderInfo, "Companion is already at full health");
                return;
            }

            var inventory = player.inventory;
            if (inventory == null)
            {
                Output(_senderInfo, "Cannot access player inventory");
                return;
            }

            string[] healingItems = { "medicalFirstAidKit", "medicalBandage", "medicalFirstAidBandage", "medicalPlasterCast", "medicalBloodDrawKit" };
            string foundItem = null;

            foreach (var itemName in healingItems)
            {
                var itemValue = ItemClass.GetItem(itemName);
                if (itemValue != null && inventory.GetItemCount(itemValue) > 0)
                {
                    foundItem = itemName;
                    break;
                }
            }

            if (foundItem == null)
            {
                Output(_senderInfo, "No healing items found in inventory (need: first aid kit, bandage, etc.)");
                return;
            }

            try
            {
                var itemValue = ItemClass.GetItem(foundItem);
                inventory.DecItem(itemValue, 1);

                int healAmount = foundItem == "medicalFirstAidKit" ? 100 : 50;
                entity.Health = Math.Min(entity.Health + healAmount, entity.GetMaxHealth());

                Output(_senderInfo, $"Healed companion for {healAmount} HP using {foundItem}");
                Output(_senderInfo, $"Companion health: {entity.Health}/{entity.GetMaxHealth()}");

                if (ModMain.Chat != null)
                {
                    _ = ModMain.Chat.SendMessage("heal", "Игрок вылечил компаньона");
                }
            }
            catch (Exception ex)
            {
                Output(_senderInfo, $"Error healing companion: {ex.Message}");
                Log.Error($"[CompanionBot] Heal error: {ex.Message}");
            }
        }

        private void HandleEquip(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb equip <item_name>");
                Output(_senderInfo, "Example: cb equip gunRifleT1AK47");
                return;
            }

            string itemName = _params[1];
            var inventory = player.inventory;

            if (inventory == null)
            {
                Output(_senderInfo, "Cannot access player inventory");
                return;
            }

            var itemValue = ItemClass.GetItem(itemName);
            if (itemValue == null || inventory.GetItemCount(itemValue) <= 0)
            {
                Output(_senderInfo, $"Item '{itemName}' not found in inventory");
                return;
            }

            try
            {
                inventory.DecItem(itemValue, 1);

                int entityId = companion.Entity.entityId;
                EquipmentSlot? slot = DetermineEquipmentSlot(itemName);

                if (slot.HasValue)
                {
                    if (InventorySystem.EquipItem(entityId, slot.Value, itemName))
                    {
                        Output(_senderInfo, $"Equipped companion with {itemName} in {slot.Value} slot");
                    }
                    else
                    {
                        inventory.AddItem(new ItemStack(itemValue, 1), out _);
                        Output(_senderInfo, $"Failed to equip {itemName}");
                        return;
                    }
                }
                else
                {
                    if (InventorySystem.AddItemToCompanion(entityId, itemName, 1))
                    {
                        Output(_senderInfo, $"Added {itemName} to companion inventory");
                    }
                    else
                    {
                        inventory.AddItem(new ItemStack(itemValue, 1), out _);
                        Output(_senderInfo, $"Companion inventory full, returned {itemName}");
                        return;
                    }
                }

                if (ModMain.Chat != null)
                {
                    _ = ModMain.Chat.SendMessage("equip", $"Игрок дал компаньону {itemName}");
                }
            }
            catch (Exception ex)
            {
                Output(_senderInfo, $"Error equipping companion: {ex.Message}");
                Log.Error($"[CompanionBot] Equip error: {ex.Message}");
            }
        }

        private EquipmentSlot? DetermineEquipmentSlot(string itemName)
        {
            itemName = itemName.ToLower();

            if (itemName.Contains("gun") || itemName.Contains("melee") || itemName.Contains("tool"))
                return EquipmentSlot.Weapon;

            if (itemName.Contains("helmet") || itemName.Contains("hat") || itemName.Contains("head"))
                return EquipmentSlot.Head;

            if (itemName.Contains("chest") || itemName.Contains("armor") || itemName.Contains("vest"))
                return EquipmentSlot.Chest;

            if (itemName.Contains("legs") || itemName.Contains("pants"))
                return EquipmentSlot.Legs;

            if (itemName.Contains("boots") || itemName.Contains("feet") || itemName.Contains("shoes"))
                return EquipmentSlot.Feet;

            return null;
        }

        private void HandleStats(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            var stats = CombatSystem.GetStats(companion.Entity.entityId);
            if (stats == null)
            {
                Output(_senderInfo, "No combat statistics available");
                return;
            }

            Output(_senderInfo, "=== Combat Statistics ===");
            Output(_senderInfo, $"Total Kills: {stats.TotalKills}");
            Output(_senderInfo, $"Zombie Kills: {stats.ZombieKills}");
            Output(_senderInfo, $"Animal Kills: {stats.AnimalKills}");
            Output(_senderInfo, $"Damage Dealt: {stats.TotalDamageDealt:F0}");
            Output(_senderInfo, $"Damage Taken: {stats.TotalDamageTaken:F0}");
            Output(_senderInfo, $"Retreats: {stats.RetreatCount}");
            Output(_senderInfo, $"Friendly Fire Avoided: {stats.FriendlyFireAvoided}");
            Output(_senderInfo, $"Combo Hits: {stats.ComboHits}");
            Output(_senderInfo, $"Dodges: {stats.DodgesPerformed}");
            Output(_senderInfo, $"Staggers Applied: {stats.StaggerApplied}");
        }

        private void HandleUnequip(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb unequip <slot>");
                Output(_senderInfo, "Slots: weapon, head, chest, legs, feet");
                return;
            }

            string slotName = _params[1].ToLower();
            EquipmentSlot slot;

            switch (slotName)
            {
                case "weapon":
                    slot = EquipmentSlot.Weapon;
                    break;
                case "head":
                    slot = EquipmentSlot.Head;
                    break;
                case "chest":
                    slot = EquipmentSlot.Chest;
                    break;
                case "legs":
                    slot = EquipmentSlot.Legs;
                    break;
                case "feet":
                    slot = EquipmentSlot.Feet;
                    break;
                default:
                    Output(_senderInfo, $"Unknown slot: {slotName}");
                    Output(_senderInfo, "Valid slots: weapon, head, chest, legs, feet");
                    return;
            }

            int entityId = companion.Entity.entityId;
            if (InventorySystem.UnequipItem(entityId, slot))
            {
                Output(_senderInfo, $"Unequipped item from {slotName} slot");
            }
            else
            {
                Output(_senderInfo, $"Failed to unequip from {slotName} slot (empty or inventory full)");
            }
        }

        private void HandleInventory(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            int entityId = companion.Entity.entityId;
            var inventory = InventorySystem.GetInventory(entityId);

            Output(_senderInfo, "=== Companion Inventory ===");
            Output(_senderInfo, $"Capacity: {inventory.GetTotalItemCount()}/{inventory.MaxCapacity}");
            Output(_senderInfo, $"Auto-pickup: {(inventory.AutoPickupEnabled ? "ON" : "OFF")}");
            Output(_senderInfo, "");

            var equipment = inventory.GetAllEquipment();
            if (equipment.Count > 0)
            {
                Output(_senderInfo, "--- Equipment ---");
                foreach (var item in equipment)
                {
                    Output(_senderInfo, item);
                }
                Output(_senderInfo, "");
            }

            var items = inventory.GetAllItems();
            if (items.Count > 0)
            {
                Output(_senderInfo, "--- Items ---");
                foreach (var item in items)
                {
                    Output(_senderInfo, item);
                }
            }
            else
            {
                Output(_senderInfo, "Inventory is empty");
            }
        }

        private void HandleAutoPickup(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            int entityId = companion.Entity.entityId;

            if (_params.Count < 2)
            {
                bool currentState = InventorySystem.IsAutoPickupEnabled(entityId);
                Output(_senderInfo, $"Auto-pickup is currently {(currentState ? "ON" : "OFF")}");
                Output(_senderInfo, "Usage: cb autopickup [on/off]");
                return;
            }

            string setting = _params[1].ToLower();
            bool enable = setting == "on" || setting == "true" || setting == "1";

            InventorySystem.SetAutoPickup(entityId, enable);
            Output(_senderInfo, $"Auto-pickup {(enable ? "enabled" : "disabled")}");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("autopickup", enable ? "Компаньон будет подбирать лут" : "Компаньон перестал подбирать лут");
            }
        }

        // Phase 5: Advanced AI Commands
        private void HandlePatrol(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb patrol [add/clear/start]");
                return;
            }

            int entityId = companion.Entity.entityId;
            string action = _params[1].ToLower();

            switch (action)
            {
                case "add":
                    AdvancedAI.AddPatrolWaypoint(entityId, player.position);
                    Output(_senderInfo, $"Patrol waypoint added at {player.position}");
                    break;
                case "clear":
                    AdvancedAI.ClearPatrolWaypoints(entityId);
                    Output(_senderInfo, "Patrol waypoints cleared");
                    break;
                case "start":
                    AdvancedAI.SetMode(entityId, AdvancedBehaviorMode.Patrol);
                    Output(_senderInfo, "Patrol mode started");
                    break;
                default:
                    Output(_senderInfo, "Unknown patrol action. Use: add, clear, start");
                    break;
            }
        }

        private void HandleEscort(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            float distance = 5f;
            if (_params.Count > 1 && float.TryParse(_params[1], out float parsedDist))
            {
                distance = parsedDist;
            }

            int entityId = companion.Entity.entityId;
            AdvancedAI.SetEscortParams(entityId, distance);
            Output(_senderInfo, $"Escort mode enabled with distance {distance}m");
        }

        private void HandleScout(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            float radius = 50f;
            if (_params.Count > 1 && float.TryParse(_params[1], out float parsedRadius))
            {
                radius = parsedRadius;
            }

            int entityId = companion.Entity.entityId;
            AdvancedAI.SetScoutParams(entityId, player.position, radius);
            Output(_senderInfo, $"Scout mode enabled with radius {radius}m");
        }

        private void HandleHorde(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            int entityId = companion.Entity.entityId;
            AdvancedAI.SetHordeDefensePosition(entityId, player.position);
            Output(_senderInfo, $"Horde defense position set at {player.position}");
        }

        // Phase 6: Customization Commands
        private void HandleName(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb name <name>");
                return;
            }

            string name = string.Join(" ", _params.GetRange(1, _params.Count - 1));
            int entityId = companion.Entity.entityId;
            ProfileManager.SetName(entityId, name);
            Output(_senderInfo, $"Companion renamed to '{name}'");
        }

        private void HandleClass(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb class <soldier/medic/engineer/scout/guardian>");
                return;
            }

            string className = _params[1].ToLower();
            CompanionClass companionClass;

            switch (className)
            {
                case "soldier":
                    companionClass = CompanionClass.Soldier;
                    break;
                case "medic":
                    companionClass = CompanionClass.Medic;
                    break;
                case "engineer":
                    companionClass = CompanionClass.Engineer;
                    break;
                case "scout":
                    companionClass = CompanionClass.Scout;
                    break;
                case "guardian":
                    companionClass = CompanionClass.Guardian;
                    break;
                default:
                    Output(_senderInfo, "Unknown class. Use: soldier, medic, engineer, scout, guardian");
                    return;
            }

            int entityId = companion.Entity.entityId;
            ProfileManager.SetClass(entityId, companionClass);
            Output(_senderInfo, $"Companion class set to {companionClass}");
        }

        private void HandlePersonality(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb personality <aggressive/defensive/balanced/cautious/brave>");
                return;
            }

            string traitName = _params[1].ToLower();
            PersonalityTrait personality;

            switch (traitName)
            {
                case "aggressive":
                    personality = PersonalityTrait.Aggressive;
                    break;
                case "defensive":
                    personality = PersonalityTrait.Defensive;
                    break;
                case "balanced":
                    personality = PersonalityTrait.Balanced;
                    break;
                case "cautious":
                    personality = PersonalityTrait.Cautious;
                    break;
                case "brave":
                    personality = PersonalityTrait.Brave;
                    break;
                default:
                    Output(_senderInfo, "Unknown personality. Use: aggressive, defensive, balanced, cautious, brave");
                    return;
            }

            int entityId = companion.Entity.entityId;
            ProfileManager.SetPersonality(entityId, personality);
            Output(_senderInfo, $"Companion personality set to {personality}");
        }

        private void HandleProfile(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            int entityId = companion.Entity.entityId;
            var profile = ProfileManager.GetProfile(entityId);
            
            Output(_senderInfo, "=== Companion Profile ===");
            Output(_senderInfo, profile.GetStatusReport());
            Output(_senderInfo, "");
            Output(_senderInfo, "--- Skills ---");
            foreach (var skill in profile.Skills)
            {
                Output(_senderInfo, $"{skill.Key}: Level {skill.Value.Level}/{skill.Value.MaxLevel} (XP: {skill.Value.Experience:F0}/{skill.Value.ExperienceToNextLevel:F0})");
            }
        }

        // Phase 7: Squad Commands
        private void HandleSquad(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb squad [add/remove/formation/all/status]");
                return;
            }

            string action = _params[1].ToLower();
            int ownerEntityId = player.entityId;

            switch (action)
            {
                case "add":
                    HandleSquadAdd(_senderInfo, player);
                    break;
                case "remove":
                    HandleSquadRemove(_senderInfo, player);
                    break;
                case "formation":
                    HandleSquadFormation(_params, _senderInfo, player);
                    break;
                case "all":
                    HandleSquadAll(_params, _senderInfo, player);
                    break;
                case "status":
                    HandleSquadStatus(_senderInfo, player);
                    break;
                default:
                    Output(_senderInfo, "Unknown squad action. Use: add, remove, formation, all, status");
                    break;
            }
        }

        private void HandleSquadAdd(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            int ownerEntityId = player.entityId;
            int companionEntityId = companion.Entity.entityId;
            SquadManager.AddToSquad(ownerEntityId, companionEntityId);
            Output(_senderInfo, $"Companion added to squad (Squad size: {SquadManager.GetSquadSize(ownerEntityId)})");
        }

        private void HandleSquadRemove(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, "No active companion found");
                return;
            }

            int ownerEntityId = player.entityId;
            int companionEntityId = companion.Entity.entityId;
            SquadManager.RemoveFromSquad(ownerEntityId, companionEntityId);
            Output(_senderInfo, $"Companion removed from squad (Squad size: {SquadManager.GetSquadSize(ownerEntityId)})");
        }

        private void HandleSquadFormation(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 3)
            {
                Output(_senderInfo, "Usage: cb squad formation <line/wedge/circle/column/free>");
                return;
            }

            string formationName = _params[2].ToLower();
            FormationType formation;

            switch (formationName)
            {
                case "line":
                    formation = FormationType.Line;
                    break;
                case "wedge":
                    formation = FormationType.Wedge;
                    break;
                case "circle":
                    formation = FormationType.Circle;
                    break;
                case "column":
                    formation = FormationType.Column;
                    break;
                case "free":
                    formation = FormationType.Free;
                    break;
                default:
                    Output(_senderInfo, "Unknown formation. Use: line, wedge, circle, column, free");
                    return;
            }

            int ownerEntityId = player.entityId;
            SquadManager.SetFormation(ownerEntityId, formation);
            Output(_senderInfo, $"Squad formation set to {formation}");
        }

        private void HandleSquadAll(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 3)
            {
                Output(_senderInfo, "Usage: cb squad all [follow/guard/attack]");
                return;
            }

            string command = _params[2].ToLower();
            int ownerEntityId = player.entityId;

            switch (command)
            {
                case "follow":
                    SquadManager.AllFollow(ownerEntityId);
                    Output(_senderInfo, "All squad members set to follow");
                    break;
                case "guard":
                    SquadManager.AllGuard(ownerEntityId, player.position);
                    Output(_senderInfo, $"All squad members set to guard at {player.position}");
                    break;
                case "attack":
                    SquadManager.AllAttack(ownerEntityId);
                    Output(_senderInfo, "All squad members attacking current target");
                    break;
                default:
                    Output(_senderInfo, "Unknown squad command. Use: follow, guard, attack");
                    break;
            }
        }

        private void HandleSquadStatus(CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            int ownerEntityId = player.entityId;
            var squad = SquadManager.GetSquad(ownerEntityId);
            
            Output(_senderInfo, "=== Squad Status ===");
            Output(_senderInfo, $"Squad Size: {squad.MemberEntityIds.Count}");
            Output(_senderInfo, $"Formation: {squad.Formation}");
            Output(_senderInfo, $"Formation Active: {squad.IsFormationActive}");
            Output(_senderInfo, $"Spacing: {squad.FormationSpacing}m");
            Output(_senderInfo, "");
            Output(_senderInfo, "--- Members ---");
            
            foreach (int entityId in squad.MemberEntityIds)
            {
                var companion = CompanionManager.GetCompanion(entityId);
                if (companion != null && companion.Entity != null && !companion.Entity.IsDead())
                {
                    var profile = ProfileManager.GetProfile(entityId);
                    float distance = Vector3.Distance(player.position, companion.Entity.position);
                    Output(_senderInfo, $"{profile.Name} ({profile.Class}) - HP: {companion.Entity.Health}/{companion.Entity.GetMaxHealth()} - Distance: {distance:F1}m");
                }
            }
        }

        private EntityPlayer GetPlayer(CommandSenderInfo _senderInfo)
        {
            if (_senderInfo.RemoteClientInfo != null)
            {
                return GameManager.Instance.World.GetPrimaryPlayer();
            }
            return GameManager.Instance.World.GetPrimaryPlayer();
        }

        private void Output(CommandSenderInfo _senderInfo, string message)
        {
            SdtdConsole.Instance.Output(message);
        }

        private void OutputHelp(CommandSenderInfo _senderInfo)
        {
            Output(_senderInfo, GetHelp());
        }

        // Phase 8: Multi-Companion Support
        private void HandleRole(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, Localization.Get("no_companion"));
                return;
            }

            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb role <leader/assault/support/medic/sniper/tank/scout>");
                return;
            }

            string roleName = _params[1].ToLower();
            SquadRole role;

            switch (roleName)
            {
                case "leader": role = SquadRole.Leader; break;
                case "assault": role = SquadRole.Assault; break;
                case "support": role = SquadRole.Support; break;
                case "medic": role = SquadRole.Medic; break;
                case "sniper": role = SquadRole.Sniper; break;
                case "tank": role = SquadRole.Tank; break;
                case "scout": role = SquadRole.Scout; break;
                default:
                    Output(_senderInfo, "Unknown role. Use: leader, assault, support, medic, sniper, tank, scout");
                    return;
            }

            SquadRoleManager.AssignRole(companion.Entity.entityId, role);
            Output(_senderInfo, Localization.Get("role_assigned") + $": {role}");
        }

        private void HandleShared(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 2)
            {
                bool isEnabled = SharedInventoryManager.IsEnabled(player.entityId);
                Output(_senderInfo, $"Shared inventory is currently {(isEnabled ? "enabled" : "disabled")}");
                Output(_senderInfo, "Usage: cb shared [on/off]");
                return;
            }

            string setting = _params[1].ToLower();
            bool enable = setting == "on" || setting == "true" || setting == "1";

            SharedInventoryManager.EnableSharedInventory(player.entityId, enable);
            Output(_senderInfo, enable ? Localization.Get("shared_inventory_enabled") : Localization.Get("shared_inventory_disabled"));
        }

        private void HandleDistribute(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb distribute [ammo/healing]");
                return;
            }

            string type = _params[1].ToLower();

            switch (type)
            {
                case "ammo":
                    SharedInventoryManager.DistributeAmmo(player.entityId);
                    Output(_senderInfo, Localization.Get("ammo_distributed"));
                    break;
                case "healing":
                    SharedInventoryManager.DistributeHealingItems(player.entityId);
                    Output(_senderInfo, Localization.Get("healing_distributed"));
                    break;
                default:
                    Output(_senderInfo, "Unknown type. Use: ammo, healing");
                    break;
            }
        }

        // Phase 9: Integration & Polish
        private void HandleDeath(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 2)
            {
                var settings = DeathConsequenceManager.GetCurrentSettings();
                Output(_senderInfo, $"Current death consequence: {settings.Consequence}");
                Output(_senderInfo, "Usage: cb death [respawn/permadeath/cooldown]");
                return;
            }

            string type = _params[1].ToLower();
            DeathConsequence consequence;

            switch (type)
            {
                case "respawn": consequence = DeathConsequence.Respawn; break;
                case "permadeath": consequence = DeathConsequence.Permadeath; break;
                case "cooldown": consequence = DeathConsequence.Cooldown; break;
                default:
                    Output(_senderInfo, "Unknown type. Use: respawn, permadeath, cooldown");
                    return;
            }

            DeathConsequenceManager.SetConsequence(consequence);
            Output(_senderInfo, $"Death consequence set to: {consequence}");
        }

        private void HandleConfig(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb config [reset]");
                return;
            }

            string action = _params[1].ToLower();

            if (action == "reset")
            {
                GlobalConfigManager.ResetToDefaults();
                Output(_senderInfo, "Configuration reset to defaults");
            }
            else
            {
                Output(_senderInfo, "Unknown action. Use: reset");
            }
        }

        private void HandleLanguage(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 2)
            {
                Output(_senderInfo, $"Current language: {Localization.GetLanguage()}");
                Output(_senderInfo, "Available languages: " + string.Join(", ", Localization.GetAvailableLanguages()));
                Output(_senderInfo, "Usage: cb language <lang>");
                return;
            }

            string lang = _params[1].ToLower();
            Localization.SetLanguage(lang);
            Output(_senderInfo, $"Language set to: {lang}");
        }

        // Phase 10: Stretch Goals
        private void HandleQuest(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, Localization.Get("no_companion"));
                return;
            }

            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb quest [list/accept]");
                return;
            }

            string action = _params[1].ToLower();

            switch (action)
            {
                case "list":
                    var available = QuestSystem.GetAvailableQuests(companion.Entity.entityId);
                    var active = QuestSystem.GetActiveQuests(companion.Entity.entityId);

                    Output(_senderInfo, "=== Available Quests ===");
                    foreach (var quest in available)
                    {
                        Output(_senderInfo, $"[{quest.Id}] {quest.Title} - {quest.Description} (Reward: {quest.RewardXP} XP)");
                    }

                    Output(_senderInfo, "");
                    Output(_senderInfo, "=== Active Quests ===");
                    foreach (var quest in active)
                    {
                        Output(_senderInfo, $"[{quest.Id}] {quest.Title} - Progress: {quest.CurrentProgress}/{quest.TargetCount} ({quest.GetProgressPercent() * 100:F0}%)");
                    }
                    break;

                case "accept":
                    if (_params.Count < 3)
                    {
                        Output(_senderInfo, "Usage: cb quest accept <quest_id>");
                        return;
                    }
                    string questId = _params[2];
                    if (QuestSystem.AcceptQuest(companion.Entity.entityId, questId))
                    {
                        Output(_senderInfo, "Quest accepted");
                    }
                    else
                    {
                        Output(_senderInfo, "Failed to accept quest");
                    }
                    break;

                default:
                    Output(_senderInfo, "Unknown action. Use: list, accept");
                    break;
            }
        }

        private void HandleCraft(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, Localization.Get("no_companion"));
                return;
            }

            if (_params.Count < 2)
            {
                var recipes = CompanionCrafting.GetAvailableRecipes(companion.Entity.entityId);
                Output(_senderInfo, "=== Available Recipes ===");
                foreach (var recipe in recipes)
                {
                    Output(_senderInfo, recipe);
                }
                Output(_senderInfo, "");
                Output(_senderInfo, "Usage: cb craft <item_name>");
                return;
            }

            string itemName = _params[1];
            if (CompanionCrafting.StartCrafting(companion.Entity.entityId, itemName))
            {
                Output(_senderInfo, Localization.Get("crafting_started", itemName));
            }
            else
            {
                Output(_senderInfo, "Failed to start crafting");
            }
        }

        private void HandleBuild(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            var companion = CompanionManager.GetCompanionByOwner(player);
            if (companion == null || companion.Entity.IsDead())
            {
                Output(_senderInfo, Localization.Get("no_companion"));
                return;
            }

            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb build [repair/upgrade/cancel]");
                return;
            }

            string action = _params[1].ToLower();

            switch (action)
            {
                case "repair":
                    BaseBuildingAssistant.SetRepairTask(companion.Entity.entityId, player.position, 0);
                    Output(_senderInfo, Localization.Get("building_repair"));
                    break;

                case "upgrade":
                    BaseBuildingAssistant.SetUpgradeTask(companion.Entity.entityId, player.position, 0);
                    Output(_senderInfo, Localization.Get("building_upgrade"));
                    break;

                case "cancel":
                    BaseBuildingAssistant.CancelTask(companion.Entity.entityId);
                    Output(_senderInfo, "Building task cancelled");
                    break;

                default:
                    Output(_senderInfo, "Unknown action. Use: repair, upgrade, cancel");
                    break;
            }
        }

        private void HandleAnimal(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb animal [spawn/feed]");
                return;
            }

            string action = _params[1].ToLower();

            switch (action)
            {
                case "spawn":
                    if (_params.Count < 3)
                    {
                        Output(_senderInfo, "Usage: cb animal spawn <dog/wolf/bear>");
                        return;
                    }
                    string typeName = _params[2].ToLower();
                    AnimalType animalType;

                    switch (typeName)
                    {
                        case "dog": animalType = AnimalType.Dog; break;
                        case "wolf": animalType = AnimalType.Wolf; break;
                        case "bear": animalType = AnimalType.Bear; break;
                        default:
                            Output(_senderInfo, "Unknown animal. Use: dog, wolf, bear");
                            return;
                    }

                    int animalEntityId = player.entityId + 10000;
                    AnimalCompanionManager.RegisterAnimal(animalEntityId, animalType, player);
                    Output(_senderInfo, Localization.Get("animal_tamed") + $": {animalType}");
                    break;

                case "feed":
                    if (_params.Count < 3)
                    {
                        Output(_senderInfo, "Usage: cb animal feed <item>");
                        return;
                    }
                    string foodItem = _params[2];
                    var animals = AnimalCompanionManager.GetAllAnimals();
                    if (animals.Count > 0)
                    {
                        AnimalCompanionManager.FeedAnimal(animals[0].EntityId, foodItem);
                        Output(_senderInfo, $"Animal fed with {foodItem}");
                    }
                    else
                    {
                        Output(_senderInfo, "No animal companion found");
                    }
                    break;

                default:
                    Output(_senderInfo, "Unknown action. Use: spawn, feed");
                    break;
            }
        }

        private void HandleDrone(List<string> _params, CommandSenderInfo _senderInfo, EntityPlayer player)
        {
            if (_params.Count < 2)
            {
                Output(_senderInfo, "Usage: cb drone [spawn/mode/recharge]");
                return;
            }

            string action = _params[1].ToLower();

            switch (action)
            {
                case "spawn":
                    int droneEntityId = player.entityId + 20000;
                    DroneCompanionManager.RegisterDrone(droneEntityId, player);
                    Output(_senderInfo, Localization.Get("drone_deployed"));
                    break;

                case "mode":
                    if (_params.Count < 3)
                    {
                        Output(_senderInfo, "Usage: cb drone mode <follow/patrol/scout/attack/support>");
                        return;
                    }
                    string modeName = _params[2].ToLower();
                    DroneMode mode;

                    switch (modeName)
                    {
                        case "follow": mode = DroneMode.Follow; break;
                        case "patrol": mode = DroneMode.Patrol; break;
                        case "scout": mode = DroneMode.Scout; break;
                        case "attack": mode = DroneMode.Attack; break;
                        case "support": mode = DroneMode.Support; break;
                        default:
                            Output(_senderInfo, "Unknown mode. Use: follow, patrol, scout, attack, support");
                            return;
                    }

                    var drones = DroneCompanionManager.GetAllDrones();
                    if (drones.Count > 0)
                    {
                        DroneCompanionManager.SetDroneMode(drones[0].EntityId, mode);
                        Output(_senderInfo, $"Drone mode set to: {mode}");
                    }
                    else
                    {
                        Output(_senderInfo, "No drone found");
                    }
                    break;

                case "recharge":
                    var droneList = DroneCompanionManager.GetAllDrones();
                    if (droneList.Count > 0)
                    {
                        DroneCompanionManager.RechargeDrone(droneList[0].EntityId, 50f);
                        Output(_senderInfo, "Drone recharged (+50 battery)");
                    }
                    else
                    {
                        Output(_senderInfo, "No drone found");
                    }
                    break;

                default:
                    Output(_senderInfo, "Unknown action. Use: spawn, mode, recharge");
                    break;
            }
        }
    }
}
