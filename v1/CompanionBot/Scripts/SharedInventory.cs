using System;
using System.Collections.Generic;
using System.Linq;

namespace CompanionBot
{
    public class SharedInventory
    {
        public int OwnerEntityId { get; set; }
        public Dictionary<string, int> Items { get; set; }
        public int MaxCapacity { get; set; }
        public bool IsEnabled { get; set; }

        public SharedInventory(int ownerEntityId, int maxCapacity = 100)
        {
            OwnerEntityId = ownerEntityId;
            Items = new Dictionary<string, int>();
            MaxCapacity = maxCapacity;
            IsEnabled = false;
        }

        public int GetTotalItemCount()
        {
            return Items.Values.Sum();
        }

        public bool HasSpace(int count = 1)
        {
            return GetTotalItemCount() + count <= MaxCapacity;
        }

        public bool AddItem(string itemName, int count = 1)
        {
            if (!HasSpace(count))
            {
                Log.Out($"[CompanionBot] Shared inventory full, cannot add {count}x {itemName}");
                return false;
            }

            if (Items.ContainsKey(itemName))
            {
                Items[itemName] += count;
            }
            else
            {
                Items[itemName] = count;
            }

            Log.Out($"[CompanionBot] Added {count}x {itemName} to shared inventory");
            return true;
        }

        public bool RemoveItem(string itemName, int count = 1)
        {
            if (!Items.ContainsKey(itemName) || Items[itemName] < count)
            {
                return false;
            }

            Items[itemName] -= count;
            if (Items[itemName] <= 0)
            {
                Items.Remove(itemName);
            }

            Log.Out($"[CompanionBot] Removed {count}x {itemName} from shared inventory");
            return true;
        }

        public int GetItemCount(string itemName)
        {
            return Items.ContainsKey(itemName) ? Items[itemName] : 0;
        }

        public bool HasItem(string itemName, int count = 1)
        {
            return GetItemCount(itemName) >= count;
        }

        public List<string> GetAllItems()
        {
            var result = new List<string>();
            foreach (var kvp in Items)
            {
                result.Add($"{kvp.Key} x{kvp.Value}");
            }
            return result;
        }

        public void Clear()
        {
            Items.Clear();
            Log.Out($"[CompanionBot] Shared inventory cleared");
        }
    }

    public static class SharedInventoryManager
    {
        private static Dictionary<int, SharedInventory> _sharedInventories = new Dictionary<int, SharedInventory>();

        public static SharedInventory GetSharedInventory(int ownerEntityId)
        {
            if (!_sharedInventories.ContainsKey(ownerEntityId))
            {
                _sharedInventories[ownerEntityId] = new SharedInventory(ownerEntityId);
            }
            return _sharedInventories[ownerEntityId];
        }

        public static void EnableSharedInventory(int ownerEntityId, bool enabled)
        {
            var inventory = GetSharedInventory(ownerEntityId);
            inventory.IsEnabled = enabled;
            Log.Out($"[CompanionBot] Shared inventory {(enabled ? "enabled" : "disabled")} for player {ownerEntityId}");
        }

        public static bool IsEnabled(int ownerEntityId)
        {
            return GetSharedInventory(ownerEntityId).IsEnabled;
        }

        public static bool AddItemToShared(int ownerEntityId, string itemName, int count = 1)
        {
            var inventory = GetSharedInventory(ownerEntityId);
            if (!inventory.IsEnabled)
                return false;

            return inventory.AddItem(itemName, count);
        }

        public static bool RemoveItemFromShared(int ownerEntityId, string itemName, int count = 1)
        {
            var inventory = GetSharedInventory(ownerEntityId);
            if (!inventory.IsEnabled)
                return false;

            return inventory.RemoveItem(itemName, count);
        }

        public static bool TryGetItemFromShared(int ownerEntityId, string itemName, int count = 1)
        {
            var inventory = GetSharedInventory(ownerEntityId);
            if (!inventory.IsEnabled)
                return false;

            return inventory.HasItem(itemName, count);
        }

        public static void DistributeAmmo(int ownerEntityId)
        {
            var inventory = GetSharedInventory(ownerEntityId);
            if (!inventory.IsEnabled)
                return;

            var squad = SquadManager.GetSquad(ownerEntityId);
            if (squad == null || squad.MemberEntityIds.Count == 0)
                return;

            string[] ammoTypes = { "ammo9mmBulletBall", "ammo762mmBulletBall", "ammoShotgunShell", "ammoArrowIron" };

            foreach (var ammoType in ammoTypes)
            {
                int totalAmmo = inventory.GetItemCount(ammoType);
                if (totalAmmo <= 0)
                    continue;

                int ammoPerCompanion = (int)(totalAmmo / squad.MemberEntityIds.Count);
                if (ammoPerCompanion <= 0)
                    continue;

                foreach (int companionEntityId in squad.MemberEntityIds)
                {
                    var companionInventory = InventorySystem.GetInventory(companionEntityId);
                    if (companionInventory.HasSpace())
                    {
                        companionInventory.AddItem(ammoType, ammoPerCompanion);
                        inventory.RemoveItem(ammoType, ammoPerCompanion);
                        Log.Out($"[CompanionBot] Distributed {ammoPerCompanion}x {ammoType} to companion {companionEntityId}");
                    }
                }
            }
        }

        public static void DistributeHealingItems(int ownerEntityId)
        {
            var inventory = GetSharedInventory(ownerEntityId);
            if (!inventory.IsEnabled)
                return;

            var squad = SquadManager.GetSquad(ownerEntityId);
            if (squad == null || squad.MemberEntityIds.Count == 0)
                return;

            string[] healingItems = { "medicalFirstAidKit", "medicalBandage", "medicalPlasterCast" };

            foreach (var item in healingItems)
            {
                int totalCount = inventory.GetItemCount(item);
                if (totalCount <= 0)
                    continue;

                int itemsPerCompanion = (int)(totalCount / squad.MemberEntityIds.Count);
                if (itemsPerCompanion <= 0)
                    continue;

                foreach (int companionEntityId in squad.MemberEntityIds)
                {
                    var companionInventory = InventorySystem.GetInventory(companionEntityId);
                    if (companionInventory.HasSpace())
                    {
                        companionInventory.AddItem(item, itemsPerCompanion);
                        inventory.RemoveItem(item, itemsPerCompanion);
                        Log.Out($"[CompanionBot] Distributed {itemsPerCompanion}x {item} to companion {companionEntityId}");
                    }
                }
            }
        }

        public static void RemoveSharedInventory(int ownerEntityId)
        {
            if (_sharedInventories.ContainsKey(ownerEntityId))
            {
                _sharedInventories.Remove(ownerEntityId);
            }
        }
    }
}
