using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace CompanionBot
{
    public class CompanionSaveData
    {
        public int EntityId { get; set; }
        public int OwnerEntityId { get; set; }
        public string Gender { get; set; }
        public CompanionState State { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 GuardPosition { get; set; }
        public float GuardRadius { get; set; }
        public string EntityType { get; set; }
        public DateTime SpawnTime { get; set; }
        public CompanionInventory Inventory { get; set; }
    }

    public class SaveData
    {
        public List<CompanionSaveData> Companions { get; set; } = new List<CompanionSaveData>();
    }

    public static class SaveSystem
    {
        private static string _saveFilePath;

        public static void Initialize()
        {
            var modDir = Path.GetDirectoryName(typeof(SaveSystem).Assembly.Location);
            _saveFilePath = Path.Combine(modDir, "Data", "companions_save.json");

            var dataDir = Path.GetDirectoryName(_saveFilePath);
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            Log.Out($"[CompanionBot] SaveSystem initialized, path: {_saveFilePath}");
        }

        public static void SaveCompanions()
        {
            try
            {
                if (string.IsNullOrEmpty(_saveFilePath))
                {
                    Initialize();
                }

                var saveData = new SaveData();
                var companions = CompanionManager.GetAllCompanions();

                foreach (var companion in companions)
                {
                    if (companion.Entity == null || companion.Entity.IsDead())
                        continue;

                    if (companion.Owner == null || companion.Owner.IsDead())
                        continue;

                    var entityTypeName = GetEntityTypeName(companion.Entity);
                    var inventory = InventorySystem.GetInventory(companion.Entity.entityId);

                    var data = new CompanionSaveData
                    {
                        EntityId = companion.Entity.entityId,
                        OwnerEntityId = companion.Owner.entityId,
                        Gender = companion.Gender,
                        State = companion.State,
                        Position = companion.Entity.position,
                        GuardPosition = companion.GuardPosition,
                        GuardRadius = companion.GuardRadius,
                        EntityType = entityTypeName,
                        SpawnTime = companion.SpawnTime,
                        Inventory = inventory
                    };

                    saveData.Companions.Add(data);
                }

                var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                File.WriteAllText(_saveFilePath, json);

                Log.Out($"[CompanionBot] Saved {saveData.Companions.Count} companions");
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to save companions: {ex.Message}");
            }
        }

        public static void LoadCompanions()
        {
            try
            {
                if (string.IsNullOrEmpty(_saveFilePath))
                {
                    Initialize();
                }

                if (!File.Exists(_saveFilePath))
                {
                    Log.Out("[CompanionBot] No save file found");
                    return;
                }

                var json = File.ReadAllText(_saveFilePath);
                var saveData = JsonConvert.DeserializeObject<SaveData>(json);

                if (saveData == null || saveData.Companions == null)
                {
                    Log.Out("[CompanionBot] Save file is empty or invalid");
                    return;
                }

                Log.Out($"[CompanionBot] Loading {saveData.Companions.Count} companions...");

                foreach (var data in saveData.Companions)
                {
                    SpawnCompanionFromSave(data);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to load companions: {ex.Message}");
            }
        }

        private static void SpawnCompanionFromSave(CompanionSaveData data)
        {
            try
            {
                var owner = FindPlayerById(data.OwnerEntityId);
                if (owner == null)
                {
                    owner = GameManager.Instance.World.GetPrimaryPlayer();
                }

                if (owner == null)
                {
                    Log.Error($"[CompanionBot] Cannot find owner for companion {data.EntityId}");
                    return;
                }

                var spawnedEntity = GameApi.CreateEntity(data.EntityType, data.Position);
                if (spawnedEntity == null)
                {
                    Log.Error($"[CompanionBot] Failed to create entity: {data.EntityType}");
                    return;
                }
                int entityId = spawnedEntity.entityId;

                if (entityId <= 0)
                {
                    Log.Error($"[CompanionBot] Failed to spawn companion {data.EntityType}");
                    return;
                }

                var entity = GameManager.Instance.World.GetEntity(entityId) as EntityAlive;
                if (entity == null)
                {
                    Log.Error($"[CompanionBot] Failed to get entity {entityId}");
                    return;
                }

                CompanionManager.RegisterCompanion(entity, owner, data.Gender);

                var companionData = CompanionManager.GetCompanion(entityId);
                if (companionData != null)
                {
                    companionData.State = data.State;
                    companionData.GuardPosition = data.GuardPosition;
                    companionData.GuardRadius = data.GuardRadius;
                    companionData.SpawnTime = data.SpawnTime;
                }

                if (data.Inventory != null)
                {
                    InventorySystem.LoadInventory(entityId, data.Inventory);
                    Log.Out($"[CompanionBot] Loaded inventory for companion {entityId}");
                }

                Log.Out($"[CompanionBot] Restored companion {entityId} ({data.EntityType}) for player {owner.EntityName}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to restore companion {data.EntityId}: {ex.Message}");
            }
        }

        private static EntityPlayer FindPlayerById(int entityId)
        {
            if (GameManager.Instance?.World?.Players?.list == null)
                return null;

            foreach (var player in GameManager.Instance.World.Players.list)
            {
                if (player != null && player.entityId == entityId)
                    return player;
            }

            return null;
        }

        private static string GetEntityTypeName(EntityAlive entity)
        {
            if (entity == null)
                return "companionbot";

            var entityClass = entity.GetType().Name;

            if (entityClass.Contains("Female") || entityClass.Contains("female"))
            {
                if (entityClass.Contains("Armed") || entityClass.Contains("armed"))
                    return "companionbotfemalearmed";
                return "companionbotfemale";
            }
            else
            {
                if (entityClass.Contains("Armed") || entityClass.Contains("armed"))
                    return "companionbotarmed";
                return "companionbot";
            }
        }

        public static void ClearSave()
        {
            try
            {
                if (File.Exists(_saveFilePath))
                {
                    File.Delete(_saveFilePath);
                    Log.Out("[CompanionBot] Save file cleared");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to clear save: {ex.Message}");
            }
        }
    }
}
