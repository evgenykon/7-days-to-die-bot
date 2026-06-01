using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public enum QuestType
    {
        KillZombies,
        GatherResources,
        ExploreLocation,
        CraftItem,
        BuildStructure,
        EscortNPC,
        SurviveHorde
    }

    public enum QuestStatus
    {
        Available,
        Active,
        Completed,
        Failed
    }

    public class Quest
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public QuestType Type { get; set; }
        public QuestStatus Status { get; set; }
        public int TargetCount { get; set; }
        public int CurrentProgress { get; set; }
        public int RewardXP { get; set; }
        public Dictionary<string, int> RewardItems { get; set; }
        public float TimeLimit { get; set; }
        public DateTime StartTime { get; set; }
        public int GiverEntityId { get; set; }

        public Quest()
        {
            RewardItems = new Dictionary<string, int>();
            Status = QuestStatus.Available;
            CurrentProgress = 0;
        }

        public bool IsCompleted()
        {
            return CurrentProgress >= TargetCount;
        }

        public float GetProgressPercent()
        {
            return TargetCount > 0 ? (float)CurrentProgress / TargetCount : 0f;
        }

        public float GetTimeRemaining()
        {
            if (TimeLimit <= 0)
                return -1f;

            var elapsed = (float)(DateTime.Now - StartTime).TotalSeconds;
            return Math.Max(0, TimeLimit - elapsed);
        }

        public bool IsExpired()
        {
            if (TimeLimit <= 0)
                return false;

            return GetTimeRemaining() <= 0;
        }
    }

    public static class QuestSystem
    {
        private static Dictionary<int, List<Quest>> _activeQuests = new Dictionary<int, List<Quest>>();
        private static Dictionary<int, List<Quest>> _availableQuests = new Dictionary<int, List<Quest>>();
        private static int _questIdCounter = 1;

        public static List<Quest> GetActiveQuests(int companionEntityId)
        {
            if (!_activeQuests.ContainsKey(companionEntityId))
            {
                _activeQuests[companionEntityId] = new List<Quest>();
            }
            return _activeQuests[companionEntityId];
        }

        public static List<Quest> GetAvailableQuests(int companionEntityId)
        {
            if (!_availableQuests.ContainsKey(companionEntityId))
            {
                _availableQuests[companionEntityId] = GenerateQuests(companionEntityId);
            }
            return _availableQuests[companionEntityId];
        }

        private static List<Quest> GenerateQuests(int companionEntityId)
        {
            var quests = new List<Quest>();
            var profile = ProfileManager.GetProfile(companionEntityId);
            var random = new System.Random();

            quests.Add(new Quest
            {
                Id = $"quest_{_questIdCounter++}",
                Title = "Zombie Hunter",
                Description = $"Kill {10 + profile.Level * 5} zombies",
                Type = QuestType.KillZombies,
                TargetCount = 10 + profile.Level * 5,
                RewardXP = 50 + profile.Level * 10,
                GiverEntityId = companionEntityId,
                TimeLimit = 600f
            });

            quests.Add(new Quest
            {
                Id = $"quest_{_questIdCounter++}",
                Title = "Resource Gatherer",
                Description = "Gather 50 wood",
                Type = QuestType.GatherResources,
                TargetCount = 50,
                RewardXP = 30,
                GiverEntityId = companionEntityId
            });
            quests[quests.Count - 1].RewardItems["resourceWood"] = 10;

            quests.Add(new Quest
            {
                Id = $"quest_{_questIdCounter++}",
                Title = "Explorer",
                Description = "Explore 3 new locations",
                Type = QuestType.ExploreLocation,
                TargetCount = 3,
                RewardXP = 100,
                GiverEntityId = companionEntityId
            });

            quests.Add(new Quest
            {
                Id = $"quest_{_questIdCounter++}",
                Title = "Craftsman",
                Description = "Craft 5 items",
                Type = QuestType.CraftItem,
                TargetCount = 5,
                RewardXP = 40,
                GiverEntityId = companionEntityId
            });

            quests.Add(new Quest
            {
                Id = $"quest_{_questIdCounter++}",
                Title = "Horde Survivor",
                Description = "Survive the next horde night",
                Type = QuestType.SurviveHorde,
                TargetCount = 1,
                RewardXP = 200,
                GiverEntityId = companionEntityId
            });

            return quests;
        }

        public static bool AcceptQuest(int companionEntityId, string questId)
        {
            var available = GetAvailableQuests(companionEntityId);
            var quest = available.Find(q => q.Id == questId);

            if (quest == null)
            {
                Log.Error($"[CompanionBot] Quest {questId} not found");
                return false;
            }

            quest.Status = QuestStatus.Active;
            quest.StartTime = DateTime.Now;

            var active = GetActiveQuests(companionEntityId);
            active.Add(quest);
            available.Remove(quest);

            Log.Out($"[CompanionBot] Quest accepted: {quest.Title}");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("quest_started", Localization.Get("quest_started", quest.Title));
            }

            return true;
        }

        public static void UpdateQuestProgress(int companionEntityId, QuestType type, int amount = 1)
        {
            var active = GetActiveQuests(companionEntityId);

            foreach (var quest in active)
            {
                if (quest.Type == type && quest.Status == QuestStatus.Active)
                {
                    quest.CurrentProgress += amount;

                    if (quest.IsCompleted())
                    {
                        CompleteQuest(companionEntityId, quest);
                    }
                }
            }
        }

        private static void CompleteQuest(int companionEntityId, Quest quest)
        {
            quest.Status = QuestStatus.Completed;

            var profile = ProfileManager.GetProfile(companionEntityId);
            profile.AddExperience(quest.RewardXP);

            var inventory = InventorySystem.GetInventory(companionEntityId);
            foreach (var reward in quest.RewardItems)
            {
                inventory.AddItem(reward.Key, reward.Value);
            }

            var active = GetActiveQuests(companionEntityId);
            active.Remove(quest);

            Log.Out($"[CompanionBot] Quest completed: {quest.Title} (+{quest.RewardXP} XP)");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("quest_completed", Localization.Get("quest_completed", quest.Title));
            }
        }

        public static void CheckQuestExpiration(int companionEntityId)
        {
            var active = GetActiveQuests(companionEntityId);
            var expired = new List<Quest>();

            foreach (var quest in active)
            {
                if (quest.IsExpired())
                {
                    quest.Status = QuestStatus.Failed;
                    expired.Add(quest);

                    Log.Out($"[CompanionBot] Quest failed (expired): {quest.Title}");

                    if (ModMain.Chat != null)
                    {
                        _ = ModMain.Chat.SendMessage("quest_failed", Localization.Get("quest_failed", quest.Title));
                    }
                }
            }

            foreach (var quest in expired)
            {
                active.Remove(quest);
            }
        }

        public static void ClearQuestData(int companionEntityId)
        {
            if (_activeQuests.ContainsKey(companionEntityId))
                _activeQuests.Remove(companionEntityId);
            if (_availableQuests.ContainsKey(companionEntityId))
                _availableQuests.Remove(companionEntityId);
        }
    }
}
