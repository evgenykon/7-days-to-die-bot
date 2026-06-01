using System;
using System.Collections.Generic;

namespace CompanionBot
{
    public enum CompanionClass
    {
        Soldier,
        Medic,
        Engineer,
        Scout,
        Guardian
    }

    public enum PersonalityTrait
    {
        Aggressive,
        Defensive,
        Balanced,
        Cautious,
        Brave
    }

    public class CompanionSkill
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public int MaxLevel { get; set; }
        public float Experience { get; set; }
        public float ExperienceToNextLevel { get; set; }

        public CompanionSkill(string name, int maxLevel = 10)
        {
            Name = name;
            Level = 1;
            MaxLevel = maxLevel;
            Experience = 0f;
            ExperienceToNextLevel = 100f;
        }

        public void AddExperience(float amount)
        {
            Experience += amount;
            while (Experience >= ExperienceToNextLevel && Level < MaxLevel)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Experience -= ExperienceToNextLevel;
            Level++;
            ExperienceToNextLevel *= 1.5f;
            Log.Out($"[CompanionBot] Skill {Name} leveled up to {Level}");
        }
    }

    public class CompanionProfile
    {
        public string Name { get; set; }
        public CompanionClass Class { get; set; }
        public PersonalityTrait Personality { get; set; }
        public int Level { get; set; }
        public float Experience { get; set; }
        public float ExperienceToNextLevel { get; set; }
        public Dictionary<string, CompanionSkill> Skills { get; set; }
        public int TotalKills { get; set; }
        public float TotalDistanceTraveled { get; set; }
        public DateTime CreationTime { get; set; }

        public CompanionProfile()
        {
            Name = "Companion";
            Class = CompanionClass.Soldier;
            Personality = PersonalityTrait.Balanced;
            Level = 1;
            Experience = 0f;
            ExperienceToNextLevel = 100f;
            Skills = new Dictionary<string, CompanionSkill>();
            TotalKills = 0;
            TotalDistanceTraveled = 0f;
            CreationTime = DateTime.Now;

            InitializeDefaultSkills();
        }

        private void InitializeDefaultSkills()
        {
            Skills["Combat"] = new CompanionSkill("Combat", 20);
            Skills["Endurance"] = new CompanionSkill("Endurance", 15);
            Skills["Perception"] = new CompanionSkill("Perception", 15);
            Skills["Leadership"] = new CompanionSkill("Leadership", 10);
        }

        public void AddExperience(float amount)
        {
            Experience += amount;
            while (Experience >= ExperienceToNextLevel)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Experience -= ExperienceToNextLevel;
            Level++;
            ExperienceToNextLevel *= 1.3f;
            Log.Out($"[CompanionBot] Companion leveled up to {Level}");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("level_up", $"Уровень повышен до {Level}!");
            }
        }

        public void AddKill()
        {
            TotalKills++;
            AddExperience(10f);
            if (Skills.ContainsKey("Combat"))
            {
                Skills["Combat"].AddExperience(5f);
            }
        }

        public void AddDistanceTraveled(float distance)
        {
            TotalDistanceTraveled += distance;
            if (Skills.ContainsKey("Endurance"))
            {
                Skills["Endurance"].AddExperience(distance * 0.1f);
            }
        }

        public float GetCombatModifier()
        {
            float modifier = 1.0f;
            
            if (Skills.ContainsKey("Combat"))
            {
                modifier += Skills["Combat"].Level * 0.05f;
            }

            switch (Personality)
            {
                case PersonalityTrait.Aggressive:
                    modifier *= 1.2f;
                    break;
                case PersonalityTrait.Defensive:
                    modifier *= 0.9f;
                    break;
                case PersonalityTrait.Brave:
                    modifier *= 1.1f;
                    break;
            }

            switch (Class)
            {
                case CompanionClass.Soldier:
                    modifier *= 1.15f;
                    break;
                case CompanionClass.Guardian:
                    modifier *= 1.1f;
                    break;
            }

            return modifier;
        }

        public float GetDefenseModifier()
        {
            float modifier = 1.0f;

            if (Skills.ContainsKey("Endurance"))
            {
                modifier += Skills["Endurance"].Level * 0.03f;
            }

            switch (Personality)
            {
                case PersonalityTrait.Defensive:
                    modifier *= 1.2f;
                    break;
                case PersonalityTrait.Cautious:
                    modifier *= 1.15f;
                    break;
            }

            switch (Class)
            {
                case CompanionClass.Guardian:
                    modifier *= 1.2f;
                    break;
                case CompanionClass.Medic:
                    modifier *= 1.1f;
                    break;
            }

            return modifier;
        }

        public float GetPerceptionModifier()
        {
            float modifier = 1.0f;

            if (Skills.ContainsKey("Perception"))
            {
                modifier += Skills["Perception"].Level * 0.05f;
            }

            switch (Class)
            {
                case CompanionClass.Scout:
                    modifier *= 1.3f;
                    break;
            }

            return modifier;
        }

        public string GetStatusReport()
        {
            return $"Name: {Name}\n" +
                   $"Class: {Class}\n" +
                   $"Personality: {Personality}\n" +
                   $"Level: {Level} (XP: {Experience:F0}/{ExperienceToNextLevel:F0})\n" +
                   $"Total Kills: {TotalKills}\n" +
                   $"Distance Traveled: {TotalDistanceTraveled:F0}m";
        }
    }

    public static class ProfileManager
    {
        private static Dictionary<int, CompanionProfile> _profiles = new Dictionary<int, CompanionProfile>();

        public static CompanionProfile GetProfile(int entityId)
        {
            if (!_profiles.ContainsKey(entityId))
            {
                _profiles[entityId] = new CompanionProfile();
            }
            return _profiles[entityId];
        }

        public static void SetProfile(int entityId, CompanionProfile profile)
        {
            _profiles[entityId] = profile;
        }

        public static void RemoveProfile(int entityId)
        {
            if (_profiles.ContainsKey(entityId))
            {
                _profiles.Remove(entityId);
            }
        }

        public static void SetName(int entityId, string name)
        {
            var profile = GetProfile(entityId);
            profile.Name = name;
            Log.Out($"[CompanionBot] Companion {entityId} renamed to {name}");
        }

        public static void SetClass(int entityId, CompanionClass companionClass)
        {
            var profile = GetProfile(entityId);
            profile.Class = companionClass;
            Log.Out($"[CompanionBot] Companion {entityId} class set to {companionClass}");
        }

        public static void SetPersonality(int entityId, PersonalityTrait personality)
        {
            var profile = GetProfile(entityId);
            profile.Personality = personality;
            Log.Out($"[CompanionBot] Companion {entityId} personality set to {personality}");
        }

        public static void RecordKill(int entityId)
        {
            var profile = GetProfile(entityId);
            profile.AddKill();
        }

        public static void RecordDistance(int entityId, float distance)
        {
            var profile = GetProfile(entityId);
            profile.AddDistanceTraveled(distance);
        }

        public static Dictionary<int, CompanionProfile> GetAllProfiles()
        {
            return new Dictionary<int, CompanionProfile>(_profiles);
        }
    }
}
