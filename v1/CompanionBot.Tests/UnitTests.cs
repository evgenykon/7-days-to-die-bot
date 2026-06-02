using Xunit;
using CompanionBot;
using System.Collections.Generic;

namespace CompanionBot.Tests
{
    public class InventorySystemTests
    {
        [Fact]
        public void CompanionInventory_AddItem_ShouldSucceed()
        {
            // Arrange
            var inventory = new CompanionInventory(100);
            
            // Act
            bool result = inventory.AddItem("testItem", 5);
            
            // Assert
            Assert.True(result);
            Assert.Equal(5, inventory.GetItemCount("testItem"));
        }

        [Fact]
        public void CompanionInventory_AddItem_WhenFull_ShouldFail()
        {
            // Arrange
            var inventory = new CompanionInventory(10);
            inventory.AddItem("item1", 10);
            
            // Act
            bool result = inventory.AddItem("item2", 1);
            
            // Assert
            Assert.False(result);
            Assert.Equal(0, inventory.GetItemCount("item2"));
        }

        [Fact]
        public void CompanionInventory_RemoveItem_ShouldSucceed()
        {
            // Arrange
            var inventory = new CompanionInventory(100);
            inventory.AddItem("testItem", 10);
            
            // Act
            bool result = inventory.RemoveItem("testItem", 3);
            
            // Assert
            Assert.True(result);
            Assert.Equal(7, inventory.GetItemCount("testItem"));
        }

        [Fact]
        public void CompanionInventory_RemoveItem_WhenNotEnough_ShouldFail()
        {
            // Arrange
            var inventory = new CompanionInventory(100);
            inventory.AddItem("testItem", 5);
            
            // Act
            bool result = inventory.RemoveItem("testItem", 10);
            
            // Assert
            Assert.False(result);
            Assert.Equal(5, inventory.GetItemCount("testItem"));
        }

        [Fact]
        public void CompanionInventory_HasItem_ShouldReturnCorrectResult()
        {
            // Arrange
            var inventory = new CompanionInventory(100);
            inventory.AddItem("testItem", 5);
            
            // Act & Assert
            Assert.True(inventory.HasItem("testItem", 3));
            Assert.True(inventory.HasItem("testItem", 5));
            Assert.False(inventory.HasItem("testItem", 10));
            Assert.False(inventory.HasItem("nonExistentItem", 1));
        }

        [Fact]
        public void CompanionInventory_Clear_ShouldRemoveAllItems()
        {
            // Arrange
            var inventory = new CompanionInventory(100);
            inventory.AddItem("item1", 5);
            inventory.AddItem("item2", 10);
            
            // Act
            inventory.Clear();
            
            // Assert
            Assert.Equal(0, inventory.GetItemCount("item1"));
            Assert.Equal(0, inventory.GetItemCount("item2"));
            Assert.Equal(0, inventory.GetTotalItemCount());
        }

        [Fact]
        public void CompanionInventory_EquipItem_ShouldSucceed()
        {
            // Arrange
            var inventory = new CompanionInventory(100);
            inventory.AddItem("weapon", 1);
            
            // Act
            bool result = inventory.EquipItem(EquipmentSlot.Weapon, "weapon");
            
            // Assert
            Assert.True(result);
            Assert.True(inventory.IsSlotEquipped(EquipmentSlot.Weapon));
            Assert.Equal(0, inventory.GetItemCount("weapon"));
        }

        [Fact]
        public void CompanionInventory_UnequipItem_ShouldSucceed()
        {
            // Arrange
            var inventory = new CompanionInventory(100);
            inventory.AddItem("weapon", 1);
            inventory.EquipItem(EquipmentSlot.Weapon, "weapon");
            
            // Act
            bool result = inventory.UnequipItem(EquipmentSlot.Weapon);
            
            // Assert
            Assert.True(result);
            Assert.False(inventory.IsSlotEquipped(EquipmentSlot.Weapon));
            Assert.Equal(1, inventory.GetItemCount("weapon"));
        }
    }

    public class CombatStatsTests
    {
        [Fact]
        public void CombatStats_RecordKill_ShouldIncrementCounters()
        {
            // Arrange
            var stats = new CombatStats();
            
            // Act
            stats.TotalKills++;
            stats.ZombieKills++;
            
            // Assert
            Assert.Equal(1, stats.TotalKills);
            Assert.Equal(1, stats.ZombieKills);
        }

        [Fact]
        public void CombatStats_RecordDamage_ShouldAccumulate()
        {
            // Arrange
            var stats = new CombatStats();
            
            // Act
            stats.TotalDamageDealt += 50.5f;
            stats.TotalDamageDealt += 30.2f;
            stats.TotalDamageTaken += 20.0f;
            
            // Assert
            Assert.Equal(80.7f, stats.TotalDamageDealt, 1);
            Assert.Equal(20.0f, stats.TotalDamageTaken, 1);
        }

        [Fact]
        public void CombatStats_RecordRetreat_ShouldIncrementCounter()
        {
            // Arrange
            var stats = new CombatStats();
            
            // Act
            stats.RetreatCount++;
            stats.RetreatCount++;
            
            // Assert
            Assert.Equal(2, stats.RetreatCount);
        }
    }

    public class CompanionProfileTests
    {
        [Fact]
        public void CompanionProfile_AddExperience_ShouldLevelUp()
        {
            // Arrange
            var profile = new CompanionProfile();
            float initialLevel = profile.Level;
            
            // Act
            profile.AddExperience(150); // Should level up
            
            // Assert
            Assert.True(profile.Level > initialLevel);
        }

        [Fact]
        public void CompanionProfile_AddKill_ShouldIncrementKillsAndXP()
        {
            // Arrange
            var profile = new CompanionProfile();
            float initialXP = profile.Experience;
            
            // Act
            profile.AddKill();
            
            // Assert
            Assert.Equal(1, profile.TotalKills);
            Assert.True(profile.Experience > initialXP);
        }

        [Fact]
        public void CompanionProfile_GetCombatModifier_ShouldReturnCorrectValue()
        {
            // Arrange
            var profile = new CompanionProfile();
            profile.Class = CompanionClass.Soldier;
            
            // Act
            float modifier = profile.GetCombatModifier();
            
            // Assert
            Assert.True(modifier > 1.0f); // Soldier should have bonus
        }

        [Fact]
        public void CompanionProfile_GetDefenseModifier_ShouldReturnCorrectValue()
        {
            // Arrange
            var profile = new CompanionProfile();
            profile.Class = CompanionClass.Guardian;
            
            // Act
            float modifier = profile.GetDefenseModifier();
            
            // Assert
            Assert.True(modifier > 1.0f); // Guardian should have defense bonus
        }
    }

    public class GlobalConfigTests
    {
        [Fact]
        public void GlobalConfig_DefaultValues_ShouldBeSet()
        {
            // Arrange & Act
            var config = new GlobalConfig();
            
            // Assert
            Assert.Equal(3f, config.FollowDistance);
            Assert.Equal(15f, config.MaxFollowDistance);
            Assert.Equal(25f, config.AttackRange);
            Assert.Equal(20, config.InventoryMaxCapacity);
        }

        [Fact]
        public void GlobalConfig_ModifyValues_ShouldPersist()
        {
            // Arrange
            var config = new GlobalConfig();
            
            // Act
            config.FollowDistance = 5f;
            config.MaxCompanionsPerPlayer = 10;
            
            // Assert
            Assert.Equal(5f, config.FollowDistance);
            Assert.Equal(10, config.MaxCompanionsPerPlayer);
        }
    }
}
