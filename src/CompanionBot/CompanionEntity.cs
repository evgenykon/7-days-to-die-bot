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

    private AudioSource _audio;
    private float[] _pendingSamples;
    private int _pendingChannels;
    private int _pendingSampleRate;
    private bool _wavPlaying;
    private float _volume = 0.8f;

    private Animator _cachedAnim;
    private bool _paramsCached;
    private bool _hasDanceTypeID;
    private bool _hasHappy;
    private bool _hasAngry;

    public void SetTalking(bool talking)
    {
        var anim = GetComponentInChildren<Animator>();
        if (anim == null || !anim.isActiveAndEnabled) return;

        anim.SetBool("Talking", talking);

        if (!_paramsCached || anim != _cachedAnim)
        {
            _cachedAnim = anim;
            _paramsCached = true;
            _hasDanceTypeID = _hasHappy = _hasAngry = false;
            var paramNames = "";
            foreach (var p in anim.parameters)
            {
                paramNames += $"{p.name}({p.type}) ";
                if (p.name == "DanceTypeID") _hasDanceTypeID = true;
                else if (p.name == "Happy") _hasHappy = true;
                else if (p.name == "Angry") _hasAngry = true;
            }
            Log.Out($"[CB] Animator parameters: {paramNames}");
        }

        if (talking)
        {
            if (_hasDanceTypeID)
                anim.SetInteger("DanceTypeID", UnityEngine.Random.Range(1, 4));
            if (_hasHappy && UnityEngine.Random.value < 0.3f)
                anim.SetBool("Happy", true);
            if (_hasAngry && UnityEngine.Random.value < 0.1f)
                anim.SetBool("Angry", true);
        }
        else
        {
            if (_hasDanceTypeID) anim.SetInteger("DanceTypeID", 0);
            if (_hasHappy) anim.SetBool("Happy", false);
            if (_hasAngry) anim.SetBool("Angry", false);
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

    public void ScheduleWav(byte[] wavBytes)
    {
        float[] samples;
        int channels, sampleRate;
        if (!ParseWav(wavBytes, out samples, out channels, out sampleRate)) return;
        _pendingSamples = samples;
        _pendingChannels = channels;
        _pendingSampleRate = sampleRate;
    }

    public void SetVolume(float vol)
    {
        _volume = Mathf.Clamp01(vol);
        if (_audio != null) _audio.volume = _volume;
    }

    private static bool ParseWav(byte[] wav, out float[] samples, out int channels, out int sampleRate)
    {
        samples = null;
        channels = 1;
        sampleRate = 22050;
        if (wav == null || wav.Length < 44) return false;
        if (System.Text.Encoding.ASCII.GetString(wav, 0, 4) != "RIFF") return false;
        if (System.Text.Encoding.ASCII.GetString(wav, 8, 4) != "WAVE") return false;
        channels = wav[22] | (wav[23] << 8);
        sampleRate = wav[24] | (wav[25] << 8) | (wav[26] << 16) | (wav[27] << 24);
        int bitsPerSample = wav[34] | (wav[35] << 8);
        int offset = 12, dataSize = 0;
        while (offset < wav.Length - 8)
        {
            int chunkSize = wav[offset + 4] | (wav[offset + 5] << 8) | (wav[offset + 6] << 16) | (wav[offset + 7] << 24);
            if (System.Text.Encoding.ASCII.GetString(wav, offset, 4) == "data") { dataSize = chunkSize; offset += 8; break; }
            offset += 8 + chunkSize;
        }
        if (dataSize <= 0 || offset >= wav.Length) return false;
        int dataEnd = offset + dataSize;
        if (dataEnd > wav.Length) dataEnd = wav.Length;
        int byteCount = dataEnd - offset;
        int sampleCount = byteCount / (bitsPerSample / 8);
        samples = new float[sampleCount];
        if (bitsPerSample == 16)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                int idx = offset + i * 2;
                if (idx + 1 >= wav.Length) break;
                samples[i] = (short)(wav[idx] | (wav[idx + 1] << 8)) / 32768f;
            }
        }
        else if (bitsPerSample == 8)
        {
            for (int i = 0; i < sampleCount; i++)
                samples[i] = (wav[offset + i] - 128) / 128f;
        }
        else return false;
        return true;
    }

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

        _audio = gameObject.GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 1f;
        _audio.volume = _volume;
        _audio.minDistance = 3f;
        _audio.maxDistance = 40f;
        _audio.rolloffMode = AudioRolloffMode.Linear;

        if (GameManager.Instance.World.GetPrimaryPlayer() is EntityPlayerLocal)
        {
            BotHttpServer.Instance?.Start();
        }
    }

    public override void OnUpdateLive()
    {
        if (IsDead()) return;

        // Audio playback (main thread)
        if (_pendingSamples != null && !_wavPlaying)
        {
            _wavPlaying = true;
            if (_audio == null)
            {
                _audio = gameObject.GetComponent<AudioSource>();
                if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
                _audio.spatialBlend = 1f;
            }
            _audio.volume = _volume;
            var clip = AudioClip.Create("TTS", _pendingSamples.Length / _pendingChannels, _pendingChannels, _pendingSampleRate, false);
            clip.SetData(_pendingSamples, 0);
            _pendingSamples = null;
            _audio.clip = clip;
            _audio.Play();
        }
        else if (_wavPlaying && _audio != null && !_audio.isPlaying)
        {
            _wavPlaying = false;
            if (_pendingSamples == null)
                SetTalking(false);
        }
        if (_wavPlaying) return;

        // Movement follows below
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

        // Obstacle jump
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

        // Edge detection — don't walk off cliffs
        if (horizSpeed > 0.01f && motion.y < 0.5f)
        {
            var fwd = new Vector3(motion.x, 0f, motion.z).normalized;
            var ahead = position + Vector3.up * 0.3f + fwd * 1.2f;
            if (!Physics.Raycast(ahead, Vector3.down, out _, 5f))
            {
                motion.x = 0f;
                motion.z = 0f;
                horizSpeed = 0f;
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
            if (Physics.Raycast(position, Vector3.down, out _, 4f))
                motion.y = -3f;
        }

        // Fall damage prevention — slow descent near ground
        if (motion.y < -3f)
        {
            if (Physics.Raycast(position, Vector3.down, out var hit, 8f))
            {
                if (hit.distance < 5f)
                    motion.y = Mathf.Max(motion.y, -2f);
            }
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

    private const float RotLerpSpeed = 0.08f;

    public override void OnUpdatePosition(float _partialTicks)
    {
        base.OnUpdatePosition(_partialTicks);
        var speed = new Vector3(motion.x, 0f, motion.z).magnitude;
        if (speed > 0.001f)
        {
            rotation = new Vector3(rotation.x, Mathf.Atan2(motion.x, motion.z) * Mathf.Rad2Deg, rotation.z);
        }
        else
        {
            var player = GameManager.Instance.World?.GetPrimaryPlayer();
            if (player != null && !player.IsDead())
            {
                var dir = player.position - position; dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    var target = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    var diff = target - rotation.y;
                    while (diff > 180f) diff -= 360f;
                    while (diff < -180f) diff += 360f;
                    rotation = new Vector3(rotation.x, rotation.y + diff * RotLerpSpeed, rotation.z);
                }
            }
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
