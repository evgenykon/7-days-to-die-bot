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
                System.Reflection.Assembly assembly = typeof(EntityFactory).Assembly;
                System.Type entityClassType = assembly.GetType("EntityClass");
                
                if (entityClassType != null)
                {
                    // Try GetId method first - this should return the correct ID
                    System.Reflection.MethodInfo getIdMethod = entityClassType.GetMethod("GetId", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (getIdMethod != null)
                    {
                        int id = (int)getIdMethod.Invoke(null, new object[] { className });
                        if (id >= 0)
                        {
                            Log.Out($"[CompanionBot] Found entity class ID: {id} for {className}");
                            return EntityFactory.CreateEntity(id, position);
                        }
                    }

                    // Fallback: try FromString method
                    System.Reflection.MethodInfo fromStringMethod = entityClassType.GetMethod("FromString", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (fromStringMethod != null)
                    {
                        int id = (int)fromStringMethod.Invoke(null, new object[] { className });
                        if (id >= 0)
                        {
                            Log.Out($"[CompanionBot] Found entity class ID (FromString): {id} for {className}");
                            return EntityFactory.CreateEntity(id, position);
                        }
                    }
                }

                Log.Error($"[CompanionBot] Could not find entity class: {className}");
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
