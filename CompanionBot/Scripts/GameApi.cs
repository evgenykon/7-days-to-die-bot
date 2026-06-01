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
                
                // Log all available methods in EntityClass for debugging
                System.Type entityClassType = assembly.GetType("EntityClass");
                if (entityClassType != null)
                {
                    Log.Out($"[CompanionBot] EntityClass methods:");
                    foreach (var method in entityClassType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                    {
                        var parameters = string.Join(", ", Array.ConvertAll(method.GetParameters(), p => p.ParameterType.Name));
                        Log.Out($"  {method.ReturnType.Name} {method.Name}({parameters})");
                    }
                }

                // Try EntityClassList if it exists
                System.Type entityClassListType = assembly.GetType("EntityClassList");
                if (entityClassListType != null)
                {
                    Log.Out($"[CompanionBot] EntityClassList methods:");
                    foreach (var method in entityClassListType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance))
                    {
                        var parameters = string.Join(", ", Array.ConvertAll(method.GetParameters(), p => p.ParameterType.Name));
                        Log.Out($"  {method.ReturnType.Name} {method.Name}({parameters})");
                    }

                    // Try to get instance
                    var instanceField = entityClassListType.GetField("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (instanceField != null)
                    {
                        var instance = instanceField.GetValue(null);
                        if (instance != null)
                        {
                            // Try GetEntityClass method
                            var getMethod = entityClassListType.GetMethod("GetEntityClass", new[] { typeof(string) });
                            if (getMethod != null)
                            {
                                var entityClass = getMethod.Invoke(instance, new object[] { className });
                                if (entityClass != null)
                                {
                                    var idProp = entityClass.GetType().GetProperty("id");
                                    if (idProp != null)
                                    {
                                        int id = (int)idProp.GetValue(entityClass);
                                        Log.Out($"[CompanionBot] Found entity class ID: {id} for {className}");
                                        return EntityFactory.CreateEntity(id, position);
                                    }
                                }
                            }
                        }
                    }
                }

                // Try EntityClass static methods
                if (entityClassType != null)
                {
                    string[] methodNames = { 
                        "GetEntityClassFromEntityClassName", 
                        "GetEntityClassByName",
                        "GetByName",
                        "FindByName",
                        "GetEntityClass"
                    };

                    foreach (string methodName in methodNames)
                    {
                        System.Reflection.MethodInfo method = entityClassType.GetMethod(methodName, 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        
                        if (method != null)
                        {
                            Log.Out($"[CompanionBot] Trying method: {methodName}");
                            var parameters = method.GetParameters();
                            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                            {
                                object result = method.Invoke(null, new object[] { className });
                                if (result != null)
                                {
                                    System.Reflection.PropertyInfo idProp = result.GetType().GetProperty("id");
                                    if (idProp != null)
                                    {
                                        int id = (int)idProp.GetValue(result);
                                        Log.Out($"[CompanionBot] Found entity class ID: {id} for {className}");
                                        return EntityFactory.CreateEntity(id, position);
                                    }
                                }
                            }
                        }
                    }
                }

                // Fallback: try EntityFactory.CreateEntity with string directly
                System.Reflection.MethodInfo createMethod = typeof(EntityFactory).GetMethod("CreateEntity", 
                    new[] { typeof(string), typeof(Vector3) });
                
                if (createMethod != null)
                {
                    Log.Out($"[CompanionBot] Using EntityFactory.CreateEntity(string, Vector3)");
                    return (Entity)createMethod.Invoke(null, new object[] { className, position });
                }

                Log.Error($"[CompanionBot] Could not find method to create entity: {className}");
                return null;
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
