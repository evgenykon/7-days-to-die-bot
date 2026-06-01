using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace CompanionBot
{
    public class ModMain : IModApi
    {
        private static Harmony _harmony;
        public static ModMain Instance { get; private set; }

        public static LLMClient LLM { get; private set; }
        public static RAGSystem RAG { get; private set; }
        public static MemoryLogger MemoryLog { get; private set; }
        public static ChatSystem Chat { get; private set; }

        public void InitMod(Mod _modInstance)
        {
            Instance = this;
            _harmony = new Harmony("com.ai7d2d.companionbot");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            SaveSystem.Initialize();
            InitializeLLMSystems();

            Log.Out("[CompanionBot] Mod loaded successfully");
        }

        private void InitializeLLMSystems()
        {
            try
            {
                var configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Config", "llm_config.json");

                if (!File.Exists(configPath))
                {
                    Log.Error($"[CompanionBot] Config not found: {configPath}");
                    return;
                }

                var configJson = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<LLMConfig>(configJson);

                LLM = new LLMClient(
                    config.Endpoint,
                    config.Model,
                    config.Temperature,
                    config.MaxTokens,
                    config.RateLimit.CooldownSeconds
                );

                var memoryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data", "memories.json");
                RAG = new RAGSystem(LLM, memoryPath);
                MemoryLog = new MemoryLogger(RAG);

                Chat = new ChatSystem(
                    LLM,
                    RAG,
                    "male",
                    config.Personality.Tone,
                    config.Personality.Verbosity
                );

                Log.Out("[CompanionBot] LLM systems initialized");
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to initialize LLM systems: {ex.Message}");
            }
        }

        private class LLMConfig
        {
            [JsonProperty("endpoint")]
            public string Endpoint { get; set; }

            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("temperature")]
            public float Temperature { get; set; }

            [JsonProperty("max_tokens")]
            public int MaxTokens { get; set; }

            [JsonProperty("personality")]
            public PersonalityConfig Personality { get; set; }

            [JsonProperty("rate_limit")]
            public RateLimitConfig RateLimit { get; set; }
        }

        private class PersonalityConfig
        {
            [JsonProperty("tone")]
            public string Tone { get; set; }

            [JsonProperty("verbosity")]
            public string Verbosity { get; set; }

            [JsonProperty("humor")]
            public string Humor { get; set; }
        }

        private class RateLimitConfig
        {
            [JsonProperty("messages_per_minute")]
            public int MessagesPerMinute { get; set; }

            [JsonProperty("cooldown_seconds")]
            public int CooldownSeconds { get; set; }
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "Update")]
    public class CompanionAIPatch
    {
        static void Postfix(EntityAlive __instance)
        {
            var companionData = CompanionManager.GetCompanion(__instance.entityId);
            if (companionData == null)
                return;

            if (__instance.IsDead())
            {
                CompanionManager.UnregisterCompanion(__instance.entityId);
                return;
            }

            CompanionAI.Update(companionData);
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "OnEntityDeath")]
    public class CompanionDeathPatch
    {
        static void Postfix(EntityAlive __instance, EntityAlive _attackingEntity)
        {
            if (__instance == null)
                return;

            if (__instance is EntityPlayer)
            {
                ModMain.MemoryLog?.LogDeath(__instance, _attackingEntity);
                _ = ModMain.Chat?.SendMessage("player_death", "Игрок погиб");
            }

            var companionData = CompanionManager.GetCompanion(__instance.entityId);
            if (companionData != null)
            {
                CompanionManager.UnregisterCompanion(__instance.entityId);
                Log.Out($"[CompanionBot] Companion died: {__instance.entityId}");
                _ = ModMain.Chat?.SendMessage("companion_death", "Компаньон погиб");
            }
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "OnEntityKill")]
    public class EntityKillPatch
    {
        static void Postfix(EntityAlive __instance, EntityAlive _attackingEntity)
        {
            if (__instance == null || _attackingEntity == null)
                return;

            if (_attackingEntity is EntityPlayer)
            {
                ModMain.MemoryLog?.LogKill(_attackingEntity, __instance);
                _ = ModMain.Chat?.SendMessage("player_kill", $"Игрок убил {__instance.EntityName}");
            }
            else if (CompanionManager.GetCompanion(_attackingEntity.entityId) != null)
            {
                CombatSystem.RecordKill(_attackingEntity.entityId, __instance);
                ModMain.MemoryLog?.LogKill(_attackingEntity, __instance);
                _ = ModMain.Chat?.SendMessage("companion_kill", $"Компаньон убил {__instance.EntityName}");
            }
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "DamageEntity")]
    public class DamageEntityPatch
    {
        static void Postfix(EntityAlive __instance, DamageSource _damageSource, EntityAlive _attackingEntity, float _damage, bool _critical)
        {
            if (__instance == null)
                return;

            var companionData = CompanionManager.GetCompanion(__instance.entityId);
            if (companionData != null)
            {
                CombatSystem.RecordDamageTaken(__instance.entityId, _damage);
                CombatSystem.PlayDamageFeedback(__instance, _damage);
            }

            if (_attackingEntity != null && CompanionManager.GetCompanion(_attackingEntity.entityId) != null)
            {
                CombatSystem.RecordDamageDealt(_attackingEntity.entityId, _damage);
                CombatSystem.PlayAttackFeedback(_attackingEntity, __instance, _damage);
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), "SaveWorld")]
    public class SaveWorldPatch
    {
        static void Prefix()
        {
            try
            {
                SaveSystem.SaveCompanions();
                Log.Out("[CompanionBot] World save - companions saved");
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to save companions on world save: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), "LoadWorld")]
    public class LoadWorldPatch
    {
        static void Postfix()
        {
            try
            {
                CompanionManager.Cleanup();
                SaveSystem.LoadCompanions();
                Log.Out("[CompanionBot] World load - companions restored");
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to load companions on world load: {ex.Message}");
            }
        }
    }

    public static class CompanionAI
    {
        private const float FollowDistance = 3f;
        private const float MaxFollowDistance = 15f;
        private const float AttackRange = 25f;
        private const float UpdateInterval = 0.5f;
        private const float GuardReturnDistance = 5f;

        private static Dictionary<int, float> lastUpdateTime = new Dictionary<int, float>();

        public static void Update(CompanionData companionData)
        {
            if (companionData == null)
                return;

            var companion = companionData.Entity;
            var owner = companionData.Owner;

            if (companion == null || companion.IsDead())
            {
                CompanionManager.UnregisterCompanion(companionData.Entity?.entityId ?? -1);
                return;
            }

            if (owner == null || owner.IsDead())
                return;

            int entityId = companion.entityId;

            CombatSystem.InitializeStats(entityId);

            if (!lastUpdateTime.ContainsKey(entityId))
            {
                lastUpdateTime[entityId] = 0f;
            }

            if (Time.time - lastUpdateTime[entityId] < UpdateInterval)
                return;

            lastUpdateTime[entityId] = Time.time;

            UpdateCombatBehavior(companion, owner, entityId);

            List<EntityAlive> enemies = FindAllEnemies(companion, owner);
            EntityAlive target = CombatSystem.FindBestTarget(companion, owner, enemies);

            if (CombatSystem.ShouldRetreat(companion) && target != null)
            {
                if (!CombatSystem.IsRetreating(entityId))
                {
                    CombatSystem.StartRetreat(entityId);
                    Log.Out($"[CompanionBot] Companion {entityId} retreating (low HP)");
                }

                Vector3 retreatPos = CombatSystem.CalculateRetreatPosition(companion, owner, target);
                MoveTowards(companion, retreatPos);
                companion.SetAttackTarget(null);
                return;
            }

            if (target != null && Vector3.Distance(companion.position, target.position) <= AttackRange)
            {
                if (CombatSystem.IsMeleeWeapon(companion))
                {
                    float distanceToTarget = Vector3.Distance(companion.position, target.position);
                    if (distanceToTarget <= 3f)
                    {
                        CombatSystem.RecordAttack(entityId);

                        if (CombatSystem.ShouldApplyStagger(entityId))
                        {
                            Log.Out($"[CompanionBot] Companion {entityId} staggered {target.EntityName}");
                        }
                    }
                }
                else
                {
                    if (!CombatSystem.HasAmmo(companion))
                    {
                        Log.Out($"[CompanionBot] Companion {entityId} out of ammo, switching to melee");
                    }
                    else if (CombatSystem.ShouldReload(companion))
                    {
                        Log.Out($"[CompanionBot] Companion {entityId} reloading");
                    }
                }

                if (CombatSystem.IsLineOfFireClear(companion, target, owner))
                {
                    companion.SetAttackTarget(target);
                    return;
                }
                else
                {
                    CombatSystem.RecordFriendlyFireAvoided(entityId);
                    Vector3 repositionDir = Vector3.Cross((target.position - companion.position).normalized, Vector3.up);
                    Vector3 repositionPos = companion.position + repositionDir * 3f;
                    MoveTowards(companion, repositionPos);
                    companion.SetAttackTarget(null);
                    return;
                }
            }

            companion.SetAttackTarget(null);

            switch (companionData.State)
            {
                case CompanionState.Follow:
                    UpdateFollowState(companion, owner);
                    break;

                case CompanionState.Stay:
                    break;

                case CompanionState.Guard:
                    UpdateGuardState(companion, companionData);
                    break;
            }
        }

        private static void UpdateCombatBehavior(EntityAlive companion, EntityPlayer owner, int entityId)
        {
            if (GameManager.Instance?.World?.Entities?.list == null)
                return;

            foreach (var entity in GameManager.Instance.World.Entities.list)
            {
                if (entity == null || entity.IsDead())
                    continue;

                if (!(entity is EntityZombie) && !(entity is EntityEnemyAnimal))
                    continue;

                float distance = Vector3.Distance(companion.position, entity.position);
                if (distance > 5f)
                    continue;

                if (CombatSystem.ShouldDodge(entityId, companion, entity))
                {
                    Vector3 dodgeDir = CombatSystem.CalculateDodgeDirection(entityId, companion, entity);
                    Vector3 dodgePos = companion.position + dodgeDir * 4f;
                    MoveTowards(companion, dodgePos);
                    Log.Out($"[CompanionBot] Companion {entityId} dodged attack from {entity.EntityName}");
                    return;
                }
            }
        }

        private static void UpdateFollowState(EntityAlive companion, EntityPlayer owner)
        {
            float distanceToOwner = Vector3.Distance(companion.position, owner.position);

            if (distanceToOwner > MaxFollowDistance)
            {
                TeleportToOwner(companion, owner);
            }
            else if (distanceToOwner > FollowDistance)
            {
                MoveTowards(companion, owner.position);
            }
        }

        private static void UpdateGuardState(EntityAlive companion, CompanionData companionData)
        {
            float distanceToGuardPos = Vector3.Distance(companion.position, companionData.GuardPosition);

            if (distanceToGuardPos > companionData.GuardRadius)
            {
                MoveTowards(companion, companionData.GuardPosition);
            }
            else if (distanceToGuardPos > GuardReturnDistance)
            {
                MoveTowards(companion, companionData.GuardPosition);
            }
        }

        private static List<EntityAlive> FindAllEnemies(EntityAlive companion, EntityPlayer owner)
        {
            var enemies = new List<EntityAlive>();

            if (GameManager.Instance?.World?.Entities?.list == null)
                return enemies;

            List<EntityAlive> entities = GameManager.Instance.World.Entities.list;
            foreach (EntityAlive entity in entities)
            {
                if (entity == null || entity.IsDead())
                    continue;

                if (entity == companion || entity == owner)
                    continue;

                if (entity is EntityPlayer)
                    continue;

                if (CompanionManager.GetCompanion(entity.entityId) != null)
                    continue;

                if (entity is EntityTrader || entity is EntityNPC)
                    continue;

                if (!(entity is EntityZombie) && !(entity is EntityEnemyAnimal))
                    continue;

                float distance = Vector3.Distance(companion.position, entity.position);
                if (distance <= AttackRange)
                {
                    enemies.Add(entity);
                }
            }

            return enemies;
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
}
