using System;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleCmdSpawnCompanion : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new[] { "spawncompanion", "scc", "sc" };
    }

    public override string getDescription()
    {
        return "Spawns/kills companion bots. Use 'kill' to remove all.";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        if (_params.Count > 0 && (_params[0].Equals("kill", StringComparison.OrdinalIgnoreCase) || _params[0] == "k"))
        {
            KillAllCompanions();
            return;
        }

        var player = GameManager.Instance.World.GetPrimaryPlayer();
        if (player == null)
        {
            SdtdConsole.Instance.Output("No player found.");
            return;
        }

        var classId = EntityClass.FromString("companionBot");
        if (classId < 0)
        {
            SdtdConsole.Instance.Output("Entity class 'companionBot' not found.");
            return;
        }

        var pos = player.position + player.GetForwardVector() * 3f;

        var ecd = new EntityCreationData
        {
            entityClass = classId,
            id = EntityFactory.nextEntityID++,
            pos = pos,
            rot = Vector3.zero,
            spawnById = -1
        };

        Entity entity = null;
        try
        {
            entity = EntityFactory.CreateEntity(ecd);
        }
        catch (Exception ex)
        {
            SdtdConsole.Instance.Output($"EntityFactory error: {ex.Message}");
            Log.Error($"[CB] CreateEntity error: {ex.StackTrace}");
            return;
        }

        if (entity == null)
        {
            SdtdConsole.Instance.Output("EntityFactory returned null.");
            return;
        }

        try
        {
            GameManager.Instance.World.SpawnEntityInWorld(entity);
            SdtdConsole.Instance.Output($"Spawned companionBot! ID={entity.entityId} Type={entity.GetType().Name}");
            Log.Out($"[CB] Spawned: ID={entity.entityId} Type={entity.GetType().Name}");
        }
        catch (Exception ex)
        {
            SdtdConsole.Instance.Output($"Spawn error: {ex.Message}");
            Log.Error($"[CB] Spawn error: {ex.StackTrace}");
        }
    }

    public static void KillAll()
    {
        var world = GameManager.Instance.World;
        if (world == null) return;
        var ids = new List<int>();
        foreach (var kv in world.Entities.dict)
        {
            if (kv.Value is CompanionEntity)
                ids.Add(kv.Key);
        }
        foreach (var id in ids)
        {
            world.RemoveEntity(id, EnumRemoveEntityReason.Despawned);
        }
        Log.Out($"[CB] Auto-killed {ids.Count} companion(s) on startup");
    }

    private void KillAllCompanions()
    {
        KillAll();
        SdtdConsole.Instance.Output("Companion bots removed.");
    }
}
