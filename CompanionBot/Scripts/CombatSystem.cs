using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public class CombatStats
    {
        public int TotalKills { get; set; }
        public int ZombieKills { get; set; }
        public int AnimalKills { get; set; }
        public float TotalDamageDealt { get; set; }
        public float TotalDamageTaken { get; set; }
        public int RetreatCount { get; set; }
        public int FriendlyFireAvoided { get; set; }
        public DateTime LastKillTime { get; set; }
    }

    public static class CombatSystem
    {
        private const float LowHealthThreshold = 0.3f;
        private const float RetreatDistance = 8f;
        private const float SafeDistance = 12f;
        private const float AttackRange = 25f;

        private static Dictionary<int, CombatStats> _stats = new Dictionary<int, CombatStats>();
        private static Dictionary<int, float> _retreatTimers = new Dictionary<int, float>();

        public static CombatStats GetStats(int entityId)
        {
            return _stats.ContainsKey(entityId) ? _stats[entityId] : null;
        }

        public static void InitializeStats(int entityId)
        {
            if (!_stats.ContainsKey(entityId))
            {
                _stats[entityId] = new CombatStats();
            }
        }

        public static void RecordKill(int entityId, EntityAlive victim)
        {
            if (!_stats.ContainsKey(entityId))
                InitializeStats(entityId);

            var stats = _stats[entityId];
            stats.TotalKills++;
            stats.LastKillTime = DateTime.Now;

            if (victim is EntityZombie)
                stats.ZombieKills++;
            else if (victim is EntityEnemyAnimal)
                stats.AnimalKills++;
        }

        public static void RecordDamageDealt(int entityId, float damage)
        {
            if (!_stats.ContainsKey(entityId))
                InitializeStats(entityId);

            _stats[entityId].TotalDamageDealt += damage;
        }

        public static void RecordDamageTaken(int entityId, float damage)
        {
            if (!_stats.ContainsKey(entityId))
                InitializeStats(entityId);

            _stats[entityId].TotalDamageTaken += damage;
        }

        public static void RecordRetreat(int entityId)
        {
            if (!_stats.ContainsKey(entityId))
                InitializeStats(entityId);

            _stats[entityId].RetreatCount++;
        }

        public static void RecordFriendlyFireAvoided(int entityId)
        {
            if (!_stats.ContainsKey(entityId))
                InitializeStats(entityId);

            _stats[entityId].FriendlyFireAvoided++;
        }

        public static EntityAlive FindBestTarget(EntityAlive companion, EntityPlayer owner, List<EntityAlive> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return null;

            EntityAlive bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.IsDead())
                    continue;

                float score = CalculateTargetScore(companion, owner, enemy);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        private static float CalculateTargetScore(EntityAlive companion, EntityPlayer owner, EntityAlive target)
        {
            float score = 0f;

            float distance = Vector3.Distance(companion.position, target.position);
            score += (AttackRange - distance) * 2f;

            float healthPercent = target.Health / (float)target.GetMaxHealth();
            if (healthPercent < 0.3f)
                score += 50f;

            if (owner != null && owner.attackingEntity == target)
                score += 100f;

            Vector3 toTarget = target.position - companion.position;
            Vector3 toOwner = owner != null ? owner.position - companion.position : Vector3.zero;

            if (owner != null && Vector3.Dot(toTarget.normalized, toOwner.normalized) > 0.7f)
            {
                score -= 200f;
            }

            if (target is EntityZombie)
            {
                if (target.EntityName.Contains("Feral") || target.EntityName.Contains("Radiated"))
                    score += 30f;
            }

            return score;
        }

        public static bool ShouldRetreat(EntityAlive companion)
        {
            float healthPercent = companion.Health / (float)companion.GetMaxHealth();
            return healthPercent < LowHealthThreshold;
        }

        public static Vector3 CalculateRetreatPosition(EntityAlive companion, EntityPlayer owner, EntityAlive threat)
        {
            Vector3 awayFromThreat = (companion.position - threat.position).normalized;
            Vector3 towardsOwner = owner != null ? (owner.position - companion.position).normalized : Vector3.zero;

            Vector3 retreatDirection = (awayFromThreat + towardsOwner).normalized;

            Vector3 retreatPos = companion.position + retreatDirection * RetreatDistance;

            if (owner != null)
            {
                float distToOwner = Vector3.Distance(retreatPos, owner.position);
                if (distToOwner > SafeDistance)
                {
                    retreatPos = owner.position + (retreatPos - owner.position).normalized * SafeDistance;
                }
            }

            return retreatPos;
        }

        public static bool IsLineOfFireClear(EntityAlive companion, EntityAlive target, EntityPlayer owner)
        {
            if (owner == null)
                return true;

            Vector3 fireDirection = (target.position - companion.position).normalized;
            Vector3 toOwner = owner.position - companion.position;

            float dotProduct = Vector3.Dot(fireDirection, toOwner.normalized);

            if (dotProduct > 0.85f)
            {
                float ownerDistance = toOwner.magnitude;
                float targetDistance = Vector3.Distance(companion.position, target.position);

                if (ownerDistance < targetDistance)
                {
                    Vector3 closestPoint = companion.position + fireDirection * ownerDistance;
                    float perpendicularDistance = Vector3.Distance(owner.position, closestPoint);

                    if (perpendicularDistance < 2f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsRetreating(int entityId)
        {
            if (_retreatTimers.ContainsKey(entityId))
            {
                if (Time.time - _retreatTimers[entityId] < 3f)
                    return true;
                else
                    _retreatTimers.Remove(entityId);
            }
            return false;
        }

        public static void StartRetreat(int entityId)
        {
            _retreatTimers[entityId] = Time.time;
            RecordRetreat(entityId);
        }

        public static void CleanupStats(int entityId)
        {
            if (_stats.ContainsKey(entityId))
                _stats.Remove(entityId);
            if (_retreatTimers.ContainsKey(entityId))
                _retreatTimers.Remove(entityId);
        }
    }
}
