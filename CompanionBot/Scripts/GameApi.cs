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
                // Use reflection to find the correct method
                System.Reflection.Assembly assembly = typeof(EntityFactory).Assembly;
                
                // Try to find EntityClass type and its methods
                System.Type entityClassType = assembly.GetType("EntityClass");
                if (entityClassType != null)
                {
                    // Try different method names that might exist
                    string[] methodNames = { 
                        "GetEntityClassFromEntityClassName", 
                        "GetEntityClassByName",
                        "GetByName",
                        "FindByName"
                    };

                    foreach (string methodName in methodNames)
                    {
                        System.Reflection.MethodInfo method = entityClassType.GetMethod(methodName, 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        
                        if (method != null && method.GetParameters().Length == 1 && 
                            method.GetParameters()[0].ParameterType == typeof(string))
                        {
                            object result = method.Invoke(null, new object[] { className });
                            if (result != null)
                            {
                                // Try to get ID from result
                                System.Reflection.PropertyInfo idProp = result.GetType().GetProperty("id");
                                if (idProp != null)
                                {
                                    int id = (int)idProp.GetValue(result);
                                    return EntityFactory.CreateEntity(id, position);
                                }
                            }
                        }
                    }
                }

                // Fallback: try EntityFactory.CreateEntity with string directly
                // Some versions might support this
                System.Reflection.MethodInfo createMethod = typeof(EntityFactory).GetMethod("CreateEntity", 
                    new[] { typeof(string), typeof(Vector3) });
                
                if (createMethod != null)
                {
                    return (Entity)createMethod.Invoke(null, new object[] { className, position });
                }

                Log.Error($"[CompanionBot] Could not find method to create entity: {className}");
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
