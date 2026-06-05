using UnityEngine;

public class CompanionEntity : EntityAlive
{
    private Vector3 _smoothDir;
    private readonly ItemStack[] _inventory = new ItemStack[20];
    private float _lastHealth = -1f;
    private float _lastPlayerHealth = -1f;
    private const float FollowDist = 1.5f;
    private const float MinDist = 1.0f;
    private const float MoveSpeed = 0.1f;
    private const float RetreatSpeed = 0.15f;
    private const float SmoothFactor = 0.12f;
    private bool _followEnabled = false;

    public void SetTalking(bool talking)
    {
        var anim = GetComponentInChildren<Animator>();
        if (anim != null && anim.isActiveAndEnabled)
        {
            anim.SetBool("Talking", talking);
        }
    }

    public void SetFollowMode(bool enabled)
    {
        _followEnabled = enabled;
        if (!enabled)
        {
            _smoothDir = Vector3.zero;
            motion = new Vector3(0f, motion.y, 0f);
        }
    }

    public ItemStack[] GetInventory() => _inventory;

    public override void PostInit()
    {
        base.PostInit();
        for (int i = 0; i < _inventory.Length; i++)
            _inventory[i] = ItemStack.Empty.Clone();
        IsGodMode.Value = false;
        SetSpawnerSource(EnumSpawnerSource.Biome);
        PhysicsTransform.gameObject.SetActive(true);
        if (ModelTransform != null)
            ModelTransform.gameObject.SetActive(true);

        if (GameManager.Instance.World.GetPrimaryPlayer() is EntityPlayerLocal)
        {
            BotHttpServer.Instance?.Start();
        }
    }

    public override void OnUpdateLive()
    {
        if (IsDead()) return;

        var player = GameManager.Instance.World.GetPrimaryPlayer();
        if (player == null || player.IsDead()) return;

        if (!_followEnabled)
        {
            speedForward = 0f;
            speedStrafe = 0f;
            return;
        }

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

        var horizSpeed = new Vector3(motion.x, 0f, motion.z).magnitude;
        if (horizSpeed > 0.01f && motion.y < 0.5f)
        {
            var fwd = new Vector3(motion.x, 0f, motion.z).normalized;
            var origin = position + Vector3.up * 0.3f;
            var rayDist = Mathf.Max(0.5f, horizSpeed * 8f);
            if (Physics.Raycast(origin, fwd, rayDist))
            {
                if (Physics.Raycast(position, Vector3.down, 1.5f))
                    motion.y = 8f;
            }
        }

        var yDiff = player.position.y - position.y;
        if (yDiff > 0.8f && motion.y < 1f)
        {
            if (Physics.Raycast(position, Vector3.down, out _, 1.5f))
                motion.y = 8f;
        }
        else if (yDiff < -0.8f && motion.y > -1f)
        {
            motion.y = -5f;
        }

        speedForward = horizSpeed;
        speedStrafe = 0f;

        var h = Health;
        if (_lastHealth < 0f) _lastHealth = h;
        if (h < _lastHealth)
        {
            var dmg = _lastHealth - h;
            _lastHealth = h;
            BotHttpServer.ForwardEvent("bot_damaged", $"\"damage\":{dmg},\"health\":{h},\"maxHealth\":{GetMaxHealth()}");
        }
        else if (h > _lastHealth) _lastHealth = h;

        var ph = player.Health;
        if (_lastPlayerHealth < 0f) _lastPlayerHealth = ph;
        if (ph < _lastPlayerHealth)
        {
            var dmg = _lastPlayerHealth - ph;
            _lastPlayerHealth = ph;
            BotHttpServer.ForwardEvent("player_damaged", $"\"damage\":{dmg},\"health\":{ph},\"maxHealth\":{player.GetMaxHealth()}");
        }
        else if (ph > _lastPlayerHealth) _lastPlayerHealth = ph;
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
