using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public enum AdvancedBehaviorMode
    {
        Follow,
        Patrol,
        Guard,
        Escort,
        Scout,
        HordeDefense
    }

    public class Waypoint
    {
        public Vector3 Position { get; set; }
        public float WaitTime { get; set; }
        public int Index { get; set; }

        public Waypoint(Vector3 position, float waitTime = 2f, int index = 0)
        {
            Position = position;
            WaitTime = waitTime;
            Index = index;
        }
    }

    public class AdvancedBehaviorData
    {
        public AdvancedBehaviorMode Mode { get; set; }
        public List<Waypoint> PatrolWaypoints { get; set; }
        public int CurrentWaypointIndex { get; set; }
        public float WaypointArrivalTime { get; set; }
        public Vector3 GuardCenter { get; set; }
        public float GuardRadius { get; set; }
        public float EscortDistance { get; set; }
        public float ScoutRadius { get; set; }
        public Vector3 ScoutOrigin { get; set; }
        public bool IsScouting { get; set; }
        public float LastScoutReportTime { get; set; }
        public Vector3 HordeDefensePosition { get; set; }
        public bool IsHordeNight { get; set; }

        public AdvancedBehaviorData()
        {
            Mode = AdvancedBehaviorMode.Follow;
            PatrolWaypoints = new List<Waypoint>();
            CurrentWaypointIndex = 0;
            WaypointArrivalTime = 0f;
            GuardCenter = Vector3.zero;
            GuardRadius = 15f;
            EscortDistance = 5f;
            ScoutRadius = 50f;
            ScoutOrigin = Vector3.zero;
            IsScouting = false;
            LastScoutReportTime = 0f;
            HordeDefensePosition = Vector3.zero;
            IsHordeNight = false;
        }
    }

    public static class AdvancedAI
    {
        private static Dictionary<int, AdvancedBehaviorData> _behaviorData = new Dictionary<int, AdvancedBehaviorData>();
        private static Dictionary<int, float> _lastUpdateTime = new Dictionary<int, float>();
        private const float UpdateInterval = 0.5f;

        public static AdvancedBehaviorData GetBehaviorData(int entityId)
        {
            if (!_behaviorData.ContainsKey(entityId))
            {
                _behaviorData[entityId] = new AdvancedBehaviorData();
            }
            return _behaviorData[entityId];
        }

        public static void SetMode(int entityId, AdvancedBehaviorMode mode)
        {
            var data = GetBehaviorData(entityId);
            data.Mode = mode;
            Log.Out($"[CompanionBot] Companion {entityId} behavior mode set to {mode}");
        }

        public static void AddPatrolWaypoint(int entityId, Vector3 position, float waitTime = 2f)
        {
            var data = GetBehaviorData(entityId);
            int index = data.PatrolWaypoints.Count;
            data.PatrolWaypoints.Add(new Waypoint(position, waitTime, index));
            Log.Out($"[CompanionBot] Added waypoint {index} for companion {entityId} at {position}");
        }

        public static void ClearPatrolWaypoints(int entityId)
        {
            var data = GetBehaviorData(entityId);
            data.PatrolWaypoints.Clear();
            data.CurrentWaypointIndex = 0;
            Log.Out($"[CompanionBot] Cleared patrol waypoints for companion {entityId}");
        }

        public static void SetGuardArea(int entityId, Vector3 center, float radius)
        {
            var data = GetBehaviorData(entityId);
            data.GuardCenter = center;
            data.GuardRadius = radius;
            data.Mode = AdvancedBehaviorMode.Guard;
            Log.Out($"[CompanionBot] Companion {entityId} guarding area at {center} with radius {radius}");
        }

        public static void SetEscortParams(int entityId, float distance)
        {
            var data = GetBehaviorData(entityId);
            data.EscortDistance = distance;
            data.Mode = AdvancedBehaviorMode.Escort;
            Log.Out($"[CompanionBot] Companion {entityId} escort mode with distance {distance}");
        }

        public static void SetScoutParams(int entityId, Vector3 origin, float radius)
        {
            var data = GetBehaviorData(entityId);
            data.ScoutOrigin = origin;
            data.ScoutRadius = radius;
            data.Mode = AdvancedBehaviorMode.Scout;
            data.IsScouting = true;
            Log.Out($"[CompanionBot] Companion {entityId} scout mode from {origin} with radius {radius}");
        }

        public static void SetHordeDefensePosition(int entityId, Vector3 position)
        {
            var data = GetBehaviorData(entityId);
            data.HordeDefensePosition = position;
            data.IsHordeNight = true;
            data.Mode = AdvancedBehaviorMode.HordeDefense;
            Log.Out($"[CompanionBot] Companion {entityId} horde defense at {position}");
        }

        public static void Update(EntityAlive companion, EntityPlayer owner)
        {
            if (companion == null || companion.IsDead() || owner == null || owner.IsDead())
                return;

            int entityId = companion.entityId;

            if (!_lastUpdateTime.ContainsKey(entityId))
                _lastUpdateTime[entityId] = 0f;

            if (Time.time - _lastUpdateTime[entityId] < UpdateInterval)
                return;

            _lastUpdateTime[entityId] = Time.time;

            var data = GetBehaviorData(entityId);

            CheckDayNightCycle(data);
            CheckHordeNight(data);

            switch (data.Mode)
            {
                case AdvancedBehaviorMode.Patrol:
                    UpdatePatrolBehavior(companion, owner, data);
                    break;
                case AdvancedBehaviorMode.Guard:
                    UpdateGuardBehavior(companion, owner, data);
                    break;
                case AdvancedBehaviorMode.Escort:
                    UpdateEscortBehavior(companion, owner, data);
                    break;
                case AdvancedBehaviorMode.Scout:
                    UpdateScoutBehavior(companion, owner, data);
                    break;
                case AdvancedBehaviorMode.HordeDefense:
                    UpdateHordeDefenseBehavior(companion, owner, data);
                    break;
            }
        }

        private static void CheckDayNightCycle(AdvancedBehaviorData data)
        {
            if (GameManager.Instance == null)
                return;

            bool isDay = GameManager.Instance.IsDaytime();

            if (isDay && data.Mode == AdvancedBehaviorMode.Follow)
            {
                // Day behavior - normal follow
            }
            else if (!isDay && data.Mode == AdvancedBehaviorMode.Follow)
            {
                // Night behavior - more alert
            }
        }

        private static void CheckHordeNight(AdvancedBehaviorData data)
        {
            if (GameManager.Instance == null)
                return;

            int dayNumber = GameUtils.WorldTimeToDays(GameManager.Instance.World.worldTime);
            bool isBloodMoon = dayNumber % 7 == 0;

            if (isBloodMoon && !data.IsHordeNight)
            {
                data.IsHordeNight = true;
                Log.Out("[CompanionBot] Blood moon detected - switching to horde defense");
            }
            else if (!isBloodMoon && data.IsHordeNight)
            {
                data.IsHordeNight = false;
                data.Mode = AdvancedBehaviorMode.Follow;
                Log.Out("[CompanionBot] Blood moon ended - returning to normal behavior");
            }
        }

        private static void UpdatePatrolBehavior(EntityAlive companion, EntityPlayer owner, AdvancedBehaviorData data)
        {
            if (data.PatrolWaypoints.Count == 0)
            {
                data.Mode = AdvancedBehaviorMode.Follow;
                return;
            }

            var currentWaypoint = data.PatrolWaypoints[data.CurrentWaypointIndex];
            float distanceToWaypoint = Vector3.Distance(companion.position, currentWaypoint.Position);

            if (distanceToWaypoint < 2f)
            {
                if (data.WaypointArrivalTime == 0f)
                {
                    data.WaypointArrivalTime = Time.time;
                    Log.Out($"[CompanionBot] Companion {companion.entityId} reached waypoint {data.CurrentWaypointIndex}");
                }

                if (Time.time - data.WaypointArrivalTime >= currentWaypoint.WaitTime)
                {
                    data.CurrentWaypointIndex = (data.CurrentWaypointIndex + 1) % data.PatrolWaypoints.Count;
                    data.WaypointArrivalTime = 0f;
                }
            }
            else
            {
                MoveTowards(companion, currentWaypoint.Position);
            }

            CheckForEnemies(companion, owner, 25f);
        }

        private static void UpdateGuardBehavior(EntityAlive companion, EntityPlayer owner, AdvancedBehaviorData data)
        {
            float distanceToCenter = Vector3.Distance(companion.position, data.GuardCenter);

            if (distanceToCenter > data.GuardRadius)
            {
                MoveTowards(companion, data.GuardCenter);
            }
            else
            {
                CheckForEnemies(companion, owner, data.GuardRadius);
            }
        }

        private static void UpdateEscortBehavior(EntityAlive companion, EntityPlayer owner, AdvancedBehaviorData data)
        {
            float distanceToOwner = Vector3.Distance(companion.position, owner.position);

            if (distanceToOwner > data.EscortDistance)
            {
                MoveTowards(companion, owner.position);
            }
            else
            {
                CheckForEnemies(companion, owner, 30f);

                if (owner.Health < owner.GetMaxHealth() * 0.5f)
                {
                    Log.Out($"[CompanionBot] Escort: Owner health low, staying close");
                    float closeDistance = Vector3.Distance(companion.position, owner.position);
                    if (closeDistance > 3f)
                    {
                        MoveTowards(companion, owner.position);
                    }
                }
            }
        }

        private static void UpdateScoutBehavior(EntityAlive companion, EntityPlayer owner, AdvancedBehaviorData data)
        {
            if (!data.IsScouting)
            {
                data.Mode = AdvancedBehaviorMode.Follow;
                return;
            }

            float distanceToOrigin = Vector3.Distance(companion.position, data.ScoutOrigin);

            if (distanceToOrigin > data.ScoutRadius)
            {
                MoveTowards(companion, data.ScoutOrigin);
            }
            else
            {
                var enemies = FindEnemiesInRange(companion, data.ScoutRadius);

                if (enemies.Count > 0 && Time.time - data.LastScoutReportTime > 10f)
                {
                    data.LastScoutReportTime = Time.time;
                    Log.Out($"[CompanionBot] Scout report: {enemies.Count} enemies detected");

                    if (ModMain.Chat != null)
                    {
                        _ = ModMain.Chat.SendMessage("scout_report", $"Обнаружено {enemies.Count} врагов в радиусе {data.ScoutRadius}м");
                    }
                }
            }
        }

        private static void UpdateHordeDefenseBehavior(EntityAlive companion, EntityPlayer owner, AdvancedBehaviorData data)
        {
            float distanceToPosition = Vector3.Distance(companion.position, data.HordeDefensePosition);

            if (distanceToPosition > 5f)
            {
                MoveTowards(companion, data.HordeDefensePosition);
            }
            else
            {
                CheckForEnemies(companion, owner, 40f);
            }
        }

        private static void CheckForEnemies(EntityAlive companion, EntityPlayer owner, float range)
        {
            var enemies = FindEnemiesInRange(companion, range);

            if (enemies.Count > 0)
            {
                var nearestEnemy = FindNearestEnemy(companion, enemies);
                if (nearestEnemy != null)
                {
                    companion.SetAttackTarget(nearestEnemy);
                }
            }
        }

        private static List<EntityAlive> FindEnemiesInRange(EntityAlive companion, float range)
        {
            var enemies = new List<EntityAlive>();

            if (GameManager.Instance?.World?.Entities?.list == null)
                return enemies;

            foreach (var entity in GameManager.Instance.World.Entities.list)
            {
                if (entity == null || entity.IsDead())
                    continue;

                if (!(entity is EntityZombie) && !(entity is EntityEnemyAnimal))
                    continue;

                float distance = Vector3.Distance(companion.position, entity.position);
                if (distance <= range)
                {
                    enemies.Add(entity);
                }
            }

            return enemies;
        }

        private static EntityAlive FindNearestEnemy(EntityAlive companion, List<EntityAlive> enemies)
        {
            EntityAlive nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                float distance = Vector3.Distance(companion.position, enemy.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private static void MoveTowards(EntityAlive companion, Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - companion.position).normalized;
            companion.Move(direction * companion.MoveSpeed);
            companion.RotateToTarget(targetPosition);
        }

        public static void RemoveBehaviorData(int entityId)
        {
            if (_behaviorData.ContainsKey(entityId))
                _behaviorData.Remove(entityId);
            if (_lastUpdateTime.ContainsKey(entityId))
                _lastUpdateTime.Remove(entityId);
        }
    }
}
