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
