using System;
using System.IO;
using Newtonsoft.Json;

namespace CompanionBot
{
    public class GlobalConfig
    {
        public float FollowDistance { get; set; } = 3f;
        public float MaxFollowDistance { get; set; } = 15f;
        public float AttackRange { get; set; } = 25f;
        public float UpdateInterval { get; set; } = 0.5f;
        public float GuardReturnDistance { get; set; } = 5f;

        public float LowHealthThreshold { get; set; } = 0.3f;
        public float RetreatDistance { get; set; } = 8f;
        public float SafeDistance { get; set; } = 12f;
        public float MeleeRange { get; set; } = 3f;
        public float DodgeDistance { get; set; } = 4f;
        public float ComboWindow { get; set; } = 1.5f;

        public int InventoryMaxCapacity { get; set; } = 20;
        public float WeaponDurabilityLossMelee { get; set; } = 0.1f;
        public float WeaponDurabilityLossRanged { get; set; } = 0.5f;
        public float LootPickupChance { get; set; } = 0.3f;
        public float AutoHealThreshold { get; set; } = 0.5f;

        public float PatrolWaypointArrivalDistance { get; set; } = 2f;
        public float PatrolDefaultWaitTime { get; set; } = 2f;
        public float EscortDefaultDistance { get; set; } = 5f;
        public float ScoutDefaultRadius { get; set; } = 50f;
        public float ScoutReportInterval { get; set; } = 10f;

        public float InteractionRange { get; set; } = 5f;
        public float HealCheckInterval { get; set; } = 10f;
        public float AmmoShareInterval { get; set; } = 15f;
        public float CoordinationInterval { get; set; } = 5f;

        public float RespawnDelay { get; set; } = 60f;
        public float CooldownTime { get; set; } = 300f;
        public bool LoseInventoryOnDeath { get; set; } = false;
        public bool LoseExperienceOnDeath { get; set; } = false;
        public float ExperienceLossPercent { get; set; } = 0.1f;

        public float XpPerKill { get; set; } = 10f;
        public float XpPerDistanceUnit { get; set; } = 0.1f;
        public float LevelUpMultiplier { get; set; } = 1.3f;
        public float SkillLevelUpMultiplier { get; set; } = 1.5f;

        public float FormationDefaultSpacing { get; set; } = 3f;
        public int MaxCompanionsPerPlayer { get; set; } = 5;

        public string Language { get; set; } = "en";
    }

    public static class GlobalConfigManager
    {
        private static GlobalConfig _config;
        private static string _configPath;

        public static GlobalConfig Config
        {
            get
            {
                if (_config == null)
                {
                    LoadConfig();
                }
                return _config;
            }
        }

        public static void Initialize()
        {
            var modDir = Path.GetDirectoryName(typeof(GlobalConfigManager).Assembly.Location);
            _configPath = Path.Combine(modDir, "Config", "global_config.json");
            LoadConfig();
        }

        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonConvert.DeserializeObject<GlobalConfig>(json);
                    Log.Out($"[CompanionBot] Global config loaded from {_configPath}");
                }
                else
                {
                    _config = new GlobalConfig();
                    SaveConfig();
                    Log.Out($"[CompanionBot] Default global config created at {_configPath}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to load global config: {ex.Message}");
                _config = new GlobalConfig();
            }
        }

        public static void SaveConfig()
        {
            try
            {
                var directory = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                Log.Out($"[CompanionBot] Global config saved to {_configPath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to save global config: {ex.Message}");
            }
        }

        public static void ResetToDefaults()
        {
            _config = new GlobalConfig();
            SaveConfig();
            Log.Out($"[CompanionBot] Global config reset to defaults");
        }
    }
}
