using System;
using System.Collections.Generic;
using UnityEngine;

namespace CompanionBot
{
    public static class GameApi
    {
        public const float DefaultMoveSpeed = 1.5f;

        public static void MoveTo(EntityAlive entity, Vector3 targetPosition, float speed = DefaultMoveSpeed)
        {
            if (entity == null) return;
            float distance = Vector3.Distance(entity.position, targetPosition);
            if (distance < 0.5f) return;

            Vector3 direction = (targetPosition - entity.position).normalized;
            Vector3 motion = direction * speed;
            motion.y = -9.8f * 0.02f;

            entity.motion = motion;
            entity.speedForward = speed;
            entity.moveDirection = direction;
            entity.Move(motion, false, 0f, 0f);
        }

        public static void StopMoving(EntityAlive entity)
        {
            if (entity == null) return;
            entity.motion = Vector3.zero;
            entity.speedForward = 0f;
            entity.speedStrafe = 0f;
            entity.moveDirection = Vector3.zero;
        }

        public static void LookAt(EntityAlive entity, Vector3 targetPosition)
        {
            if (entity == null) return;
            Vector3 direction = targetPosition - entity.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                entity.transform.rotation = Quaternion.Euler(0, angle, 0);
            }
        }

        public static void StopMoving(EntityAlive entity)
        {
            if (entity == null) return;
            if (entity is EntityPlayer player && player.moveHelper != null)
                player.moveHelper.StopMove();
        }

        public static void SetTarget(EntityAlive entity, EntityAlive target)
        {
            if (entity == null) return;
            entity.SetAttackTarget(target, 0);
        }

        public static EntityAlive GetTarget(EntityAlive entity)
        {
            if (entity == null) return null;
            return entity.GetAttackTarget();
        }

        public static bool IsDay()
        {
            if (GameManager.Instance == null) return true;
            int hour = GameUtils.WorldTimeToHours(GameManager.Instance.World.worldTime);
            return hour >= 6 && hour < 22;
        }

        public static int GetDayNumber()
        {
            if (GameManager.Instance == null) return 0;
            return GameUtils.WorldTimeToDays(GameManager.Instance.World.worldTime);
        }

        public static Entity CreateEntity(string className, Vector3 position)
        {
            if (string.IsNullOrEmpty(className))
                return null;

            try
            {
                int entityClassId = EntityClass.FromString(className);
                if (entityClassId < 0)
                {
                    Log.Error($"[CompanionBot] Entity class not found: {className}");
                    return null;
                }

                Log.Out($"[CompanionBot] Found entity class ID: {entityClassId} for {className}");

                var ecd = new EntityCreationData();
                ecd.entityClass = entityClassId;
                ecd.id = EntityFactory.nextEntityID++;
                ecd.pos = position;
                ecd.rot = Vector3.zero;
                ecd.spawnById = -1;
                ecd.spawnByName = null;
                ecd.skinTexture = "";
                ecd.playerProfile = new PlayerProfile
                {
                    isMale = className.Contains("male") || !className.Contains("female"),
                    archetype = "random"
                };

                Entity entity = EntityFactory.CreateEntity(ecd);
                if (entity == null)
                {
                    Log.Error($"[CompanionBot] EntityFactory.CreateEntity returned null for {className}");
                    return null;
                }

                GameManager.Instance.World.SpawnEntityInWorld(entity);
                Log.Out($"[CompanionBot] Entity spawned in world: ID={ecd.id}, classId={entityClassId}");
                return entity;
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to create entity {className}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public static string GetEntityName(Entity entity)
        {
            if (entity == null) return "unknown";
            if (entity is EntityAlive alive)
                return alive.EntityName ?? "unknown";
            return entity.name ?? "unknown";
        }

        public static EntityAlive AsEntityAlive(Entity entity)
        {
            return entity as EntityAlive;
        }
    }
}
