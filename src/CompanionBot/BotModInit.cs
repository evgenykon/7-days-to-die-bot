using System;
using System.Net;
using System.Text;
using System.Threading;

public class BotModInit : IModApi
{
    private const string BotServerUrl = "http://localhost:9091/chat";

    public void InitMod(Mod _modInstance)
    {
        Log.Out("[CB] InitMod called via IModApi");

        var server = new BotHttpServer(9090);
        server.Start();

        ModEvents.ChatMessage.RegisterHandler(OnChatMessage);

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
        var _message = data.Message;
        var _mainName = data.MainName;

        if (string.IsNullOrEmpty(_message)) return ModEvents.EModEventResult.Continue;
        if (_mainName == "Quinn") return ModEvents.EModEventResult.Continue;

        var msg = _message;
        var name = _mainName ?? "Player";

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                var json = $"{{\"sender\":\"{EscapeJson(name)}\",\"message\":\"{EscapeJson(msg)}\"}}";
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    client.UploadString(BotServerUrl, "POST", json);
                }
            }
            catch (Exception ex) { Log.Error($"[CB] Forward error: {ex.Message}"); }
        });

        return ModEvents.EModEventResult.Continue;
    }

    private static string EscapeJson(string s)
    {
        return s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") ?? "";
    }
}
