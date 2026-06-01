using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CompanionBot
{
    [HarmonyPatch(typeof(EModelPlayer), "SetSkinTexture")]
    public class SetSkinTexturePatch
    {
        static bool Prefix(string _textureName)
        {
            if (string.IsNullOrEmpty(_textureName))
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(EntityPlayer), "OnUpdateEntity")]
    public class CompanionPlayerUpdatePatch
    {
        static bool Prefix(EntityPlayer __instance)
        {
            if (CompanionManager.GetCompanion(__instance.entityId) != null)
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(GameManager), "Update")]
    public class GameEventTriggerPatch
    {
        private static float _lastCheckTime = 0f;
        private static int _lastDayNumber = -1;
        private static bool _wasHordeNight = false;
        private static Dictionary<int, float> _lastLowHpWarning = new Dictionary<int, float>();
        private static float _lastIdleMessageTime = 0f;

        static void Postfix()
        {
            if (ModMain.Chat == null || ModMain.MemoryLog == null)
                return;

            if (GameManager.Instance == null || GameManager.Instance.World == null)
                return;

            if (Time.time - _lastCheckTime < 5f)
                return;

            _lastCheckTime = Time.time;

            CheckDayNightCycle();
            CheckHordeNight();
            CheckPlayerHealth();
            CheckCompanionHealth();
            CheckIdleChatter();
        }

        private static void CheckDayNightCycle()
        {
            if (GameManager.Instance?.World == null)
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
            if (GameManager.Instance?.World == null)
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

        private static void CheckIdleChatter()
        {
            if (Time.time - _lastIdleMessageTime < 300f)
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
                string[] messages = {
                    "Хороший день для выживания!",
                    "Интересно, что мы найдём сегодня?",
                    "Мне нравится эта местность.",
                    "Мы отличная команда!",
                    "Надеюсь, сегодня будет спокойный день."
                };
                _ = ModMain.Chat.SendMessage("idle_chatter", messages[UnityEngine.Random.Range(0, messages.Length)]);
            }
        }
    }

}
