using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public enum FormationType
    {
        Line,
        Wedge,
        Circle,
        Column,
        Free
    }

    public class SquadData
    {
        public int OwnerEntityId { get; set; }
        public List<int> MemberEntityIds { get; set; }
        public FormationType Formation { get; set; }
        public float FormationSpacing { get; set; }
        public Vector3 FormationCenter { get; set; }
        public bool IsFormationActive { get; set; }

        public SquadData(int ownerEntityId)
        {
            OwnerEntityId = ownerEntityId;
            MemberEntityIds = new List<int>();
            Formation = FormationType.Free;
            FormationSpacing = 3f;
            FormationCenter = Vector3.zero;
            IsFormationActive = false;
        }
    }

    public static class SquadManager
    {
        private static Dictionary<int, SquadData> _squads = new Dictionary<int, SquadData>();

        public static SquadData GetSquad(int ownerEntityId)
        {
            if (!_squads.ContainsKey(ownerEntityId))
            {
                _squads[ownerEntityId] = new SquadData(ownerEntityId);
            }
            return _squads[ownerEntityId];
        }

        public static void AddToSquad(int ownerEntityId, int companionEntityId)
        {
            var squad = GetSquad(ownerEntityId);
            if (!squad.MemberEntityIds.Contains(companionEntityId))
            {
                squad.MemberEntityIds.Add(companionEntityId);
                Log.Out($"[CompanionBot] Companion {companionEntityId} added to squad of player {ownerEntityId}");
            }
        }

        public static void RemoveFromSquad(int ownerEntityId, int companionEntityId)
        {
            var squad = GetSquad(ownerEntityId);
            if (squad.MemberEntityIds.Contains(companionEntityId))
            {
                squad.MemberEntityIds.Remove(companionEntityId);
                Log.Out($"[CompanionBot] Companion {companionEntityId} removed from squad of player {ownerEntityId}");
            }
        }

        public static void SetFormation(int ownerEntityId, FormationType formation, float spacing = 3f)
        {
            var squad = GetSquad(ownerEntityId);
            squad.Formation = formation;
            squad.FormationSpacing = spacing;
            squad.IsFormationActive = formation != FormationType.Free;
            Log.Out($"[CompanionBot] Squad formation set to {formation} with spacing {spacing}");
        }

        public static void UpdateFormation(int ownerEntityId, Vector3 ownerPosition, Vector3 ownerForward)
        {
            var squad = GetSquad(ownerEntityId);
            if (!squad.IsFormationActive || squad.MemberEntityIds.Count == 0)
                return;

            var positions = CalculateFormationPositions(ownerPosition, ownerForward, squad);
            
            for (int i = 0; i < squad.MemberEntityIds.Count && i < positions.Count; i++)
            {
                int companionEntityId = squad.MemberEntityIds[i];
                var companionData = CompanionManager.GetCompanion(companionEntityId);
                
                if (companionData != null && companionData.Entity != null && !companionData.Entity.IsDead())
                {
                    Vector3 targetPosition = positions[i];
                    float distance = Vector3.Distance(companionData.Entity.position, targetPosition);
                    
                    if (distance > 2f)
                    {
                        MoveTowards(companionData.Entity, targetPosition);
                    }
                }
            }
        }

        private static List<Vector3> CalculateFormationPositions(Vector3 center, Vector3 forward, SquadData squad)
        {
            var positions = new List<Vector3>();
            int count = squad.MemberEntityIds.Count;
            float spacing = squad.FormationSpacing;

            switch (squad.Formation)
            {
                case FormationType.Line:
                    CalculateLineFormation(center, forward, count, spacing, positions);
                    break;
                case FormationType.Wedge:
                    CalculateWedgeFormation(center, forward, count, spacing, positions);
                    break;
                case FormationType.Circle:
                    CalculateCircleFormation(center, count, spacing, positions);
                    break;
                case FormationType.Column:
                    CalculateColumnFormation(center, forward, count, spacing, positions);
                    break;
            }

            return positions;
        }

        private static void CalculateLineFormation(Vector3 center, Vector3 forward, int count, float spacing, List<Vector3> positions)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float totalWidth = (count - 1) * spacing;
            Vector3 startPos = center - right * (totalWidth / 2f);

            for (int i = 0; i < count; i++)
            {
                positions.Add(startPos + right * (i * spacing));
            }
        }

        private static void CalculateWedgeFormation(Vector3 center, Vector3 forward, int count, float spacing, List<Vector3> positions)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 back = -forward.normalized;

            positions.Add(center);

            int side = 1;
            int row = 1;
            int countInRow = 2;
            int placed = 1;

            while (placed < count)
            {
                for (int i = 0; i < countInRow && placed < count; i++)
                {
                    float xOffset = (i - (countInRow - 1) / 2f) * spacing;
                    Vector3 pos = center + back * (row * spacing) + right * xOffset;
                    positions.Add(pos);
                    placed++;
                }
                row++;
                countInRow += 2;
            }
        }

        private static void CalculateCircleFormation(Vector3 center, int count, float spacing, List<Vector3> positions)
        {
            float radius = spacing * count / (2f * Mathf.PI);
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
                positions.Add(center + offset);
            }
        }

        private static void CalculateColumnFormation(Vector3 center, Vector3 forward, int count, float spacing, List<Vector3> positions)
        {
            Vector3 back = -forward.normalized;

            for (int i = 0; i < count; i++)
            {
                positions.Add(center + back * (i * spacing));
            }
        }

        private static void MoveTowards(EntityAlive companion, Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - companion.position).normalized;
            companion.Move(direction * companion.MoveSpeed);
            companion.RotateToTarget(targetPosition);
        }

        public static void ExecuteSquadCommand(int ownerEntityId, Action<int> command)
        {
            var squad = GetSquad(ownerEntityId);
            foreach (int companionEntityId in squad.MemberEntityIds)
            {
                command(companionEntityId);
            }
        }

        public static void AllFollow(int ownerEntityId)
        {
            ExecuteSquadCommand(ownerEntityId, (entityId) =>
            {
                var companionData = CompanionManager.GetCompanion(entityId);
                if (companionData != null)
                {
                    CompanionManager.SetState(entityId, CompanionState.Follow);
                }
            });
            Log.Out($"[CompanionBot] All squad members set to follow");
        }

        public static void AllGuard(int ownerEntityId, Vector3 position, float radius = 15f)
        {
            ExecuteSquadCommand(ownerEntityId, (entityId) =>
            {
                AdvancedAI.SetGuardArea(entityId, position, radius);
            });
            Log.Out($"[CompanionBot] All squad members set to guard at {position}");
        }

        public static void AllAttack(int ownerEntityId)
        {
            var squad = GetSquad(ownerEntityId);
            var owner = GameManager.Instance.World.GetEntity(ownerEntityId) as EntityPlayer;
            
            if (owner == null)
                return;

            var target = owner.GetAttackTarget();
            if (target != null)
            {
                ExecuteSquadCommand(ownerEntityId, (entityId) =>
                {
                    var companionData = CompanionManager.GetCompanion(entityId);
                    if (companionData != null && companionData.Entity != null)
                    {
                        companionData.Entity.SetAttackTarget(target);
                    }
                });
                Log.Out($"[CompanionBot] All squad members attacking target");
            }
        }

        public static void RemoveSquad(int ownerEntityId)
        {
            if (_squads.ContainsKey(ownerEntityId))
            {
                _squads.Remove(ownerEntityId);
                Log.Out($"[CompanionBot] Squad removed for player {ownerEntityId}");
            }
        }

        public static int GetSquadSize(int ownerEntityId)
        {
            var squad = GetSquad(ownerEntityId);
            return squad.MemberEntityIds.Count;
        }

        public static bool IsInSquad(int ownerEntityId, int companionEntityId)
        {
            var squad = GetSquad(ownerEntityId);
            return squad.MemberEntityIds.Contains(companionEntityId);
        }
    }
}
