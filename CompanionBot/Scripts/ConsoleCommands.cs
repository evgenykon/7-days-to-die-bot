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
  cb status           - Show companion status";
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
