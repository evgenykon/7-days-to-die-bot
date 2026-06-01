using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public static class CompanionInteraction
    {
        private const float InteractionRange = 5f;
        private const float HealCheckInterval = 10f;
        private const float AmmoShareInterval = 15f;
        private const float CoordinationInterval = 5f;

        private static Dictionary<int, float> _lastHealTime = new Dictionary<int, float>();
        private static Dictionary<int, float> _lastAmmoShareTime = new Dictionary<int, float>();
        private static Dictionary<int, float> _lastCoordinationTime = new Dictionary<int, float>();

        public static void UpdateInteractions(int ownerEntityId)
        {
            var squad = SquadManager.GetSquad(ownerEntityId);
            if (squad == null || squad.MemberEntityIds.Count < 2)
                return;

            foreach (int companionEntityId in squad.MemberEntityIds)
            {
                var companion = CompanionManager.GetCompanion(companionEntityId);
                if (companion == null || companion.Entity == null || companion.Entity.IsDead())
                    continue;

                TryHealNearbyCompanions(companionEntityId, squad);
                TryShareAmmo(companionEntityId, squad);
                TryCoordinateActions(companionEntityId, squad);
            }
        }

        private static void TryHealNearbyCompanions(int healerEntityId, SquadData squad)
        {
            if (!_lastHealTime.ContainsKey(healerEntityId))
                _lastHealTime[healerEntityId] = 0f;

            if (Time.time - _lastHealTime[healerEntityId] < HealCheckInterval)
                return;

            _lastHealTime[healerEntityId] = Time.time;

            var healer = CompanionManager.GetCompanion(healerEntityId);
            if (healer == null || healer.Entity == null)
                return;

            var healerRole = SquadRoleManager.GetRole(healerEntityId);
            if (healerRole != SquadRole.Medic && healerRole != SquadRole.Support)
                return;

            var healerInventory = InventorySystem.GetInventory(healerEntityId);
            string healingItem = healerInventory.FindHealingItem();
            if (string.IsNullOrEmpty(healingItem))
                return;

            foreach (int targetEntityId in squad.MemberEntityIds)
            {
                if (targetEntityId == healerEntityId)
                    continue;

                var target = CompanionManager.GetCompanion(targetEntityId);
                if (target == null || target.Entity == null || target.Entity.IsDead())
                    continue;

                float distance = Vector3.Distance(healer.Entity.position, target.Entity.position);
                if (distance > InteractionRange)
                    continue;

                float healthPercent = target.Entity.Health / (float)target.Entity.GetMaxHealth();
                if (healthPercent > 0.7f)
                    continue;

                int healAmount = healingItem.Contains("FirstAidKit") ? 100 : 50;
                var roleData = SquadRoleManager.GetRoleData(healerEntityId);
                healAmount = (int)(healAmount * roleData.HealModifier);

                target.Entity.Health = Math.Min(target.Entity.Health + healAmount, target.Entity.GetMaxHealth());
                healerInventory.UseHealingItem(healingItem);

                Log.Out($"[CompanionBot] Companion {healerEntityId} healed companion {targetEntityId} for {healAmount} HP");

                if (ModMain.Chat != null)
                {
                    _ = ModMain.Chat.SendMessage("heal_companion", $"Компаньон вылечил союзника на {healAmount} HP");
                }

                break;
            }
        }

        private static void TryShareAmmo(int sharerEntityId, SquadData squad)
        {
            if (!_lastAmmoShareTime.ContainsKey(sharerEntityId))
                _lastAmmoShareTime[sharerEntityId] = 0f;

            if (Time.time - _lastAmmoShareTime[sharerEntityId] < AmmoShareInterval)
                return;

            _lastAmmoShareTime[sharerEntityId] = Time.time;

            var sharer = CompanionManager.GetCompanion(sharerEntityId);
            if (sharer == null || sharer.Entity == null)
                return;

            var sharerInventory = InventorySystem.GetInventory(sharerEntityId);
            string[] ammoTypes = { "ammo9mmBulletBall", "ammo762mmBulletBall", "ammoShotgunShell" };

            foreach (var ammoType in ammoTypes)
            {
                int sharerAmmo = sharerInventory.GetItemCount(ammoType);
                if (sharerAmmo < 20)
                    continue;

                foreach (int targetEntityId in squad.MemberEntityIds)
                {
                    if (targetEntityId == sharerEntityId)
                        continue;

                    var target = CompanionManager.GetCompanion(targetEntityId);
                    if (target == null || target.Entity == null || target.Entity.IsDead())
                        continue;

                    float distance = Vector3.Distance(sharer.Entity.position, target.Entity.position);
                    if (distance > InteractionRange)
                        continue;

                    var targetInventory = InventorySystem.GetInventory(targetEntityId);
                    int targetAmmo = targetInventory.GetItemCount(ammoType);

                    if (targetAmmo < 10 && sharerAmmo >= 20)
                    {
                        int shareAmount = 10;
                        sharerInventory.RemoveItem(ammoType, shareAmount);
                        targetInventory.AddItem(ammoType, shareAmount);
                        sharerAmmo -= shareAmount;

                        Log.Out($"[CompanionBot] Companion {sharerEntityId} shared {shareAmount}x {ammoType} with companion {targetEntityId}");
                    }
                }
            }
        }

        private static void TryCoordinateActions(int coordinatorEntityId, SquadData squad)
        {
            if (!_lastCoordinationTime.ContainsKey(coordinatorEntityId))
                _lastCoordinationTime[coordinatorEntityId] = 0f;

            if (Time.time - _lastCoordinationTime[coordinatorEntityId] < CoordinationInterval)
                return;

            _lastCoordinationTime[coordinatorEntityId] = Time.time;

            var coordinator = CompanionManager.GetCompanion(coordinatorEntityId);
            if (coordinator == null || coordinator.Entity == null)
                return;

            var role = SquadRoleManager.GetRole(coordinatorEntityId);
            if (role != SquadRole.Leader)
                return;

            var target = coordinator.Entity.GetAttackTarget();
            if (target == null || target.IsDead())
                return;

            foreach (int targetEntityId in squad.MemberEntityIds)
            {
                if (targetEntityId == coordinatorEntityId)
                    continue;

                var squadMate = CompanionManager.GetCompanion(targetEntityId);
                if (squadMate == null || squadMate.Entity == null || squadMate.Entity.IsDead())
                    continue;

                float distance = Vector3.Distance(coordinator.Entity.position, squadMate.Entity.position);
                if (distance > 20f)
                    continue;

                if (squadMate.Entity.GetAttackTarget() == null || squadMate.Entity.GetAttackTarget().IsDead())
                {
                    squadMate.Entity.SetAttackTarget(target);
                    Log.Out($"[CompanionBot] Leader {coordinatorEntityId} coordinated attack on {target.EntityName}");
                }
            }
        }

        public static void ClearInteractionData(int companionEntityId)
        {
            if (_lastHealTime.ContainsKey(companionEntityId))
                _lastHealTime.Remove(companionEntityId);
            if (_lastAmmoShareTime.ContainsKey(companionEntityId))
                _lastAmmoShareTime.Remove(companionEntityId);
            if (_lastCoordinationTime.ContainsKey(companionEntityId))
                _lastCoordinationTime.Remove(companionEntityId);
        }
    }
}
