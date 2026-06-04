using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class BotModInit : IModApi
{
    private const string BotServerEventUrl = "http://localhost:9091/event";

    public void InitMod(Mod _modInstance)
    {
        Log.Out("[CB] InitMod called via IModApi");

        var server = new BotHttpServer(9090);
        server.Start();

        ModEvents.ChatMessage.RegisterHandler(OnChatMessage);
        ModEvents.GameMessage.RegisterHandler(OnGameMessage);
        ModEvents.EntityKilled.RegisterHandler(OnEntityKilled);
        ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
        ModEvents.PlayerJoinedGame.RegisterHandler(OnPlayerJoinedGame);
        ModEvents.PlayerDisconnected.RegisterHandler(OnPlayerDisconnected);

        new Thread(() =>
        {
            Log.Out("[CB] Kill thread waiting for world...");
            while (GameManager.Instance?.World == null)
                Thread.Sleep(1000);
            Log.Out("[CB] World found, killing companions...");
            ConsoleCmdSpawnCompanion.KillAll();
            Log.Out("[CB] Kill all done");
        }) { IsBackground = true }.Start();
    }

    private ModEvents.EModEventResult OnChatMessage(ref ModEvents.SChatMessageData data)
    {
        var msg = data.Message;
        var name = data.MainName;

        if (string.IsNullOrEmpty(msg)) return ModEvents.EModEventResult.Continue;
        if (name == "Quinn") return ModEvents.EModEventResult.Continue;

        ForwardEvent("chat", $"\"sender\":\"{EscapeJson(name)}\",\"message\":\"{EscapeJson(msg)}\"");
        return ModEvents.EModEventResult.Continue;
    }

    private ModEvents.EModEventResult OnGameMessage(ref ModEvents.SGameMessageData data)
    {
        ForwardEvent("game_message", $"\"type\":{(int)data.MessageType},\"mainName\":\"{EscapeJson(data.MainName)}\",\"secondaryName\":\"{EscapeJson(data.SecondaryName)}\"");
        return ModEvents.EModEventResult.Continue;
    }

    private void OnEntityKilled(ref ModEvents.SEntityKilledData data)
    {
        var killed = data.KilledEntitiy?.EntityClass?.entityClassName ?? "unknown";
        var killer = data.KillingEntity?.EntityClass?.entityClassName ?? "unknown";
        ForwardEvent("entity_killed", $"\"killed\":\"{EscapeJson(killed)}\",\"killer\":\"{EscapeJson(killer)}\"");
    }

    private void OnPlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData data)
    {
        ForwardEvent("player_spawned", $"\"isLocal\":{data.IsLocalPlayer.ToString().ToLower()},\"entityId\":{data.EntityId}");
    }

    private void OnPlayerJoinedGame(ref ModEvents.SPlayerJoinedGameData data)
    {
        ForwardEvent("player_joined", "\"placeholder\":true");
    }

    private void OnPlayerDisconnected(ref ModEvents.SPlayerDisconnectedData data)
    {
        ForwardEvent("player_disconnected", "\"placeholder\":true");
    }

    private void ForwardEvent(string eventType, string fields)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                var json = $"{{\"type\":\"{EscapeJson(eventType)}\",{fields}}}";
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    client.UploadString(BotServerEventUrl, "POST", json);
                }
            }
            catch (Exception ex) { Log.Error($"[CB] Forward error ({eventType}): {ex.Message}"); }
        });
    }

    private static string EscapeJson(string s)
    {
        return s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") ?? "";
    }
}
