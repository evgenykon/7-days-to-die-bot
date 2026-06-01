using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CompanionBot
{
    [HarmonyPatch(typeof(GameManager), "Update")]
    public class GameEventTriggerPatch
    {
        private static float _lastCheckTime = 0f;
        private static int _lastDayNumber = -1;
        private static bool _wasHordeNight = false;
        private static Dictionary<int, float> _lastLowHpWarning = new Dictionary<int, float>();

        static void Postfix()
        {
            if (ModMain.Chat == null || ModMain.MemoryLog == null)
                return;

            if (Time.time - _lastCheckTime < 5f)
                return;

            _lastCheckTime = Time.time;

            CheckDayNightCycle();
            CheckHordeNight();
            CheckPlayerHealth();
            CheckCompanionHealth();
        }

        private static void CheckDayNightCycle()
        {
            if (GameManager.Instance == null)
                return;

            int currentDay = GameUtils.WorldTimeToDays(GameManager.Instance.World.worldTime);
            bool isDay = GameApi.IsDay();

            if (currentDay != _lastDayNumber)
            {
                _lastDayNumber = currentDay;

                if (isDay)
                {
                    ModMain.MemoryLog.LogDayNightCycle(true, currentDay);
                    _ = ModMain.Chat.SendMessage("day_start", $"Начался день {currentDay}");
                }
                else
                {
                    ModMain.MemoryLog.LogDayNightCycle(false, currentDay);
                    _ = ModMain.Chat.SendMessage("night_start", $"Началась ночь {currentDay}");
                }
            }
        }

        private static void CheckHordeNight()
        {
            if (GameManager.Instance == null)
                return;

            int dayNumber = GameUtils.WorldTimeToDays(GameManager.Instance.World.worldTime);
            bool isBloodMoon = dayNumber % 7 == 0;
            bool isNight = !GameApi.IsDay();
            bool isHordeNight = isBloodMoon && isNight;

            if (isHordeNight && !_wasHordeNight)
            {
                _wasHordeNight = true;
                ModMain.MemoryLog.LogHordeNightStart();
                _ = ModMain.Chat.SendMessage("horde_night_start", "Началась ночь орды!");
                _ = ModMain.Chat.SendMessage("blood_moon", "Кровавая луна! Будь осторожен!");
            }
            else if (!isHordeNight && _wasHordeNight)
            {
                _wasHordeNight = false;
                ModMain.MemoryLog.LogHordeNightEnd(true);
                _ = ModMain.Chat.SendMessage("horde_night_end", "Ночь орды закончилась! Мы выжили!");
            }
        }

        private static void CheckPlayerHealth()
        {
            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null || player.IsDead())
                return;

            float healthPercent = player.Health / (float)player.GetMaxHealth();

            if (healthPercent < 0.3f)
            {
                if (!_lastLowHpWarning.ContainsKey(player.entityId))
                    _lastLowHpWarning[player.entityId] = 0f;

                if (Time.time - _lastLowHpWarning[player.entityId] > 30f)
                {
                    _lastLowHpWarning[player.entityId] = Time.time;
                    _ = ModMain.Chat.SendMessage("low_hp_player", $"У тебя мало здоровья: {player.Health}/{player.GetMaxHealth()}");
                }
            }
        }

        private static void CheckCompanionHealth()
        {
            var companions = CompanionManager.GetAllCompanions();
            foreach (var companion in companions)
            {
                if (companion.Entity == null || companion.Entity.IsDead())
                    continue;

                float healthPercent = companion.Entity.Health / (float)companion.Entity.GetMaxHealth();

                if (healthPercent < 0.3f)
                {
                    if (!_lastLowHpWarning.ContainsKey(companion.Entity.entityId))
                        _lastLowHpWarning[companion.Entity.entityId] = 0f;

                    if (Time.time - _lastLowHpWarning[companion.Entity.entityId] > 30f)
                    {
                        _lastLowHpWarning[companion.Entity.entityId] = Time.time;
                        _ = ModMain.Chat.SendMessage("low_hp_companion", $"У меня мало здоровья: {companion.Entity.Health}/{companion.Entity.GetMaxHealth()}");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CraftingManager), "CraftItem")]
    public class CraftingTriggerPatch
    {
        static void Postfix(ItemValue _itemValue, int _count)
        {
            if (_itemValue == null || ModMain.MemoryLog == null)
                return;

            string itemName = _itemValue.ItemClass.GetItemName();
            ModMain.MemoryLog.LogCrafting(itemName, _count);
            _ = ModMain.Chat?.SendMessage("crafting", $"Скрафтил {itemName}!");
        }
    }

    [HarmonyPatch(typeof(Block), "OnBlockAdded")]
    public class BuildingTriggerPatch
    {
        static void Postfix(Block __instance, Vector3i _blockPos)
        {
            if (__instance == null || ModMain.MemoryLog == null)
                return;

            string blockName = __instance.GetBlockName();

            if (IsBuildingBlock(blockName))
            {
                ModMain.MemoryLog.LogBuilding(blockName, 1);

                if (UnityEngine.Random.value < 0.1f)
                {
                    _ = ModMain.Chat?.SendMessage("building", $"Построил {blockName}!");
                }
            }
        }

        private static bool IsBuildingBlock(string blockName)
        {
            return blockName.Contains("woodFrame") ||
                   blockName.Contains("brickBlock") ||
                   blockName.Contains("concreteBlock") ||
                   blockName.Contains("steelBlock") ||
                   blockName.Contains("flagstoneBlock") ||
                   blockName.Contains("cobblestoneBlock");
        }
    }

    [HarmonyPatch(typeof(EntityPlayer), "ExplorePOI")]
    public class ExplorationTriggerPatch
    {
        static void Postfix(EntityPlayer __instance, string _poiName)
        {
            if (__instance == null || string.IsNullOrEmpty(_poiName) || ModMain.MemoryLog == null)
                return;

            ModMain.MemoryLog.LogExploration(_poiName);
            _ = ModMain.Chat?.SendMessage("exploration", $"Исследуем {_poiName}!");
        }
    }

    [HarmonyPatch(typeof(EntityPlayer), "OnPlayerChat")]
    public class PlayerChatTriggerPatch
    {
        static bool Prefix(EntityPlayer __instance, string _message)
        {
            if (__instance == null || string.IsNullOrEmpty(_message))
                return true;

            if (_message.StartsWith("@companion") || _message.StartsWith("/companion"))
            {
                string userMessage = _message.Substring(_message.IndexOf(' ') + 1);

                if (ModMain.Chat != null)
                {
                    _ = ModMain.Chat.SendMessage("player_dialogue", userMessage);
                }

                return false;
            }

            return true;
        }
    }

    public class IdleChatterSystem
    {
        private static float _lastIdleMessageTime = 0f;
        private static float _idleMessageInterval = 300f;

        private static string[] _idleMessages = {
            "Хороший день для выживания!",
            "Интересно, что мы найдём сегодня?",
            "Мне нравится эта местность.",
            "Мы отличная команда!",
            "Надеюсь, сегодня будет спокойный день.",
            "Какой красивый закат!",
            "Нужно быть начеку.",
            "У нас хорошая база.",
            "Интересно, есть ли здесь другие выжившие?",
            "Сегодня мы сделали много работы!"
        };

        public static void Update()
        {
            if (ModMain.Chat == null)
                return;

            if (Time.time - _lastIdleMessageTime < _idleMessageInterval)
                return;

            var companions = CompanionManager.GetAllCompanions();
            if (companions.Count == 0)
                return;

            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null || player.IsDead())
                return;

            var companion = companions[0];
            if (companion.Entity == null || companion.Entity.IsDead())
                return;

            float distance = Vector3.Distance(companion.Entity.position, player.position);
            if (distance > 10f)
                return;

            if (UnityEngine.Random.value < 0.3f)
            {
                _lastIdleMessageTime = Time.time;
                string message = _idleMessages[UnityEngine.Random.Range(0, _idleMessages.Length)];
                _ = ModMain.Chat.SendMessage("idle_chatter", message);
            }
        }
    }

    [HarmonyPatch(typeof(EntityAlive), "Update")]
    public class IdleChatterPatch
    {
        static void Postfix(EntityAlive __instance)
        {
            if (__instance is EntityPlayer)
            {
                IdleChatterSystem.Update();
            }
        }
    }
}
