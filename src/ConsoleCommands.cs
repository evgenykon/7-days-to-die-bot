using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBotV2
{
    public class ConsoleCmdCB : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new string[] { "cb" };
        }

        public override string getDescription()
        {
            return "CompanionBot commands";
        }

        public override string GetHelp()
        {
            return "cb spawn - spawn companion";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count == 0)
            {
                OutputHelp(_senderInfo);
                return;
            }

            switch (_params[0].ToLower())
            {
                case "spawn":
                    HandleSpawn(_senderInfo);
                    break;
                case "check":
                    HandleCheck(_senderInfo);
                    break;
                default:
                    Output(_senderInfo, "Unknown command. Use: spawn");
                    break;
            }
        }

        private void HandleSpawn(CommandSenderInfo _senderInfo)
        {
            EntityPlayer player = null;
            try
            {
                player = GameManager.Instance.World.GetPrimaryPlayer();
                Output(_senderInfo, $"Player found: {player?.entityId}");
            }
            catch (Exception ex)
            {
                Output(_senderInfo, $"Player error: {ex.Message}");
                return;
            }

            if (player == null)
            {
                Output(_senderInfo, "Player not found");
                return;
            }

            int classId = EntityClass.FromString("companionBot");
            Output(_senderInfo, $"classId={classId}");

            if (classId < 0)
            {
                Output(_senderInfo, "Entity class not found");
                return;
            }

            Entity entity = null;
            try
            {
                Vector3 pos = player.position + player.GetForwardVector() * 3f;
                var ecd = new EntityCreationData();
                ecd.entityClass = classId;
                ecd.id = EntityFactory.nextEntityID++;
                ecd.pos = pos;
                ecd.rot = Vector3.zero;
                ecd.spawnById = -1;

                entity = EntityFactory.CreateEntity(ecd);
                Output(_senderInfo, $"CreateEntity done: {entity != null}");
            }
            catch (Exception ex)
            {
                Output(_senderInfo, $"CreateEntity error: {ex.Message}");
                Log.Error($"[CB] CreateEntity error: {ex.StackTrace}");
                return;
            }

            if (entity == null)
            {
                Output(_senderInfo, "EntityFactory returned null");
                return;
            }

            try
            {
                GameManager.Instance.World.SpawnEntityInWorld(entity);
                Output(_senderInfo, $"Spawned! ID={entity.entityId} Type={entity.GetType().Name}");
                Log.Out($"[CB] Spawned: ID={entity.entityId} Type={entity.GetType().Name}");
            }
            catch (Exception ex)
            {
                Output(_senderInfo, $"Spawn error: {ex.Message}");
                Log.Error($"[CB] Spawn error: {ex.StackTrace}");
            }
        }

        private void HandleCheck(CommandSenderInfo _senderInfo)
        {
            int classId = EntityClass.FromString("companionBot");
            Output(_senderInfo, $"companionBot class ID: {classId}");

            if (classId >= 0)
            {
                var className = EntityClass.GetEntityClassName(classId);
                Output(_senderInfo, $"Class name: {className}");
            }
        }

        private EntityPlayer GetPlayer(CommandSenderInfo _senderInfo)
        {
            if (_senderInfo.RemoteClientInfo != null)
                return GameManager.Instance.World.GetPrimaryPlayer();
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
    }
}
