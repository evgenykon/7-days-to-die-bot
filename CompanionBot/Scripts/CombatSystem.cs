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
        public int ComboHits { get; set; }
        public int DodgesPerformed { get; set; }
        public int StaggerApplied { get; set; }
        public DateTime LastKillTime { get; set; }
    }

    public static class CombatSystem
    {
        private const float LowHealthThreshold = 0.3f;
        private const float RetreatDistance = 8f;
        private const float SafeDistance = 12f;
        private const float AttackRange = 25f;
        private const float MeleeRange = 3f;
        private const float DodgeDistance = 4f;
        private const float ComboWindow = 1.5f;

        private static Dictionary<int, CombatStats> _stats = new Dictionary<int, CombatStats>();
        private static Dictionary<int, float> _retreatTimers = new Dictionary<int, float>();
        private static Dictionary<int, float> _lastAttackTime = new Dictionary<int, float>();
        private static Dictionary<int, int> _comboCount = new Dictionary<int, int>();
        private static Dictionary<int, float> _lastDodgeTime = new Dictionary<int, float>();
        private static Dictionary<int, Vector3> _dodgeDirection = new Dictionary<int, Vector3>();

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

        public static void RecordComboHit(int entityId)
        {
            if (!_stats.ContainsKey(entityId))
                InitializeStats(entityId);

            _stats[entityId].ComboHits++;
        }

        public static void RecordDodge(int entityId)
        {
            if (!_stats.ContainsKey(entityId))
                InitializeStats(entityId);

            _stats[entityId].DodgesPerformed++;
        }

        public static void RecordStagger(int entityId)
        {
            if (!_stats.ContainsKey(entityId))
                InitializeStats(entityId);

            _stats[entityId].StaggerApplied++;
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

            if (owner != null && GameApi.GetTarget(owner) == target)
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
            if (_lastAttackTime.ContainsKey(entityId))
                _lastAttackTime.Remove(entityId);
            if (_comboCount.ContainsKey(entityId))
                _comboCount.Remove(entityId);
            if (_lastDodgeTime.ContainsKey(entityId))
                _lastDodgeTime.Remove(entityId);
            if (_dodgeDirection.ContainsKey(entityId))
                _dodgeDirection.Remove(entityId);
        }

        public static bool HasAmmo(EntityAlive companion)
        {
            if (companion?.inventory == null)
                return false;

            return true;
        }

        public static bool ShouldReload(EntityAlive companion)
        {
            return false;
        }

        public static bool IsMeleeWeapon(EntityAlive companion)
        {
            if (companion?.inventory == null)
                return true;

            var heldItem = companion.inventory.holdingItem;
            if (heldItem == null)
                return true;

            string itemName = heldItem.GetItemName().ToLower();
            return !itemName.Contains("gun") && !itemName.Contains("bow") && !itemName.Contains("crossbow");
        }

        public static bool CanPerformCombo(int entityId)
        {
            if (!_lastAttackTime.ContainsKey(entityId))
                return false;

            float timeSinceLastAttack = Time.time - _lastAttackTime[entityId];
            return timeSinceLastAttack < ComboWindow;
        }

        public static void RecordAttack(int entityId)
        {
            if (!CanPerformCombo(entityId))
            {
                _comboCount[entityId] = 1;
            }
            else
            {
                if (!_comboCount.ContainsKey(entityId))
                    _comboCount[entityId] = 0;
                _comboCount[entityId]++;
                RecordComboHit(entityId);
            }

            _lastAttackTime[entityId] = Time.time;
        }

        public static int GetComboCount(int entityId)
        {
            return _comboCount.ContainsKey(entityId) ? _comboCount[entityId] : 0;
        }

        public static bool ShouldApplyStagger(int entityId)
        {
            int combo = GetComboCount(entityId);
            if (combo >= 3)
            {
                RecordStagger(entityId);
                _comboCount[entityId] = 0;
                return true;
            }
            return false;
        }

        public static bool ShouldDodge(int entityId, EntityAlive companion, EntityAlive threat)
        {
            if (threat == null || threat.IsDead())
                return false;

            if (_lastDodgeTime.ContainsKey(entityId))
            {
                if (Time.time - _lastDodgeTime[entityId] < 2f)
                    return false;
            }

            float distance = Vector3.Distance(companion.position, threat.position);
            if (distance > MeleeRange + 2f)
                return false;

            if (GameApi.GetTarget(threat) == companion)
                return true;

            return false;
        }

        public static Vector3 CalculateDodgeDirection(int entityId, EntityAlive companion, EntityAlive threat)
        {
            Vector3 awayFromThreat = (companion.position - threat.position).normalized;
            Vector3 strafeLeft = Vector3.Cross(awayFromThreat, Vector3.up).normalized;
            Vector3 strafeRight = -strafeLeft;

            bool dodgeLeft = UnityEngine.Random.value > 0.5f;
            Vector3 dodgeDir = dodgeLeft ? strafeLeft : strafeRight;

            _dodgeDirection[entityId] = dodgeDir;
            _lastDodgeTime[entityId] = Time.time;
            RecordDodge(entityId);

            return dodgeDir;
        }

        public static void PlayDamageFeedback(EntityAlive companion, float damageAmount)
        {
            if (companion == null)
                return;

            if (damageAmount > 20f)
            {
                Log.Out($"[CompanionBot] Companion {companion.entityId} took heavy damage: {damageAmount:F0}");
            }

            float healthPercent = companion.Health / (float)companion.GetMaxHealth();
            if (healthPercent < 0.5f && healthPercent > 0.3f)
            {
                Log.Out($"[CompanionBot] Companion {companion.entityId} health warning: {healthPercent * 100:F0}%");
            }
        }

        public static void PlayAttackFeedback(EntityAlive companion, EntityAlive target, float damageDealt)
        {
            if (companion == null || target == null)
                return;

            int entityId = companion.entityId;
            int combo = GetComboCount(entityId);

            if (combo >= 3)
            {
                Log.Out($"[CompanionBot] Companion {entityId} performed {combo}-hit combo!");
            }

            if (damageDealt > 50f)
            {
                Log.Out($"[CompanionBot] Companion {entityId} dealt critical damage: {damageDealt:F0}");
            }
        }
    }
}
