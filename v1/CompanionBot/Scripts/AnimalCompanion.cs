using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public enum AnimalType
    {
        Dog,
        Wolf,
        Bear
    }

    public class AnimalCompanionData
    {
        public int EntityId { get; set; }
        public AnimalType Type { get; set; }
        public EntityPlayer Owner { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public float Experience { get; set; }
        public float Loyalty { get; set; }
        public int TotalKills { get; set; }
        public DateTime TamedTime { get; set; }

        public AnimalCompanionData(int entityId, AnimalType type, EntityPlayer owner)
        {
            EntityId = entityId;
            Type = type;
            Owner = owner;
            Name = type.ToString();
            Level = 1;
            Experience = 0f;
            Loyalty = 50f;
            TotalKills = 0;
            TamedTime = DateTime.Now;
        }

        public void AddExperience(float amount)
        {
            Experience += amount;
            float requiredExp = Level * 100f;

            while (Experience >= requiredExp)
            {
                Experience -= requiredExp;
                Level++;
                requiredExp = Level * 100f;
                Log.Out($"[CompanionBot] Animal {Name} leveled up to {Level}");
            }
        }

        public void AddLoyalty(float amount)
        {
            Loyalty = Math.Min(100f, Math.Max(0f, Loyalty + amount));
        }

        public void RecordKill()
        {
            TotalKills++;
            AddExperience(5f);
            AddLoyalty(1f);
        }
    }

    public static class AnimalCompanionManager
    {
        private static Dictionary<int, AnimalCompanionData> _animals = new Dictionary<int, AnimalCompanionData>();
        private static Dictionary<int, float> _lastUpdateTime = new Dictionary<int, float>();
        private const float UpdateInterval = 0.5f;
        private const float FollowDistance = 5f;
        private const float MaxFollowDistance = 20f;
        private const float AttackRange = 10f;

        public static void RegisterAnimal(int entityId, AnimalType type, EntityPlayer owner)
        {
            _animals[entityId] = new AnimalCompanionData(entityId, type, owner);
            Log.Out($"[CompanionBot] Animal companion registered: {type} (ID: {entityId})");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("animal_tamed", Localization.Get("animal_tamed"));
            }
        }

        public static void UnregisterAnimal(int entityId)
        {
            if (_animals.ContainsKey(entityId))
            {
                _animals.Remove(entityId);
                Log.Out($"[CompanionBot] Animal companion unregistered: {entityId}");
            }
        }

        public static AnimalCompanionData GetAnimal(int entityId)
        {
            return _animals.ContainsKey(entityId) ? _animals[entityId] : null;
        }

        public static void UpdateAnimal(int entityId, EntityAlive animal)
        {
            var data = GetAnimal(entityId);
            if (data == null || animal == null || animal.IsDead())
                return;

            if (!_lastUpdateTime.ContainsKey(entityId))
                _lastUpdateTime[entityId] = 0f;

            if (Time.time - _lastUpdateTime[entityId] < UpdateInterval)
                return;

            _lastUpdateTime[entityId] = Time.time;

            var owner = data.Owner;
            if (owner == null || owner.IsDead())
                return;

            float distanceToOwner = Vector3.Distance(animal.position, owner.position);

            var target = FindNearestEnemy(animal, owner);

            if (target != null && Vector3.Distance(animal.position, target.position) <= AttackRange)
            {
                animal.SetAttackTarget(target, 0);
                return;
            }

            animal.SetAttackTarget(null, 0);

            if (distanceToOwner > MaxFollowDistance)
            {
                var teleportPos = owner.position + new Vector3(3, 0, 3);
                animal.position = teleportPos;
                animal.transform.position = teleportPos;
            }
            else if (distanceToOwner > FollowDistance)
            {
                GameApi.MoveTo(animal, owner.position);
                GameApi.LookAt(animal, owner.position);
            }
        }

        private static EntityAlive FindNearestEnemy(EntityAlive animal, EntityPlayer owner)
        {
            EntityAlive nearestEnemy = null;
            float nearestDistance = AttackRange;

            if (GameManager.Instance?.World?.Entities?.list == null)
                return null;

            foreach (var entity in GameManager.Instance.World.Entities.list)
            {
                if (entity == null || entity.IsDead())
                    continue;

                if (entity == animal || entity == owner)
                    continue;

                if (entity is EntityPlayer)
                    continue;

                if (!(entity is EntityZombie) && !(entity is EntityEnemyAnimal))
                    continue;

                float distance = Vector3.Distance(animal.position, entity.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = entity as EntityAlive;
                }
            }

            return nearestEnemy;
        }

        public static void RecordAnimalKill(int entityId)
        {
            var data = GetAnimal(entityId);
            if (data != null)
            {
                data.RecordKill();
            }
        }

        public static void FeedAnimal(int entityId, string foodItem)
        {
            var data = GetAnimal(entityId);
            if (data == null)
                return;

            float loyaltyGain = 10f;
            if (foodItem.Contains("Meat"))
                loyaltyGain = 20f;

            data.AddLoyalty(loyaltyGain);
            Log.Out($"[CompanionBot] Animal {data.Name} fed with {foodItem} (+{loyaltyGain} loyalty)");
        }

        public static List<AnimalCompanionData> GetAllAnimals()
        {
            return new List<AnimalCompanionData>(_animals.Values);
        }

        public static void ClearAnimalData(int entityId)
        {
            if (_lastUpdateTime.ContainsKey(entityId))
                _lastUpdateTime.Remove(entityId);
        }
    }
}
