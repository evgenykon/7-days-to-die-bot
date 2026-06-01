using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public class CraftingRecipe
    {
        public string ResultItem { get; set; }
        public int ResultCount { get; set; }
        public Dictionary<string, int> RequiredItems { get; set; }
        public float CraftTime { get; set; }
        public int RequiredLevel { get; set; }

        public CraftingRecipe(string result, int count, float time, int level = 1)
        {
            ResultItem = result;
            ResultCount = count;
            RequiredItems = new Dictionary<string, int>();
            CraftTime = time;
            RequiredLevel = level;
        }
    }

    public static class CompanionCrafting
    {
        private static Dictionary<int, CraftingRecipe> _currentCrafting = new Dictionary<int, CraftingRecipe>();
        private static Dictionary<int, float> _craftingStartTime = new Dictionary<int, float>();
        private static Dictionary<string, CraftingRecipe> _recipes;

        static CompanionCrafting()
        {
            _recipes = new Dictionary<string, CraftingRecipe>
            {
                {
                    "medicalBandage",
                    new CraftingRecipe("medicalBandage", 2, 5f, 1)
                }
            };
            _recipes["medicalBandage"].RequiredItems["resourceCloth"] = 3;

            _recipes.Add("ammo9mmBulletBall", new CraftingRecipe("ammo9mmBulletBall", 10, 10f, 2));
            _recipes["ammo9mmBulletBall"].RequiredItems["resourceBulletCasing"] = 10;
            _recipes["ammo9mmBulletBall"].RequiredItems["resourceGunPowder"] = 5;
            _recipes["ammo9mmBulletBall"].RequiredItems["resourceBulletTip"] = 10;

            _recipes.Add("foodCookedMeat", new CraftingRecipe("foodCookedMeat", 1, 8f, 1));
            _recipes["foodCookedMeat"].RequiredItems["foodRawMeat"] = 1;

            _recipes.Add("resourceWood", new CraftingRecipe("resourceWood", 4, 3f, 1));
            _recipes["resourceWood"].RequiredItems["resourceTreeStump"] = 1;

            _recipes.Add("meleeWpnClubT0WoodenClub", new CraftingRecipe("meleeWpnClubT0WoodenClub", 1, 15f, 1));
            _recipes["meleeWpnClubT0WoodenClub"].RequiredItems["resourceWood"] = 10;
        }

        public static bool CanCraft(int companionEntityId, string recipeName)
        {
            if (!_recipes.ContainsKey(recipeName))
                return false;

            var recipe = _recipes[recipeName];
            var profile = ProfileManager.GetProfile(companionEntityId);

            if (profile.Level < recipe.RequiredLevel)
                return false;

            var inventory = InventorySystem.GetInventory(companionEntityId);
            foreach (var required in recipe.RequiredItems)
            {
                if (!inventory.HasItem(required.Key, required.Value))
                    return false;
            }

            return true;
        }

        public static bool StartCrafting(int companionEntityId, string recipeName)
        {
            if (!CanCraft(companionEntityId, recipeName))
            {
                Log.Out($"[CompanionBot] Cannot craft {recipeName} - missing requirements");
                return false;
            }

            if (_currentCrafting.ContainsKey(companionEntityId))
            {
                Log.Out($"[CompanionBot] Companion {companionEntityId} is already crafting");
                return false;
            }

            var recipe = _recipes[recipeName];
            var inventory = InventorySystem.GetInventory(companionEntityId);

            foreach (var required in recipe.RequiredItems)
            {
                inventory.RemoveItem(required.Key, required.Value);
            }

            _currentCrafting[companionEntityId] = recipe;
            _craftingStartTime[companionEntityId] = Time.time;

            Log.Out($"[CompanionBot] Companion {companionEntityId} started crafting {recipeName}");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("crafting_started", Localization.Get("crafting_started", recipeName));
            }

            return true;
        }

        public static void UpdateCrafting(int companionEntityId)
        {
            if (!_currentCrafting.ContainsKey(companionEntityId))
                return;

            var recipe = _currentCrafting[companionEntityId];
            var startTime = _craftingStartTime[companionEntityId];
            var elapsed = Time.time - startTime;

            if (elapsed >= recipe.CraftTime)
            {
                CompleteCrafting(companionEntityId);
            }
        }

        private static void CompleteCrafting(int companionEntityId)
        {
            if (!_currentCrafting.ContainsKey(companionEntityId))
                return;

            var recipe = _currentCrafting[companionEntityId];
            var inventory = InventorySystem.GetInventory(companionEntityId);

            inventory.AddItem(recipe.ResultItem, recipe.ResultCount);

            var profile = ProfileManager.GetProfile(companionEntityId);
            if (profile.Skills.ContainsKey("Crafting"))
            {
                profile.Skills["Crafting"].AddExperience(recipe.CraftTime);
            }

            QuestSystem.UpdateQuestProgress(companionEntityId, QuestType.CraftItem, 1);

            Log.Out($"[CompanionBot] Companion {companionEntityId} completed crafting {recipe.ResultItem} x{recipe.ResultCount}");

            if (ModMain.Chat != null)
            {
                _ = ModMain.Chat.SendMessage("crafting_completed", Localization.Get("crafting_completed", recipe.ResultItem));
            }

            _currentCrafting.Remove(companionEntityId);
            _craftingStartTime.Remove(companionEntityId);
        }

        public static bool IsCrafting(int companionEntityId)
        {
            return _currentCrafting.ContainsKey(companionEntityId);
        }

        public static float GetCraftingProgress(int companionEntityId)
        {
            if (!_currentCrafting.ContainsKey(companionEntityId))
                return 0f;

            var recipe = _currentCrafting[companionEntityId];
            var startTime = _craftingStartTime[companionEntityId];
            var elapsed = Time.time - startTime;

            return Math.Min(1f, elapsed / recipe.CraftTime);
        }

        public static void CancelCrafting(int companionEntityId)
        {
            if (!_currentCrafting.ContainsKey(companionEntityId))
                return;

            var recipe = _currentCrafting[companionEntityId];
            var inventory = InventorySystem.GetInventory(companionEntityId);

            foreach (var required in recipe.RequiredItems)
            {
                inventory.AddItem(required.Key, required.Value);
            }

            _currentCrafting.Remove(companionEntityId);
            _craftingStartTime.Remove(companionEntityId);

            Log.Out($"[CompanionBot] Companion {companionEntityId} cancelled crafting");
        }

        public static List<string> GetAvailableRecipes(int companionEntityId)
        {
            var available = new List<string>();
            var profile = ProfileManager.GetProfile(companionEntityId);

            foreach (var kvp in _recipes)
            {
                if (profile.Level >= kvp.Value.RequiredLevel)
                {
                    available.Add(kvp.Key);
                }
            }

            return available;
        }

        public static void ClearCraftingData(int companionEntityId)
        {
            if (_currentCrafting.ContainsKey(companionEntityId))
                _currentCrafting.Remove(companionEntityId);
            if (_craftingStartTime.ContainsKey(companionEntityId))
                _craftingStartTime.Remove(companionEntityId);
        }
    }
}
