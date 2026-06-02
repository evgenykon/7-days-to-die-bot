using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public enum CompanionState
    {
        Follow,
        Stay,
        Guard
    }

    public class CompanionData
    {
        public EntityAlive Entity { get; set; }
        public EntityPlayer Owner { get; set; }
        public CompanionState State { get; set; }
        public Vector3 GuardPosition { get; set; }
        public float GuardRadius { get; set; }
        public string Gender { get; set; }
        public DateTime SpawnTime { get; set; }

        public CompanionData(EntityAlive entity, EntityPlayer owner, string gender)
        {
            Entity = entity;
            Owner = owner;
            State = CompanionState.Follow;
            GuardPosition = Vector3.zero;
            GuardRadius = 10f;
            Gender = gender;
            SpawnTime = DateTime.Now;
        }
    }

    public static class CompanionManager
    {
        private static Dictionary<int, CompanionData> _companions = new Dictionary<int, CompanionData>();

        public static void RegisterCompanion(EntityAlive companion, EntityPlayer owner, string gender)
        {
            var data = new CompanionData(companion, owner, gender);
            _companions[companion.entityId] = data;
            Log.Out($"[CompanionBot] Registered companion: {companion.entityId}, owner: {owner.EntityName}, gender: {gender}");
        }

        public static void UnregisterCompanion(int entityId)
        {
            if (_companions.ContainsKey(entityId))
            {
                _companions.Remove(entityId);
                CombatSystem.CleanupStats(entityId);
                InventorySystem.RemoveInventory(entityId);
                Log.Out($"[CompanionBot] Unregistered companion: {entityId}");
            }
        }

        public static CompanionData GetCompanion(int entityId)
        {
            return _companions.ContainsKey(entityId) ? _companions[entityId] : null;
        }

        public static List<CompanionData> GetAllCompanions()
        {
            return new List<CompanionData>(_companions.Values);
        }

        public static CompanionData GetCompanionByOwner(EntityPlayer owner)
        {
            if (owner == null)
                return null;

            foreach (var data in _companions.Values)
            {
                if (data.Owner != null && data.Owner.entityId == owner.entityId)
                    return data;
            }
            return null;
        }

        public static List<CompanionData> GetCompanionsByOwner(EntityPlayer owner)
        {
            var result = new List<CompanionData>();
            if (owner == null)
                return result;

            foreach (var data in _companions.Values)
            {
                if (data.Owner != null && data.Owner.entityId == owner.entityId)
                    result.Add(data);
            }
            return result;
        }

        public static void SetState(int entityId, CompanionState state)
        {
            if (_companions.ContainsKey(entityId))
            {
                _companions[entityId].State = state;
                Log.Out($"[CompanionBot] Companion {entityId} state changed to {state}");
            }
        }

        public static void SetGuardPosition(int entityId, Vector3 position, float radius = 10f)
        {
            if (_companions.ContainsKey(entityId))
            {
                _companions[entityId].GuardPosition = position;
                _companions[entityId].GuardRadius = radius;
                _companions[entityId].State = CompanionState.Guard;
                Log.Out($"[CompanionBot] Companion {entityId} guard position set to {position}, radius {radius}");
            }
        }

        public static CompanionState GetState(int entityId)
        {
            return _companions.ContainsKey(entityId) ? _companions[entityId].State : CompanionState.Follow;
        }

        public static void Cleanup()
        {
            var toRemove = new List<int>();
            foreach (var kvp in _companions)
            {
                if (kvp.Value.Entity == null || kvp.Value.Entity.IsDead())
                {
                    toRemove.Add(kvp.Key);
                }
                else if (kvp.Value.Owner == null || kvp.Value.Owner.IsDead())
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var id in toRemove)
            {
                Log.Out($"[CompanionBot] Cleanup: removing companion {id}");
                _companions.Remove(id);
            }

            if (toRemove.Count > 0)
            {
                Log.Out($"[CompanionBot] Cleanup completed, removed {toRemove.Count} companions");
            }
        }
    }
}
