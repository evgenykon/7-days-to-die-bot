using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public enum DroneMode
    {
        Follow,
        Patrol,
        Scout,
        Attack,
        Support
    }

    public class DroneCompanionData
    {
        public int EntityId { get; set; }
        public EntityPlayer Owner { get; set; }
        public string Name { get; set; }
        public DroneMode Mode { get; set; }
        public float BatteryLevel { get; set; }
        public float MaxBattery { get; set; }
        public float Altitude { get; set; }
        public float ScanRadius { get; set; }
        public int TotalKills { get; set; }
        public int EnemiesDetected { get; set; }
        public DateTime DeployTime { get; set; }
        public Vector3 PatrolCenter { get; set; }
        public float PatrolRadius { get; set; }

        public DroneCompanionData(int entityId, EntityPlayer owner)
        {
            EntityId = entityId;
            Owner = owner;
            Name = "Drone";
            Mode = DroneMode.Follow;
            BatteryLevel = 100f;
            MaxBattery = 100f;
            Altitude = 10f;
            ScanRadius = 50f;
            TotalKills = 0;
            EnemiesDetected = 0;
            DeployTime = DateTime.Now;
            PatrolCenter = Vector3.zero;
            PatrolRadius = 30f;
        }

        public float GetBatteryPercent()
        {
            return MaxBattery > 0 ? (BatteryLevel / MaxBattery) * 100f : 0f;
        }

        public void DrainBattery(float amount)
        {
            BatteryLevel = Math.Max(0f, BatteryLevel - amount);
        }

        public void Recharge(float amount)
        {
            BatteryLevel = Math.Min(MaxBattery, BatteryLevel + amount);
        }

        public bool IsOperational()
        {
            return BatteryLevel > 0f;
        }
    }

    public static class DroneCompanionManager
    {
        private static Dictionary<int, DroneCompanionData> _drones = new Dictionary<int, DroneCompanionData>();
        private static Dictionary<int, float> _lastUpdateTime = new Dictionary<int, float>();
        private static Dictionary<int, float> _lastScanTime = new Dictionary<int, float>();
        private const float UpdateInterval = 0.5f;
        private const float ScanInterval = 5f;
        private const float BatteryDrainPerSecond = 0.1f;
        private const float FollowDistance = 8f;
        private const float AttackRange = 30f;

        public static void RegisterDrone(int entityId, EntityPlayer owner)
        {
            _drones[entityId] = new DroneCompanionData(entityId, owner);
            Log.Out($"[CompanionBot] Drone registered (ID: {entityId})");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("drone_deployed", Localization.Get("drone_deployed"));
            }
        }

        public static void UnregisterDrone(int entityId)
        {
            if (_drones.ContainsKey(entityId))
            {
                _drones.Remove(entityId);
                Log.Out($"[CompanionBot] Drone unregistered: {entityId}");
            }
        }

        public static DroneCompanionData GetDrone(int entityId)
        {
            return _drones.ContainsKey(entityId) ? _drones[entityId] : null;
        }

        public static void UpdateDrone(int entityId, EntityAlive drone)
        {
            var data = GetDrone(entityId);
            if (data == null || drone == null || drone.IsDead())
                return;

            if (!data.IsOperational())
            {
                Log.Out($"[CompanionBot] Drone {entityId} out of battery");
                return;
            }

            if (!_lastUpdateTime.ContainsKey(entityId))
                _lastUpdateTime[entityId] = 0f;

            if (Time.time - _lastUpdateTime[entityId] < UpdateInterval)
                return;

            _lastUpdateTime[entityId] = Time.time;

            data.DrainBattery(BatteryDrainPerSecond * UpdateInterval);

            var owner = data.Owner;
            if (owner == null || owner.IsDead())
                return;

            switch (data.Mode)
            {
                case DroneMode.Follow:
                    UpdateFollowMode(drone, data, owner);
                    break;
                case DroneMode.Patrol:
                    UpdatePatrolMode(drone, data, owner);
                    break;
                case DroneMode.Scout:
                    UpdateScoutMode(drone, data, owner);
                    break;
                case DroneMode.Attack:
                    UpdateAttackMode(drone, data, owner);
                    break;
                case DroneMode.Support:
                    UpdateSupportMode(drone, data, owner);
                    break;
            }

            PerformScanning(entityId, drone, data);
        }

        private static void UpdateFollowMode(EntityAlive drone, DroneCompanionData data, EntityPlayer owner)
        {
            var targetPos = owner.position + new Vector3(0, data.Altitude, 0);
            float distance = Vector3.Distance(drone.position, targetPos);

            if (distance > FollowDistance)
            {
                var direction = (targetPos - drone.position).normalized;
                drone.Move(direction * drone.MoveSpeed);
                drone.RotateToTarget(owner.position);
            }
        }

        private static void UpdatePatrolMode(EntityAlive drone, DroneCompanionData data, EntityPlayer owner)
        {
            var patrolPos = data.PatrolCenter + new Vector3(0, data.Altitude, 0);
            float distance = Vector3.Distance(drone.position, patrolPos);

            if (distance > data.PatrolRadius)
            {
                var direction = (patrolPos - drone.position).normalized;
                drone.Move(direction * drone.MoveSpeed);
            }
            else
            {
                var randomOffset = new Vector3(
                    UnityEngine.Random.Range(-data.PatrolRadius, data.PatrolRadius),
                    0,
                    UnityEngine.Random.Range(-data.PatrolRadius, data.PatrolRadius)
                );
                var targetPos = data.PatrolCenter + randomOffset + new Vector3(0, data.Altitude, 0);
                var direction = (targetPos - drone.position).normalized;
                drone.Move(direction * drone.MoveSpeed * 0.5f);
            }
        }

        private static void UpdateScoutMode(EntityAlive drone, DroneCompanionData data, EntityPlayer owner)
        {
            var scoutPos = owner.position + new Vector3(0, data.Altitude * 2, 0);
            var direction = (scoutPos - drone.position).normalized;
            drone.Move(direction * drone.MoveSpeed);

            if (Vector3.Distance(drone.position, scoutPos) < 5f)
            {
                drone.RotateToTarget(owner.position + new Vector3(50, 0, 0));
            }
        }

        private static void UpdateAttackMode(EntityAlive drone, DroneCompanionData data, EntityPlayer owner)
        {
            var target = FindNearestEnemy(drone, owner, AttackRange);

            if (target != null)
            {
                drone.SetAttackTarget(target);
                var attackPos = target.position + new Vector3(0, data.Altitude, 0);
                var direction = (attackPos - drone.position).normalized;
                drone.Move(direction * drone.MoveSpeed);
            }
            else
            {
                drone.SetAttackTarget(null);
                UpdateFollowMode(drone, data, owner);
            }
        }

        private static void UpdateSupportMode(EntityAlive drone, DroneCompanionData data, EntityPlayer owner)
        {
            var supportPos = owner.position + new Vector3(0, data.Altitude, 5);
            float distance = Vector3.Distance(drone.position, supportPos);

            if (distance > 3f)
            {
                var direction = (supportPos - drone.position).normalized;
                drone.Move(direction * drone.MoveSpeed);
            }

            if (owner.Health < owner.GetMaxHealth() * 0.5f)
            {
                Log.Out($"[CompanionBot] Drone {data.EntityId} providing support - owner health low");
            }
        }

        private static void PerformScanning(int entityId, EntityAlive drone, DroneCompanionData data)
        {
            if (!_lastScanTime.ContainsKey(entityId))
                _lastScanTime[entityId] = 0f;

            if (Time.time - _lastScanTime[entityId] < ScanInterval)
                return;

            _lastScanTime[entityId] = Time.time;

            var enemies = FindEnemiesInRange(drone, data.ScanRadius);
            if (enemies.Count > 0)
            {
                data.EnemiesDetected += enemies.Count;
                Log.Out($"[CompanionBot] Drone {entityId} detected {enemies.Count} enemies in {data.ScanRadius}m radius");

                if (ModMain.Chat != null && enemies.Count >= 3)
                {
                    _ = ModMain.Chat.SendMessage("drone_scan", $"Дрон обнаружил {enemies.Count} врагов");
                }
            }
        }

        private static EntityAlive FindNearestEnemy(EntityAlive drone, EntityPlayer owner, float range)
        {
            EntityAlive nearestEnemy = null;
            float nearestDistance = range;

            if (GameManager.Instance?.World?.Entities?.list == null)
                return null;

            foreach (var entity in GameManager.Instance.World.Entities.list)
            {
                if (entity == null || entity.IsDead())
                    continue;

                if (entity == drone || entity == owner)
                    continue;

                if (entity is EntityPlayer)
                    continue;

                if (!(entity is EntityZombie) && !(entity is EntityEnemyAnimal))
                    continue;

                float distance = Vector3.Distance(drone.position, entity.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = entity;
                }
            }

            return nearestEnemy;
        }

        private static List<EntityAlive> FindEnemiesInRange(EntityAlive drone, float range)
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

                float distance = Vector3.Distance(drone.position, entity.position);
                if (distance <= range)
                {
                    enemies.Add(entity);
                }
            }

            return enemies;
        }

        public static void SetDroneMode(int entityId, DroneMode mode)
        {
            var data = GetDrone(entityId);
            if (data != null)
            {
                data.Mode = mode;
                Log.Out($"[CompanionBot] Drone {entityId} mode set to {mode}");
            }
        }

        public static void SetPatrolArea(int entityId, Vector3 center, float radius)
        {
            var data = GetDrone(entityId);
            if (data != null)
            {
                data.PatrolCenter = center;
                data.PatrolRadius = radius;
                Log.Out($"[CompanionBot] Drone {entityId} patrol area set to {center} with radius {radius}");
            }
        }

        public static void RechargeDrone(int entityId, float amount)
        {
            var data = GetDrone(entityId);
            if (data != null)
            {
                data.Recharge(amount);
                Log.Out($"[CompanionBot] Drone {entityId} recharged (+{amount} battery)");
            }
        }

        public static void RecordDroneKill(int entityId)
        {
            var data = GetDrone(entityId);
            if (data != null)
            {
                data.TotalKills++;
            }
        }

        public static List<DroneCompanionData> GetAllDrones()
        {
            return new List<DroneCompanionData>(_drones.Values);
        }

        public static void ClearDroneData(int entityId)
        {
            if (_lastUpdateTime.ContainsKey(entityId))
                _lastUpdateTime.Remove(entityId);
            if (_lastScanTime.ContainsKey(entityId))
                _lastScanTime.Remove(entityId);
        }
    }
}
