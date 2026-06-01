using System;
using System.Collections.Generic;

namespace CompanionBot
{
    public enum SquadRole
    {
        Leader,
        Assault,
        Support,
        Medic,
        Sniper,
        Tank,
        Scout
    }

    public class RoleData
    {
        public SquadRole Role { get; set; }
        public float DamageModifier { get; set; }
        public float DefenseModifier { get; set; }
        public float SpeedModifier { get; set; }
        public float HealModifier { get; set; }
        public float RangeModifier { get; set; }
        public string Description { get; set; }

        public RoleData(SquadRole role)
        {
            Role = role;
            SetRoleModifiers();
        }

        private void SetRoleModifiers()
        {
            switch (Role)
            {
                case SquadRole.Leader:
                    DamageModifier = 1.1f;
                    DefenseModifier = 1.1f;
                    SpeedModifier = 1.0f;
                    HealModifier = 1.0f;
                    RangeModifier = 1.0f;
                    Description = "Boosts squad morale (+10% damage, +10% defense)";
                    break;
                case SquadRole.Assault:
                    DamageModifier = 1.3f;
                    DefenseModifier = 0.9f;
                    SpeedModifier = 1.1f;
                    HealModifier = 1.0f;
                    RangeModifier = 1.0f;
                    Description = "Frontline fighter (+30% damage, -10% defense, +10% speed)";
                    break;
                case SquadRole.Support:
                    DamageModifier = 1.0f;
                    DefenseModifier = 1.0f;
                    SpeedModifier = 1.0f;
                    HealModifier = 1.5f;
                    RangeModifier = 1.0f;
                    Description = "Provides support and healing (+50% healing)";
                    break;
                case SquadRole.Medic:
                    DamageModifier = 0.8f;
                    DefenseModifier = 1.0f;
                    SpeedModifier = 1.0f;
                    HealModifier = 2.0f;
                    RangeModifier = 1.0f;
                    Description = "Dedicated healer (+100% healing, -20% damage)";
                    break;
                case SquadRole.Sniper:
                    DamageModifier = 1.5f;
                    DefenseModifier = 0.8f;
                    SpeedModifier = 0.9f;
                    HealModifier = 1.0f;
                    RangeModifier = 1.5f;
                    Description = "Long-range specialist (+50% damage, +50% range, -20% defense)";
                    break;
                case SquadRole.Tank:
                    DamageModifier = 0.9f;
                    DefenseModifier = 1.5f;
                    SpeedModifier = 0.8f;
                    HealModifier = 1.0f;
                    RangeModifier = 0.8f;
                    Description = "Heavy defender (+50% defense, -10% damage, -20% speed)";
                    break;
                case SquadRole.Scout:
                    DamageModifier = 1.0f;
                    DefenseModifier = 0.9f;
                    SpeedModifier = 1.3f;
                    HealModifier = 1.0f;
                    RangeModifier = 1.2f;
                    Description = "Fast reconnaissance (+30% speed, +20% range, -10% defense)";
                    break;
            }
        }
    }

    public static class SquadRoleManager
    {
        private static Dictionary<int, SquadRole> _companionRoles = new Dictionary<int, SquadRole>();
        private static Dictionary<SquadRole, RoleData> _roleDataCache = new Dictionary<SquadRole, RoleData>();

        static SquadRoleManager()
        {
            foreach (SquadRole role in Enum.GetValues(typeof(SquadRole)))
            {
                _roleDataCache[role] = new RoleData(role);
            }
        }

        public static void AssignRole(int companionEntityId, SquadRole role)
        {
            _companionRoles[companionEntityId] = role;
            Log.Out($"[CompanionBot] Companion {companionEntityId} assigned role: {role}");
        }

        public static SquadRole GetRole(int companionEntityId)
        {
            return _companionRoles.ContainsKey(companionEntityId) ? _companionRoles[companionEntityId] : SquadRole.Assault;
        }

        public static RoleData GetRoleData(int companionEntityId)
        {
            var role = GetRole(companionEntityId);
            return _roleDataCache[role];
        }

        public static void RemoveRole(int companionEntityId)
        {
            if (_companionRoles.ContainsKey(companionEntityId))
            {
                _companionRoles.Remove(companionEntityId);
            }
        }

        public static Dictionary<int, SquadRole> GetAllRoles()
        {
            return new Dictionary<int, SquadRole>(_companionRoles);
        }
    }
}
