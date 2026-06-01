using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public class ConsoleCmdCB : ConsoleCmdAbstract
    {
        public override string[] GetCommands()
        {
            return new string[] { "cb" };
        }

        public override string GetDescription()
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
  cb stats            - Show combat statistics";
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
                case "stats":
                    HandleStats(_senderInfo, player);
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
                int entityId = EntityFactory.CreateEntity(entityType, spawnPos);

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
                GameManager.Instance.World.RemoveEntity(entityId, EnumRemoveEntity.Kill);
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
                if (inventory.GetItemCount(itemName) > 0)
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
                inventory.DecItem(itemName: foundItem, count: 1);

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

            if (inventory.GetItemCount(itemName) <= 0)
            {
                Output(_senderInfo, $"Item '{itemName}' not found in inventory");
                return;
            }

            try
            {
                inventory.DecItem(itemName: itemName, count: 1);

                var entity = companion.Entity;
                entity.inventory.SetItem(0, new ItemStack(ItemClass.GetItem(itemName), 1));

                Output(_senderInfo, $"Equipped companion with {itemName}");

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

        private EntityPlayer GetPlayer(CommandSenderInfo _senderInfo)
        {
            if (_senderInfo.RemoteClientInfo != null)
            {
                return _senderInfo.RemoteClientInfo.entityPlayerLocal;
            }
            return GameManager.Instance.World.GetPrimaryPlayer();
        }

        private void Output(CommandSenderInfo _senderInfo, string message)
        {
            if (_senderInfo.RemoteClientInfo != null)
            {
                _senderInfo.RemoteClientInfo.SendPackage(new NetPackageChat(
                    EnumChatClients.FromServer,
                    -1,
                    _senderInfo.RemoteClientInfo.entityId,
                    message,
                    false
                ));
            }
            else
            {
                SdtdConsole.Instance.Output(message);
            }
        }

        private void OutputHelp(CommandSenderInfo _senderInfo)
        {
            Output(_senderInfo, GetHelp());
        }
    }
}
