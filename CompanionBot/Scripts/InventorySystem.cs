using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CompanionBot
{
    public enum EquipmentSlot
    {
        Weapon,
        Head,
        Chest,
        Legs,
        Feet
    }

    public class InventoryItem
    {
        public string ItemName { get; set; }
        public int Count { get; set; }
        public float Durability { get; set; }
        public float MaxDurability { get; set; }
        public DateTime AcquiredTime { get; set; }

        public InventoryItem(string itemName, int count = 1, float durability = 100f)
        {
            ItemName = itemName;
            Count = count;
            Durability = durability;
            MaxDurability = 100f;
            AcquiredTime = DateTime.Now;
        }

        public bool IsBroken()
        {
            return Durability <= 0;
        }

        public void UseDurability(float amount)
        {
            Durability = Math.Max(0, Durability - amount);
        }

        public void Repair(float amount)
        {
            Durability = Math.Min(MaxDurability, Durability + amount);
        }
    }

    public class CompanionInventory
    {
        public Dictionary<EquipmentSlot, InventoryItem> Equipment { get; set; }
        public Dictionary<string, InventoryItem> Items { get; set; }
        public int MaxCapacity { get; set; }
        public bool AutoPickupEnabled { get; set; }

        public CompanionInventory(int maxCapacity = 20)
        {
            Equipment = new Dictionary<EquipmentSlot, InventoryItem>();
            Items = new Dictionary<string, InventoryItem>();
            MaxCapacity = maxCapacity;
            AutoPickupEnabled = false;
        }

        public int GetTotalItemCount()
        {
            return Items.Values.Sum(i => i.Count);
        }

        public bool HasSpace()
        {
            return GetTotalItemCount() < MaxCapacity;
        }

        public bool AddItem(string itemName, int count = 1, float durability = 100f)
        {
            if (Items.ContainsKey(itemName))
            {
                Items[itemName].Count += count;
                return true;
            }

            if (!HasSpace())
            {
                Log.Out($"[CompanionBot] Inventory full, cannot add {itemName}");
                return false;
            }

            Items[itemName] = new InventoryItem(itemName, count, durability);
            return true;
        }

        public bool RemoveItem(string itemName, int count = 1)
        {
            if (!Items.ContainsKey(itemName))
                return false;

            var item = Items[itemName];
            if (item.Count < count)
                return false;

            item.Count -= count;
            if (item.Count <= 0)
            {
                Items.Remove(itemName);
            }

            return true;
        }

        public int GetItemCount(string itemName)
        {
            return Items.ContainsKey(itemName) ? Items[itemName].Count : 0;
        }

        public bool HasItem(string itemName, int count = 1)
        {
            return GetItemCount(itemName) >= count;
        }

        public InventoryItem GetItem(string itemName)
        {
            return Items.ContainsKey(itemName) ? Items[itemName] : null;
        }

        public bool EquipItem(EquipmentSlot slot, string itemName)
        {
            if (!Items.ContainsKey(itemName))
                return false;

            var item = Items[itemName];
            if (item.Count <= 0)
                return false;

            if (Equipment.ContainsKey(slot))
            {
                var oldItem = Equipment[slot];
                AddItem(oldItem.ItemName, 1, oldItem.Durability);
            }

            Equipment[slot] = new InventoryItem(itemName, 1, item.Durability);
            RemoveItem(itemName, 1);

            Log.Out($"[CompanionBot] Equipped {itemName} to {slot} slot");
            return true;
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            if (!Equipment.ContainsKey(slot))
                return false;

            if (!HasSpace())
            {
                Log.Out($"[CompanionBot] Inventory full, cannot unequip {slot}");
                return false;
            }

            var item = Equipment[slot];
            AddItem(item.ItemName, 1, item.Durability);
            Equipment.Remove(slot);

            Log.Out($"[CompanionBot] Unequipped {slot} slot");
            return true;
        }

        public InventoryItem GetEquippedItem(EquipmentSlot slot)
        {
            return Equipment.ContainsKey(slot) ? Equipment[slot] : null;
        }

        public bool IsSlotEquipped(EquipmentSlot slot)
        {
            return Equipment.ContainsKey(slot);
        }

        public void UseAmmo(string ammoType, int count = 1)
        {
            if (HasItem(ammoType, count))
            {
                RemoveItem(ammoType, count);
            }
        }

        public bool HasAmmo(string ammoType, int count = 1)
        {
            return HasItem(ammoType, count);
        }

        public string FindHealingItem()
        {
            string[] healingItems = { "medicalFirstAidKit", "medicalBandage", "medicalFirstAidBandage" };

            foreach (var item in healingItems)
            {
                if (HasItem(item))
                    return item;
            }

            return null;
        }

        public void UseHealingItem(string itemName)
        {
            RemoveItem(itemName, 1);
        }

        public void ApplyDurabilityDamage(EquipmentSlot slot, float damage)
        {
            if (!Equipment.ContainsKey(slot))
                return;

            var item = Equipment[slot];
            item.UseDurability(damage);

            if (item.IsBroken())
            {
                Log.Out($"[CompanionBot] {item.ItemName} in {slot} slot broke!");
                Equipment.Remove(slot);
            }
        }

        public void RepairItem(EquipmentSlot slot, float amount)
        {
            if (!Equipment.ContainsKey(slot))
                return;

            Equipment[slot].Repair(amount);
        }

        public List<string> GetAllItems()
        {
            var result = new List<string>();

            foreach (var kvp in Items)
            {
                result.Add($"{kvp.Key} x{kvp.Value.Count}");
            }

            return result;
        }

        public void Clear()
        {
            Items.Clear();
            Equipment.Clear();
        }

        public List<string> GetAllEquipment()
        {
            var result = new List<string>();

            foreach (var kvp in Equipment)
            {
                var item = kvp.Value;
                var durabilityStr = item.MaxDurability > 0 ? $" ({item.Durability:F0}%)" : "";
                result.Add($"{kvp.Key}: {item.ItemName}{durabilityStr}");
            }

            return result;
        }
    }

    public static class InventorySystem
    {
        private static Dictionary<int, CompanionInventory> _inventories = new Dictionary<int, CompanionInventory>();

        public static CompanionInventory GetInventory(int entityId)
        {
            if (!_inventories.ContainsKey(entityId))
            {
                _inventories[entityId] = new CompanionInventory();
            }
            return _inventories[entityId];
        }

        public static void RemoveInventory(int entityId)
        {
            if (_inventories.ContainsKey(entityId))
            {
                _inventories.Remove(entityId);
            }
        }

        public static bool AddItemToCompanion(int entityId, string itemName, int count = 1)
        {
            var inventory = GetInventory(entityId);
            return inventory.AddItem(itemName, count);
        }

        public static bool RemoveItemFromCompanion(int entityId, string itemName, int count = 1)
        {
            var inventory = GetInventory(entityId);
            return inventory.RemoveItem(itemName, count);
        }

        public static bool EquipItem(int entityId, EquipmentSlot slot, string itemName)
        {
            var inventory = GetInventory(entityId);
            return inventory.EquipItem(slot, itemName);
        }

        public static bool UnequipItem(int entityId, EquipmentSlot slot)
        {
            var inventory = GetInventory(entityId);
            return inventory.UnequipItem(slot);
        }

        public static void UseAmmo(int entityId, string ammoType, int count = 1)
        {
            var inventory = GetInventory(entityId);
            inventory.UseAmmo(ammoType, count);
        }

        public static bool HasAmmo(int entityId, string ammoType)
        {
            var inventory = GetInventory(entityId);
            return inventory.HasAmmo(ammoType);
        }

        public static string FindHealingItem(int entityId)
        {
            var inventory = GetInventory(entityId);
            return inventory.FindHealingItem();
        }

        public static void UseHealingItem(int entityId, string itemName)
        {
            var inventory = GetInventory(entityId);
            inventory.UseHealingItem(itemName);
        }

        public static void ApplyDurabilityDamage(int entityId, EquipmentSlot slot, float damage)
        {
            var inventory = GetInventory(entityId);
            inventory.ApplyDurabilityDamage(slot, damage);
        }

        public static void SetAutoPickup(int entityId, bool enabled)
        {
            var inventory = GetInventory(entityId);
            inventory.AutoPickupEnabled = enabled;
        }

        public static bool IsAutoPickupEnabled(int entityId)
        {
            var inventory = GetInventory(entityId);
            return inventory.AutoPickupEnabled;
        }

        public static Dictionary<int, CompanionInventory> GetAllInventories()
        {
            return new Dictionary<int, CompanionInventory>(_inventories);
        }

        public static void LoadInventory(int entityId, CompanionInventory inventory)
        {
            _inventories[entityId] = inventory;
        }
    }
}
