using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBotV2
{
    public class CompanionEntity : EntityZombie
    {
        private EntityPlayer _owner;
        private float _nextOwnerSearch;
        private const float SearchInterval = 2f;
        private const float FollowDistance = 3f;
        private const float TeleportDistance = 20f;

        public override void Init()
        {
            base.Init();
            Log.Out($"[CompanionBot v2] CompanionEntity Init: {entityId}");
        }

        public override void OnAddedToWorld()
        {
            base.OnAddedToWorld();
            Log.Out($"[CompanionBot v2] Added to world: {entityId}");
            SetFaction("player");
        }

        public override void OnUpdateLive()
        {
            base.OnUpdateLive();
            if (IsDead()) return;
            Tick();
        }

        private void Tick()
        {
            if (Time.time > _nextOwnerSearch)
            {
                _nextOwnerSearch = Time.time + SearchInterval;
                FindOwner();
            }
            if (_owner == null || _owner.IsDead()) return;

            float dist = Vector3.Distance(position, _owner.position);

            if (dist > TeleportDistance)
            {
                TeleportToOwner();
            }
            else if (dist > FollowDistance)
            {
                moveHelper.SetMoveTo(_owner.position + (_owner.position - position).normalized * FollowDistance, true);
            }
            else
            {
                moveHelper.StopMove();
                LookAt(_owner.position);
            }
        }

        private void FindOwner()
        {
            if (GameManager.Instance?.World?.Players?.list == null) return;
            var players = GameManager.Instance.World.Players.list;
            if (players.Count == 0) return;

            EntityPlayer nearest = null;
            float minDist = float.MaxValue;

            foreach (var player in players)
            {
                float dist = Vector3.Distance(position, player.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = player;
                }
            }
            _owner = nearest;
        }

        private void TeleportToOwner()
        {
            Vector3 target = _owner.position + _owner.GetForwardVector() * 2f;
            SetPosition(target);
            if (moveHelper != null)
                moveHelper.StopMove();
            Log.Out($"[CompanionBot v2] Teleported to owner");
        }

        private void LookAt(Vector3 lookTarget)
        {
            Vector3 dir = lookTarget - position;
            if (dir.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, angle, 0);
            }
        }

        private void SetFaction(string factionName)
        {
            try
            {
                if (FactionManager.Instance != null)
                {
                    var faction = FactionManager.Instance.GetFactionByName(factionName);
                    if (faction != null)
                    {
                        SetFactionInternal(faction);
                        Log.Out($"[CompanionBot v2] Faction set to {factionName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot v2] Failed to set faction: {ex.Message}");
            }
        }

        private void SetFactionInternal(Faction faction)
        {
            var field = typeof(EntityAlive).GetField("faction", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(this, faction);
        }
    }
}
