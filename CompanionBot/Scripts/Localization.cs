using System;
using System.Collections.Generic;

namespace CompanionBot
{
    public static class Localization
    {
        private static string _currentLanguage = "en";
        private static Dictionary<string, Dictionary<string, string>> _translations;

        static Localization()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "en", new Dictionary<string, string>
                    {
                        { "no_companion", "No active companion found" },
                        { "companion_spawned", "Companion spawned successfully" },
                        { "companion_dismissed", "Companion dismissed" },
                        { "companion_died", "Companion died" },
                        { "companion_following", "Companion will follow you" },
                        { "companion_staying", "Companion will stay at current position" },
                        { "companion_guarding", "Companion will guard area" },
                        { "companion_healed", "Companion healed" },
                        { "inventory_full", "Inventory is full" },
                        { "item_added", "Item added to inventory" },
                        { "item_removed", "Item removed from inventory" },
                        { "item_equipped", "Item equipped" },
                        { "item_unequipped", "Item unequipped" },
                        { "autopickup_enabled", "Auto-pickup enabled" },
                        { "autopickup_disabled", "Auto-pickup disabled" },
                        { "patrol_waypoint_added", "Patrol waypoint added" },
                        { "patrol_cleared", "Patrol waypoints cleared" },
                        { "patrol_started", "Patrol mode started" },
                        { "escort_enabled", "Escort mode enabled" },
                        { "scout_enabled", "Scout mode enabled" },
                        { "horde_defense_set", "Horde defense position set" },
                        { "name_changed", "Companion name changed" },
                        { "class_changed", "Companion class changed" },
                        { "personality_changed", "Companion personality changed" },
                        { "role_assigned", "Role assigned" },
                        { "squad_added", "Companion added to squad" },
                        { "squad_removed", "Companion removed from squad" },
                        { "formation_set", "Formation set" },
                        { "squad_follow", "All squad members following" },
                        { "squad_guard", "All squad members guarding" },
                        { "squad_attack", "All squad members attacking" },
                        { "shared_inventory_enabled", "Shared inventory enabled" },
                        { "shared_inventory_disabled", "Shared inventory disabled" },
                        { "ammo_distributed", "Ammo distributed to squad" },
                        { "healing_distributed", "Healing items distributed to squad" },
                        { "death_respawn", "Companion will respawn in {0} seconds" },
                        { "death_permadeath", "Companion is permanently dead" },
                        { "death_cooldown", "Companion on cooldown for {0} seconds" },
                        { "quest_started", "Quest started: {0}" },
                        { "quest_completed", "Quest completed: {0}" },
                        { "quest_failed", "Quest failed: {0}" },
                        { "crafting_started", "Crafting started: {0}" },
                        { "crafting_completed", "Crafting completed: {0}" },
                        { "building_repair", "Repairing building" },
                        { "building_upgrade", "Upgrading building" },
                        { "animal_tamed", "Animal companion tamed" },
                        { "drone_deployed", "Drone deployed" },
                        { "error_player_not_found", "Player not found" },
                        { "error_invalid_parameter", "Invalid parameter" },
                        { "error_unknown_command", "Unknown command" },
                        { "status_health", "Health" },
                        { "status_stamina", "Stamina" },
                        { "status_level", "Level" },
                        { "status_experience", "Experience" },
                        { "status_kills", "Kills" },
                        { "status_role", "Role" },
                        { "status_class", "Class" },
                        { "status_personality", "Personality" }
                    }
                },
                {
                    "ru", new Dictionary<string, string>
                    {
                        { "no_companion", "Компаньон не найден" },
                        { "companion_spawned", "Компаньон успешно создан" },
                        { "companion_dismissed", "Компаньон распущен" },
                        { "companion_died", "Компаньон погиб" },
                        { "companion_following", "Компаньон следует за вами" },
                        { "companion_staying", "Компаньон останется на текущей позиции" },
                        { "companion_guarding", "Компаньон будет охранять зону" },
                        { "companion_healed", "Компаньон вылечен" },
                        { "inventory_full", "Инвентарь заполнен" },
                        { "item_added", "Предмет добавлен в инвентарь" },
                        { "item_removed", "Предмет удален из инвентаря" },
                        { "item_equipped", "Предмет экипирован" },
                        { "item_unequipped", "Предмет снят" },
                        { "autopickup_enabled", "Автоподбор включен" },
                        { "autopickup_disabled", "Автоподбор выключен" },
                        { "patrol_waypoint_added", "Точка патруля добавлена" },
                        { "patrol_cleared", "Точки патруля очищены" },
                        { "patrol_started", "Режим патрулирования начат" },
                        { "escort_enabled", "Режим сопровождения включен" },
                        { "scout_enabled", "Режим разведки включен" },
                        { "horde_defense_set", "Позиция обороны от орды установлена" },
                        { "name_changed", "Имя компаньона изменено" },
                        { "class_changed", "Класс компаньона изменен" },
                        { "personality_changed", "Характер компаньона изменен" },
                        { "role_assigned", "Роль назначена" },
                        { "squad_added", "Компаньон добавлен в отряд" },
                        { "squad_removed", "Компаньон удален из отряда" },
                        { "formation_set", "Формация установлена" },
                        { "squad_follow", "Все члены отряда следуют" },
                        { "squad_guard", "Все члены отряда охраняют" },
                        { "squad_attack", "Все члены отряда атакуют" },
                        { "shared_inventory_enabled", "Общий инвентарь включен" },
                        { "shared_inventory_disabled", "Общий инвентарь выключен" },
                        { "ammo_distributed", "Боеприпасы распределены между отрядом" },
                        { "healing_distributed", "Лечебные предметы распределены между отрядом" },
                        { "death_respawn", "Компаньон возродится через {0} секунд" },
                        { "death_permadeath", "Компаньон погиб навсегда" },
                        { "death_cooldown", "Компаньон на перезарядке {0} секунд" },
                        { "quest_started", "Квест начат: {0}" },
                        { "quest_completed", "Квест завершен: {0}" },
                        { "quest_failed", "Квест провален: {0}" },
                        { "crafting_started", "Крафт начат: {0}" },
                        { "crafting_completed", "Крафт завершен: {0}" },
                        { "building_repair", "Ремонт здания" },
                        { "building_upgrade", "Улучшение здания" },
                        { "animal_tamed", "Животное-компаньон приручено" },
                        { "drone_deployed", "Дрон развернут" },
                        { "error_player_not_found", "Игрок не найден" },
                        { "error_invalid_parameter", "Неверный параметр" },
                        { "error_unknown_command", "Неизвестная команда" },
                        { "status_health", "Здоровье" },
                        { "status_stamina", "Выносливость" },
                        { "status_level", "Уровень" },
                        { "status_experience", "Опыт" },
                        { "status_kills", "Убийства" },
                        { "status_role", "Роль" },
                        { "status_class", "Класс" },
                        { "status_personality", "Характер" }
                    }
                }
            };
        }

        public static void SetLanguage(string language)
        {
            if (_translations.ContainsKey(language.ToLower()))
            {
                _currentLanguage = language.ToLower();
                Log.Out($"[CompanionBot] Language set to: {_currentLanguage}");
            }
            else
            {
                Log.Error($"[CompanionBot] Unsupported language: {language}. Using 'en'");
                _currentLanguage = "en";
            }
        }

        public static string GetLanguage()
        {
            return _currentLanguage;
        }

        public static string Get(string key, params object[] args)
        {
            if (!_translations.ContainsKey(_currentLanguage))
            {
                _currentLanguage = "en";
            }

            var translations = _translations[_currentLanguage];
            if (!translations.ContainsKey(key))
            {
                var fallbackTranslations = _translations["en"];
                if (fallbackTranslations.ContainsKey(key))
                {
                    return args.Length > 0 ? string.Format(fallbackTranslations[key], args) : fallbackTranslations[key];
                }
                return key;
            }

            var text = translations[key];
            return args.Length > 0 ? string.Format(text, args) : text;
        }

        public static List<string> GetAvailableLanguages()
        {
            return new List<string>(_translations.Keys);
        }
    }
}
