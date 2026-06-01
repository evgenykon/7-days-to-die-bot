using System;
using System.Collections.Generic;

namespace CompanionBot
{
    public enum DeathConsequence
    {
        Respawn,
        Permadeath,
        Cooldown
    }

    public class DeathConsequenceData
    {
        public DeathConsequence Consequence { get; set; }
        public float RespawnDelay { get; set; }
        public float CooldownTime { get; set; }
        public bool LoseInventory { get; set; }
        public bool LoseExperience { get; set; }
        public float ExperienceLossPercent { get; set; }

        public DeathConsequenceData()
        {
            Consequence = DeathConsequence.Respawn;
            RespawnDelay = 60f;
            CooldownTime = 300f;
            LoseInventory = false;
            LoseExperience = false;
            ExperienceLossPercent = 0.1f;
        }
    }

    public static class DeathConsequenceManager
    {
        private static DeathConsequenceData _currentSettings = new DeathConsequenceData();
        private static Dictionary<int, DateTime> _deathTimes = new Dictionary<int, DateTime>();
        private static Dictionary<int, bool> _permadeadCompanions = new Dictionary<int, bool>();

        public static void SetConsequence(DeathConsequence consequence)
        {
            _currentSettings.Consequence = consequence;
            Log.Out($"[CompanionBot] Death consequence set to: {consequence}");
        }

        public static void SetRespawnDelay(float seconds)
        {
            _currentSettings.RespawnDelay = seconds;
            Log.Out($"[CompanionBot] Respawn delay set to: {seconds}s");
        }

        public static void SetCooldownTime(float seconds)
        {
            _currentSettings.CooldownTime = seconds;
            Log.Out($"[CompanionBot] Cooldown time set to: {seconds}s");
        }

        public static void SetLoseInventory(bool lose)
        {
            _currentSettings.LoseInventory = lose;
            Log.Out($"[CompanionBot] Lose inventory on death: {lose}");
        }

        public static void SetLoseExperience(bool lose, float percent = 0.1f)
        {
            _currentSettings.LoseExperience = lose;
            _currentSettings.ExperienceLossPercent = percent;
            Log.Out($"[CompanionBot] Lose experience on death: {lose} ({percent * 100}%)");
        }

        public static DeathConsequenceData GetCurrentSettings()
        {
            return _currentSettings;
        }

        public static void OnCompanionDeath(int companionEntityId)
        {
            _deathTimes[companionEntityId] = DateTime.Now;

            switch (_currentSettings.Consequence)
            {
                case DeathConsequence.Permadeath:
                    _permadeadCompanions[companionEntityId] = true;
                    Log.Out($"[CompanionBot] Companion {companionEntityId} permanently dead");
                    break;

                case DeathConsequence.Respawn:
                    Log.Out($"[CompanionBot] Companion {companionEntityId} will respawn in {_currentSettings.RespawnDelay}s");
                    break;

                case DeathConsequence.Cooldown:
                    Log.Out($"[CompanionBot] Companion {companionEntityId} on cooldown for {_currentSettings.CooldownTime}s");
                    break;
            }

            if (_currentSettings.LoseInventory)
            {
                var inventory = InventorySystem.GetInventory(companionEntityId);
                inventory.Clear();
                Log.Out($"[CompanionBot] Companion {companionEntityId} lost all inventory items");
            }

            if (_currentSettings.LoseExperience)
            {
                var profile = ProfileManager.GetProfile(companionEntityId);
                float expLoss = profile.Experience * _currentSettings.ExperienceLossPercent;
                profile.Experience = Math.Max(0, profile.Experience - expLoss);
                Log.Out($"[CompanionBot] Companion {companionEntityId} lost {expLoss} experience");
            }
        }

        public static bool CanRespawn(int companionEntityId)
        {
            if (_permadeadCompanions.ContainsKey(companionEntityId) && _permadeadCompanions[companionEntityId])
            {
                return false;
            }

            if (!_deathTimes.ContainsKey(companionEntityId))
            {
                return true;
            }

            var deathTime = _deathTimes[companionEntityId];
            var timeSinceDeath = (DateTime.Now - deathTime).TotalSeconds;

            switch (_currentSettings.Consequence)
            {
                case DeathConsequence.Respawn:
                    return timeSinceDeath >= _currentSettings.RespawnDelay;

                case DeathConsequence.Cooldown:
                    return timeSinceDeath >= _currentSettings.CooldownTime;

                case DeathConsequence.Permadeath:
                    return false;

                default:
                    return true;
            }
        }

        public static float GetRespawnTimeRemaining(int companionEntityId)
        {
            if (!_deathTimes.ContainsKey(companionEntityId))
                return 0f;

            var deathTime = _deathTimes[companionEntityId];
            var timeSinceDeath = (float)(DateTime.Now - deathTime).TotalSeconds;

            float requiredTime = _currentSettings.Consequence == DeathConsequence.Respawn
                ? _currentSettings.RespawnDelay
                : _currentSettings.CooldownTime;

            return Math.Max(0, requiredTime - timeSinceDeath);
        }

        public static bool IsPermadead(int companionEntityId)
        {
            return _permadeadCompanions.ContainsKey(companionEntityId) && _permadeadCompanions[companionEntityId];
        }

        public static void ClearDeathData(int companionEntityId)
        {
            if (_deathTimes.ContainsKey(companionEntityId))
                _deathTimes.Remove(companionEntityId);
            if (_permadeadCompanions.ContainsKey(companionEntityId))
                _permadeadCompanions.Remove(companionEntityId);
        }
    }
}
