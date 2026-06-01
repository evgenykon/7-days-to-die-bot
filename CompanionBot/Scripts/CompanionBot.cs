using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CompanionBot
{
    public class ModMain : IModApi
    {
        private static Harmony _harmony;
        public static ModMain Instance { get; private set; }
        public static List<EntityAlive> Companions { get; } = new List<EntityAlive>();

        public void InitMod(Mod _modInstance)
        {
            Instance = this;
            _harmony = new Harmony("com.ai7d2d.companionbot");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[CompanionBot] Mod loaded successfully");
        }
    }

    [HarmonyPatch(typeof(ConsoleCmdSpawnEntity), "Execute")]
    public class SpawnCompanionPatch
    {
        static bool Prefix(string[] _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Length < 1)
                return true;

            string entityName = _params[0].ToLower();

            if (entityName == "companionbot" || entityName == "companionbotarmed")
            {
                try
                {
                    EntityPlayer player = _senderInfo.RemoteClientInfo?.entityPlayerLocal;
                    if (player == null)
                    {
                        player = GameManager.Instance.World.GetPrimaryPlayer();
                    }

                    if (player == null)
                    {
                        SdtdConsole.Instance.Output("Player not found");
                        return false;
                    }

                    Vector3 spawnPos = player.position + new Vector3(2, 0, 2);
                    int entityId = EntityFactory.CreateEntity(entityName, spawnPos);

                    if (entityId > 0)
                    {
                        EntityAlive companion = GameManager.Instance.World.GetEntity(entityId) as EntityAlive;
                        if (companion != null)
                        {
                            ModMain.Companions.Add(companion);
                            SdtdConsole.Instance.Output($"Companion spawned successfully! Entity ID: {entityId}");
                            Log.Out($"[CompanionBot] Spawned {entityName} at {spawnPos}, ID: {entityId}");
                        }
                    }
                    else
                    {
                        SdtdConsole.Instance.Output("Failed to spawn companion");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[CompanionBot] Error spawning companion: {ex.Message}");
                    SdtdConsole.Instance.Output($"Error: {ex.Message}");
                }
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "Update")]
    public class CompanionAIPatch
    {
        static void Postfix(EntityAlive __instance)
        {
            if (!ModMain.Companions.Contains(__instance))
                return;

            if (__instance.IsDead())
            {
                ModMain.Companions.Remove(__instance);
                return;
            }

            CompanionAI.Update(__instance);
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "OnEntityDeath")]
    public class CompanionDeathPatch
    {
        static void Postfix(EntityAlive __instance)
        {
            if (ModMain.Companions.Contains(__instance))
            {
                ModMain.Companions.Remove(__instance);
                Log.Out($"[CompanionBot] Companion died: {__instance.entityId}");
            }
        }
    }

    public static class CompanionAI
    {
        private const float FollowDistance = 3f;
        private const float MaxFollowDistance = 15f;
        private const float AttackRange = 25f;
        private const float UpdateInterval = 0.5f;

        private static Dictionary<int, float> lastUpdateTime = new Dictionary<int, float>();
        private static Dictionary<int, EntityPlayer> companionOwners = new Dictionary<int, EntityPlayer>();

        public static void Update(EntityAlive companion)
        {
            int entityId = companion.entityId;

            if (!lastUpdateTime.ContainsKey(entityId))
            {
                lastUpdateTime[entityId] = 0f;
            }

            if (Time.time - lastUpdateTime[entityId] < UpdateInterval)
                return;

            lastUpdateTime[entityId] = Time.time;

            EntityPlayer owner = GetOwner(companion);
            if (owner == null || owner.IsDead())
                return;

            float distanceToOwner = Vector3.Distance(companion.position, owner.position);

            EntityAlive target = FindNearestEnemy(companion, owner);

            if (target != null && Vector3.Distance(companion.position, target.position) <= AttackRange)
            {
                companion.SetAttackTarget(target);
                return;
            }

            companion.SetAttackTarget(null);

            if (distanceToOwner > MaxFollowDistance)
            {
                TeleportToOwner(companion, owner);
            }
            else if (distanceToOwner > FollowDistance)
            {
                MoveTowards(companion, owner.position);
            }
        }

        private static EntityPlayer GetOwner(EntityAlive companion)
        {
            int entityId = companion.entityId;

            if (companionOwners.ContainsKey(entityId))
            {
                EntityPlayer owner = companionOwners[entityId];
                if (owner != null && !owner.IsDead())
                    return owner;
            }

            EntityPlayer nearestPlayer = null;
            float nearestDistance = float.MaxValue;

            foreach (EntityPlayer player in GameManager.Instance.World.Players.list)
            {
                if (player == null || player.IsDead())
                    continue;

                float distance = Vector3.Distance(companion.position, player.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlayer = player;
                }
            }

            if (nearestPlayer != null)
            {
                companionOwners[entityId] = nearestPlayer;
            }

            return nearestPlayer;
        }

        private static EntityAlive FindNearestEnemy(EntityAlive companion, EntityPlayer owner)
        {
            EntityAlive nearestEnemy = null;
            float nearestDistance = AttackRange;

            List<EntityAlive> entities = GameManager.Instance.World.Entities.list;
            foreach (EntityAlive entity in entities)
            {
                if (entity == null || entity.IsDead())
                    continue;

                if (entity == companion || entity == owner)
                    continue;

                if (entity is EntityPlayer)
                    continue;

                if (!(entity is EntityZombie) && !(entity is EntityEnemyAnimal))
                    continue;

                float distance = Vector3.Distance(companion.position, entity.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = entity;
                }
            }

            return nearestEnemy;
        }

        private static void MoveTowards(EntityAlive companion, Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - companion.position).normalized;
            companion.Move(direction * companion.MoveSpeed);
            companion.RotateToTarget(targetPosition);
        }

        private static void TeleportToOwner(EntityAlive companion, EntityPlayer owner)
        {
            Vector3 teleportPos = owner.position + new Vector3(2, 0, 2);
            companion.position = teleportPos;
            companion.transform.position = teleportPos;
            Log.Out($"[CompanionBot] Teleported companion to owner");
        }
    }

    public static class ConsoleCommands
    {
        public static void RegisterCommands()
        {
            SdtdConsole.Instance.ExecuteSync("help", null);
        }
    }
}
