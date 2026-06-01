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
            Vector3 direction = (targetPosition - entity.position).normalized;
            entity.Move(direction * speed, false, 0f, 0f);
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
                // Use reflection to find the correct method to get entity class ID by name
                var assembly = typeof(EntityFactory).Assembly;
                
                // Try to find EntityClassList type
                var entityClassListType = assembly.GetType("EntityClassList");
                if (entityClassListType != null)
                {
                    // Try to get static instance or field
                    var instanceField = entityClassListType.GetField("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (instanceField != null)
                    {
                        var instance = instanceField.GetValue(null);
                        if (instance != null)
                        {
                            // Try to find GetIdByName or similar method
                            var getIdMethod = entityClassListType.GetMethod("GetIdByName", new[] { typeof(string) });
                            if (getIdMethod != null)
                            {
                                int id = (int)getIdMethod.Invoke(instance, new object[] { className });
                                if (id >= 0)
                                {
                                    return EntityFactory.CreateEntity(id, position);
                                }
                            }
                        }
                    }
                }

                // Fallback: try to find entity class ID through EntityClass static methods
                var entityClassType = typeof(EntityClass);
                var getByNameMethod = entityClassType.GetMethod("GetByName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new[] { typeof(string) }, null);
                if (getByNameMethod != null)
                {
                    var entityClass = getByNameMethod.Invoke(null, new object[] { className });
                    if (entityClass != null)
                    {
                        var idProperty = entityClassType.GetProperty("id");
                        if (idProperty != null)
                        {
                            int id = (int)idProperty.GetValue(entityClass);
                            return EntityFactory.CreateEntity(id, position);
                        }
                    }
                }

                Log.Error($"[CompanionBot] Could not find method to get entity class ID for: {className}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to create entity {className}: {ex.Message}");
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
