using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public enum BuildingTask
    {
        None,
        Repair,
        Upgrade,
        Build
    }

    public static class BaseBuildingAssistant
    {
        private const float RepairRange = 5f;
        private const float RepairInterval = 2f;
        private const float UpgradeInterval = 5f;
        private const int RepairHealthPerTick = 50;
        private const int UpgradeProgressPerTick = 10;

        private static Dictionary<int, BuildingTask> _currentTasks = new Dictionary<int, BuildingTask>();
        private static Dictionary<int, Vector3> _taskPositions = new Dictionary<int, Vector3>();
        private static Dictionary<int, float> _lastActionTime = new Dictionary<int, float>();
        private static Dictionary<int, int> _taskTargets = new Dictionary<int, int>();

        public static void SetRepairTask(int companionEntityId, Vector3 position, int blockEntityId)
        {
            _currentTasks[companionEntityId] = BuildingTask.Repair;
            _taskPositions[companionEntityId] = position;
            _taskTargets[companionEntityId] = blockEntityId;
            _lastActionTime[companionEntityId] = 0f;

            Log.Out($"[CompanionBot] Companion {companionEntityId} assigned to repair task at {position}");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("building_repair", Localization.Get("building_repair"));
            }
        }

        public static void SetUpgradeTask(int companionEntityId, Vector3 position, int blockEntityId)
        {
            _currentTasks[companionEntityId] = BuildingTask.Upgrade;
            _taskPositions[companionEntityId] = position;
            _taskTargets[companionEntityId] = blockEntityId;
            _lastActionTime[companionEntityId] = 0f;

            Log.Out($"[CompanionBot] Companion {companionEntityId} assigned to upgrade task at {position}");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("building_upgrade", Localization.Get("building_upgrade"));
            }
        }

        public static void CancelTask(int companionEntityId)
        {
            if (_currentTasks.ContainsKey(companionEntityId))
            {
                _currentTasks[companionEntityId] = BuildingTask.None;
                Log.Out($"[CompanionBot] Companion {companionEntityId} building task cancelled");
            }
        }

        public static void UpdateBuildingTasks(int companionEntityId, EntityAlive companion)
        {
            if (!_currentTasks.ContainsKey(companionEntityId))
                return;

            var task = _currentTasks[companionEntityId];
            if (task == BuildingTask.None)
                return;

            var taskPos = _taskPositions[companionEntityId];
            float distance = Vector3.Distance(companion.position, taskPos);

            if (distance > RepairRange)
            {
                GameApi.MoveTo(companion, taskPos);
                return;
            }

            if (!_lastActionTime.ContainsKey(companionEntityId))
                _lastActionTime[companionEntityId] = 0f;

            float interval = task == BuildingTask.Repair ? RepairInterval : UpgradeInterval;

            if (Time.time - _lastActionTime[companionEntityId] < interval)
                return;

            _lastActionTime[companionEntityId] = Time.time;

            if (task == BuildingTask.Repair)
            {
                PerformRepair(companionEntityId);
            }
            else if (task == BuildingTask.Upgrade)
            {
                PerformUpgrade(companionEntityId);
            }
        }

        private static void PerformRepair(int companionEntityId)
        {
            if (!_taskTargets.ContainsKey(companionEntityId))
                return;

            int blockEntityId = _taskTargets[companionEntityId];

            Log.Out($"[CompanionBot] Companion {companionEntityId} repairing block {blockEntityId} (+{RepairHealthPerTick} HP)");

            var profile = ProfileManager.GetProfile(companionEntityId);
            if (profile.Skills.ContainsKey("Engineering"))
            {
                profile.Skills["Engineering"].AddExperience(5f);
            }
        }

        private static void PerformUpgrade(int companionEntityId)
        {
            if (!_taskTargets.ContainsKey(companionEntityId))
                return;

            int blockEntityId = _taskTargets[companionEntityId];

            Log.Out($"[CompanionBot] Companion {companionEntityId} upgrading block {blockEntityId} (+{UpgradeProgressPerTick}%)");

            var profile = ProfileManager.GetProfile(companionEntityId);
            if (profile.Skills.ContainsKey("Engineering"))
            {
                profile.Skills["Engineering"].AddExperience(10f);
            }
        }

        public static BuildingTask GetCurrentTask(int companionEntityId)
        {
            return _currentTasks.ContainsKey(companionEntityId) ? _currentTasks[companionEntityId] : BuildingTask.None;
        }

        public static Vector3 GetTaskPosition(int companionEntityId)
        {
            return _taskPositions.ContainsKey(companionEntityId) ? _taskPositions[companionEntityId] : Vector3.zero;
        }

        public static void ClearBuildingData(int companionEntityId)
        {
            if (_currentTasks.ContainsKey(companionEntityId))
                _currentTasks.Remove(companionEntityId);
            if (_taskPositions.ContainsKey(companionEntityId))
                _taskPositions.Remove(companionEntityId);
            if (_lastActionTime.ContainsKey(companionEntityId))
                _lastActionTime.Remove(companionEntityId);
            if (_taskTargets.ContainsKey(companionEntityId))
                _taskTargets.Remove(companionEntityId);
        }
    }
}
