using System;
using System.Collections.Generic;
using HarmonyLib;

namespace CompanionBot
{
    public class MemoryLogger
    {
        private readonly RAGSystem _ragSystem;

        public MemoryLogger(RAGSystem ragSystem)
        {
            _ragSystem = ragSystem;
        }

        public async void LogKill(EntityAlive killer, EntityAlive victim)
        {
            var eventType = killer is EntityPlayer ? "player_kill" : "companion_kill";
            var description = $"{killer.EntityName} killed {victim.EntityName}";
            var metadata = new Dictionary<string, string>
            {
                { "killer", killer.EntityName },
                { "victim", victim.EntityName },
                { "killer_type", killer.GetType().Name },
                { "victim_type", victim.GetType().Name }
            };

            await _ragSystem.IndexEvent(eventType, description, metadata);
        }

        public async void LogDeath(EntityAlive entity, EntityAlive killer = null)
        {
            var eventType = entity is EntityPlayer ? "player_death" : "companion_death";
            var description = killer != null
                ? $"{entity.EntityName} was killed by {killer.EntityName}"
                : $"{entity.EntityName} died";

            var metadata = new Dictionary<string, string>
            {
                { "entity", entity.EntityName },
                { "entity_type", entity.GetType().Name }
            };

            if (killer != null)
                metadata["killer"] = killer.EntityName;

            await _ragSystem.IndexEvent(eventType, description, metadata);
        }

        public async void LogCrafting(string itemName, int count = 1)
        {
            var description = $"Crafted {count}x {itemName}";
            var metadata = new Dictionary<string, string>
            {
                { "item", itemName },
                { "count", count.ToString() }
            };

            await _ragSystem.IndexEvent("crafting", description, metadata);
        }

        public async void LogBuilding(string blockName, int count = 1)
        {
            var description = $"Built {count}x {blockName}";
            var metadata = new Dictionary<string, string>
            {
                { "block", blockName },
                { "count", count.ToString() }
            };

            await _ragSystem.IndexEvent("building", description, metadata);
        }

        public async void LogLoot(string itemName, int count = 1)
        {
            var description = $"Found {count}x {itemName}";
            var metadata = new Dictionary<string, string>
            {
                { "item", itemName },
                { "count", count.ToString() }
            };

            await _ragSystem.IndexEvent("loot", description, metadata);
        }

        public async void LogHordeNightStart()
        {
            await _ragSystem.IndexEvent("horde_night", "Horde night started");
        }

        public async void LogHordeNightEnd(bool survived)
        {
            var description = survived ? "Survived horde night" : "Failed horde night";
            await _ragSystem.IndexEvent("horde_night", description);
        }

        public async void LogExploration(string locationName)
        {
            var description = $"Explored {locationName}";
            var metadata = new Dictionary<string, string>
            {
                { "location", locationName }
            };

            await _ragSystem.IndexEvent("exploration", description, metadata);
        }

        public async void LogDayNightCycle(bool isDay, int dayNumber)
        {
            var description = isDay ? $"Day {dayNumber} started" : $"Night {dayNumber} started";
            var metadata = new Dictionary<string, string>
            {
                { "day_number", dayNumber.ToString() },
                { "is_day", isDay.ToString() }
            };

            await _ragSystem.IndexEvent("time_cycle", description, metadata);
        }

        public async void LogPlayerAction(string action, string details = null)
        {
            var description = string.IsNullOrEmpty(details) ? action : $"{action}: {details}";
            await _ragSystem.IndexEvent("player_action", description);
        }
    }
}
