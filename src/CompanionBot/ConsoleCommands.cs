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
        return "Spawns/kills companion bots. Commands: kill, follow/f, stop/s, give/g, take/t, list/l.";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        if (_params.Count > 0)
        {
            var cmd = _params[0].ToLower();
            if (cmd == "kill" || cmd == "k") { KillAllCompanions(); return; }
            if (cmd == "follow" || cmd == "f") { BotHttpServer.SetFollowState(true); SdtdConsole.Instance.Output("Bot will follow."); return; }
            if (cmd == "stop" || cmd == "s") { BotHttpServer.SetFollowState(false); SdtdConsole.Instance.Output("Bot stopped."); return; }
            if (cmd == "give" || cmd == "g") { GiveToBot(_params); return; }
            if (cmd == "take" || cmd == "t") { TakeFromBot(_params); return; }
            if (cmd == "list" || cmd == "l") { ListInventory(); return; }
        }

        var world = GameManager.Instance.World;
        foreach (var kv in world.Entities.dict)
        {
            if (kv.Value is CompanionEntity && !kv.Value.IsDead())
            {
                SdtdConsole.Instance.Output("Companion is already alive. Use 'kill' to remove first.");
                return;
            }
        }

        var player = world.GetPrimaryPlayer();
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
            BotHttpServer.ForwardEvent("bot_spawned", $"\"entityId\":{entity.entityId}");
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

    private static CompanionEntity GetBot()
    {
        var world = GameManager.Instance.World;
        if (world == null) return null;
        foreach (var kv in world.Entities.dict)
            if (kv.Value is CompanionEntity bot && !bot.IsDead()) return bot;
        return null;
    }

    private static EntityPlayerLocal GetPlayer() =>
        GameManager.Instance.World?.GetPrimaryPlayer() as EntityPlayerLocal;

    private void GiveToBot(List<string> _params)
    {
        if (_params.Count < 2) { SdtdConsole.Instance.Output("Usage: scc give <item> [count]"); return; }
        var bot = GetBot();
        if (bot == null) { SdtdConsole.Instance.Output("No companion alive."); return; }
        var player = GetPlayer();
        if (player == null) return;

        var itemName = _params[1];
        var count = _params.Count > 2 && int.TryParse(_params[2], out var c) ? c : 1;
        var itemClass = ItemClass.GetItemClass(itemName);
        if (itemClass == null) { SdtdConsole.Instance.Output($"Unknown item: {itemName}"); return; }

        var botInv = bot.GetInventory();
        var slots = player.inventory.GetSlots();
        int taken = 0;
        for (int i = 0; i < slots.Length && taken < count; i++)
        {
            if (slots[i].IsEmpty() || slots[i].itemValue.type != itemClass.Id) continue;
            var take = Math.Min(count - taken, slots[i].count);
            slots[i].count -= take;
            taken += take;
            if (slots[i].count <= 0) slots[i] = ItemStack.Empty.Clone();
            var leftover = AddSlots(botInv, new ItemStack(new ItemValue(itemClass.Id, false), take));
            if (leftover > 0) slots[i] = new ItemStack(new ItemValue(itemClass.Id, false), slots[i].count + leftover);
        }
        if (taken == 0) { SdtdConsole.Instance.Output($"You don't have {itemName}."); return; }
        player.inventory.SetSlots(slots);
        SdtdConsole.Instance.Output($"Gave {taken}x {itemName} to companion.");
    }

    private static int AddSlots(ItemStack[] inv, ItemStack stack)
    {
        int rem = stack.count;
        for (int i = 0; i < inv.Length && rem > 0; i++)
        {
            if (inv[i].IsEmpty()) { inv[i] = new ItemStack(stack.itemValue.Clone(), rem); rem = 0; break; }
            if (inv[i].itemValue.type == stack.itemValue.type && inv[i].CanStackWith(stack))
            {
                var space = inv[i].itemValue.ItemClass.Stacknumber.Value - inv[i].count;
                var add = Math.Min(space, rem);
                inv[i].count += add;
                rem -= add;
            }
        }
        return rem;
    }

    private void TakeFromBot(List<string> _params)
    {
        if (_params.Count < 2) { SdtdConsole.Instance.Output("Usage: scc take <item> [count]"); return; }
        var bot = GetBot();
        if (bot == null) { SdtdConsole.Instance.Output("No companion alive."); return; }
        var player = GetPlayer();
        if (player == null) return;

        var itemName = _params[1];
        var count = _params.Count > 2 && int.TryParse(_params[2], out var c) ? c : 1;
        var itemClass = ItemClass.GetItemClass(itemName);
        if (itemClass == null) { SdtdConsole.Instance.Output($"Unknown item: {itemName}"); return; }

        var inv = bot.GetInventory();
        int taken = 0;
        for (int i = 0; i < inv.Length && taken < count; i++)
        {
            if (inv[i].IsEmpty() || inv[i].itemValue.type != itemClass.Id) continue;
            var take = Math.Min(count - taken, inv[i].count);
            inv[i].count -= take;
            taken += take;
            if (inv[i].count <= 0) inv[i] = ItemStack.Empty.Clone();
        }
        if (taken == 0) { SdtdConsole.Instance.Output($"Bot has no {itemName}."); return; }
        var slots = player.inventory.GetSlots();
        var rem = taken;
        for (int i = 0; i < slots.Length && rem > 0; i++)
        {
            if (slots[i].IsEmpty()) { slots[i] = new ItemStack(new ItemValue(itemClass.Id, false), rem); rem = 0; break; }
            if (slots[i].itemValue.type == itemClass.Id && slots[i].CanStackWith(new ItemStack(new ItemValue(itemClass.Id, false), 1)))
            {
                var space = slots[i].itemValue.ItemClass.Stacknumber.Value - slots[i].count;
                var add = Math.Min(space, rem);
                slots[i].count += add;
                rem -= add;
            }
        }
        if (rem > 0) { SdtdConsole.Instance.Output($"Inventory full, {rem} left in bot."); }
        player.inventory.SetSlots(slots);
        SdtdConsole.Instance.Output($"Took {taken}x {itemName} from companion.");
    }

    private void ListInventory()
    {
        var bot = GetBot();
        if (bot == null) { SdtdConsole.Instance.Output("No companion alive."); return; }
        var inv = bot.GetInventory();
        SdtdConsole.Instance.Output("Companion inventory:");
        foreach (var stack in inv)
        {
            if (!stack.IsEmpty())
            {
                var itemClass = stack.itemValue.ItemClass;
                var name = itemClass?.GetLocalizedItemName() ?? itemClass?.Name ?? "?";
                SdtdConsole.Instance.Output($"  {name} x{stack.count}");
            }
        }
    }
}
