using UnityEngine;

public class CompanionEntity : EntityAlive
{
    private Vector3 _smoothDir;
    private const float FollowDist = 1.5f;
    private const float MinDist = 1.0f;
    private const float MoveSpeed = 0.2f;
    private const float RetreatSpeed = 0.3f;
    private const float SmoothFactor = 0.12f;

    public override void PostInit()
    {
        base.PostInit();
        IsGodMode.Value = false;
        SetSpawnerSource(EnumSpawnerSource.Biome);
        PhysicsTransform.gameObject.SetActive(true);
        if (ModelTransform != null)
            ModelTransform.gameObject.SetActive(true);
    }

    public override void OnUpdateLive()
    {
        if (IsDead()) return;

        var player = GameManager.Instance.World.GetPrimaryPlayer();
        if (player == null || player.IsDead()) return;

        var dist = Vector3.Distance(position, player.position);
        if (dist > FollowDist)
        {
            var targetDir = (player.position - position).normalized;
            _smoothDir = Vector3.Lerp(_smoothDir, targetDir, SmoothFactor);
            motion = new Vector3(_smoothDir.x * MoveSpeed, motion.y, _smoothDir.z * MoveSpeed);
        }
        else if (dist < MinDist && dist > 0.01f)
        {
            var awayDir = (position - player.position).normalized;
            _smoothDir = Vector3.Lerp(_smoothDir, awayDir, SmoothFactor);
            motion = new Vector3(_smoothDir.x * RetreatSpeed, motion.y, _smoothDir.z * RetreatSpeed);
        }
        else
        {
            _smoothDir = Vector3.zero;
            motion = new Vector3(0f, motion.y, 0f);
        }

        var yDiff = player.position.y - position.y;
        if (yDiff > 0.5f)
        {
            if (Physics.Raycast(position, Vector3.down, out _, 1.2f))
            {
                motion.y = 7.5f;
            }
        }

        speedForward = new Vector3(motion.x, 0f, motion.z).magnitude;
        speedStrafe = 0f;
    }

    public override void OnUpdatePosition(float _partialTicks)
    {
        base.OnUpdatePosition(_partialTicks);
        var speed = new Vector3(motion.x, 0f, motion.z).magnitude;
        if (speed > 0.001f)
        {
            rotation = new Vector3(rotation.x, Mathf.Atan2(motion.x, motion.z) * Mathf.Rad2Deg, rotation.z);
        }
        var anim = GetComponentInChildren<Animator>();
        if (anim != null && anim.isActiveAndEnabled)
        {
            anim.SetBool("IsMoving", speed > 0.01f);
            anim.SetFloat("Speed", speed);
            anim.SetFloat("speed", speed);
            anim.SetFloat("Forward", speed);
        }
        DefaultMoveEntity(motion, true);
    }

    public override bool CanBePushed() => true;
}
