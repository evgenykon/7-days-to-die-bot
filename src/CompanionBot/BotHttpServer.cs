using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class BotHttpServer
{
    public static BotHttpServer Instance { get; set; }

    private HttpListener _listener;
    private Thread _listenerThread;
    private readonly int _port;
    private bool _running;

    public BotHttpServer(int port = 8080)
    {
        _port = port;
        Instance = this;
    }

    public void Start()
    {
        if (_running) return;

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{_port}/");
        _listener.Prefixes.Add($"http://localhost:{_port}/");

        try
        {
            _listener.Start();
        }
        catch (Exception ex)
        {
            Log.Error($"[CB] HTTP server start error: {ex.Message}");
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_port}/");
                _listener.Start();
            }
            catch (Exception ex2)
            {
                Log.Error($"[CB] HTTP server fallback error: {ex2.Message}");
                return;
            }
        }

        _running = true;
        _listenerThread = new Thread(ListenLoop) { IsBackground = true };
        _listenerThread.Start();
        Log.Out($"[CB] HTTP server started on port {_port}");
    }

    public void Stop()
    {
        _running = false;
        try { _listener?.Stop(); } catch { }
        Log.Out("[CB] HTTP server stopped");
    }

    private void ListenLoop()
    {
        while (_running)
        {
            try
            {
                var context = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
            }
            catch (Exception ex)
            {
                if (_running) Log.Error($"[CB] HTTP listen error: {ex.Message}");
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = request.Url.AbsolutePath.ToLower();
            var method = request.HttpMethod.ToUpper();

            object result = null;

            if (path == "/chat" && method == "POST")
            {
                var body = ReadBody(request);
                var data = ParseJson(body);
                var message = GetJsonString(data, "message");
                var sender = GetJsonString(data, "sender") ?? "Quinn";

                SendChatMessage(sender, message);
                result = new { ok = true, message = $"Sent: {message}" };
            }
            else if (path == "/follow" && method == "POST")
            {
                SetFollowState(true);
                result = new { ok = true, action = "follow" };
            }
            else if (path == "/stop" && method == "POST")
            {
                SetFollowState(false);
                result = new { ok = true, action = "stop" };
            }
            else if (path == "/status" && method == "GET")
            {
                var player = GameManager.Instance.World?.GetPrimaryPlayer();
                var pos = player?.position ?? Vector3.zero;
                result = new
                {
                    ok = true,
                    playerPosition = new { x = pos.x, y = pos.y, z = pos.z },
                    botCount = GetBotCount()
                };
            }
            else if (path == "/health" && method == "GET")
            {
                result = new { ok = true, status = "alive" };
            }
            else
            {
                response.StatusCode = 404;
                result = new { error = $"Unknown endpoint: {path}" };
            }

            var json = SerializeJson(result);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            Log.Error($"[CB] HTTP request error: {ex.Message}");
            var errorJson = SerializeJson(new { error = ex.Message });
            var errorBuffer = Encoding.UTF8.GetBytes(errorJson);
            response.StatusCode = 500;
            response.ContentType = "application/json";
            response.ContentLength64 = errorBuffer.Length;
            response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
        }
        finally
        {
            try { response.OutputStream.Close(); } catch { }
        }
    }

    public static void SendChatMessage(string sender, string message)
    {
        try
        {
            var player = GameManager.Instance.World.GetPrimaryPlayer() as EntityPlayerLocal;
            if (player == null) return;

            var xui = player.PlayerUI.xui;
            foreach (var wg in xui.WindowGroups)
            {
                if (wg.ID == "chatoutput")
                {
                    var children = wg.Controller.children;
                    if (children != null)
                    {
                        foreach (var child in children)
                        {
                            var chatOutput = child as XUiC_ChatOutput;
                            if (chatOutput != null)
                            {
                                chatOutput.addMessage(EnumGameMessages.Chat, EChatType.Global, EChatDirection.None, sender, message, "");
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[CB] Chat send error: {ex.Message}");
        }
    }

    public static void SetFollowState(bool follow)
    {
        try
        {
            var world = GameManager.Instance.World;
            if (world == null) return;

            foreach (var kv in world.Entities.dict)
            {
                if (kv.Value is CompanionEntity bot)
                {
                    bot.SetFollowMode(follow);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[CB] Follow state error: {ex.Message}");
        }
    }

    private static int GetBotCount()
    {
        try
        {
            var world = GameManager.Instance.World;
            if (world == null) return 0;

            int count = 0;
            foreach (var kv in world.Entities.dict)
            {
                if (kv.Value is CompanionEntity) count++;
            }
            return count;
        }
        catch { return 0; }
    }

    private static string ReadBody(HttpListenerRequest request)
    {
        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
        {
            return reader.ReadToEnd();
        }
    }

    private static string SerializeJson(object obj)
    {
        if (obj is Dictionary<string, object> dict)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{kv.Key}\":{SerializeValue(kv.Value)}");
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
        return "{}";
    }

    private static string SerializeValue(object value)
    {
        if (value == null) return "null";
        if (value is string s) return $"\"{EscapeJson(s)}\"";
        if (value is bool b) return b ? "true" : "false";
        if (value is int || value is float || value is double) return value.ToString();
        if (value is Dictionary<string, object> dict)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{kv.Key}\":{SerializeValue(kv.Value)}");
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
        return $"\"{EscapeJson(value.ToString())}\"";
    }

    private static string EscapeJson(string s)
    {
        return s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") ?? "";
    }

    private static Dictionary<string, object> ParseJson(string json)
    {
        var result = new Dictionary<string, object>();
        if (string.IsNullOrEmpty(json)) return result;

        json = json.Trim().TrimStart('{').TrimEnd('}');
        var pairs = SplitJsonPairs(json);

        foreach (var pair in pairs)
        {
            var colonIndex = pair.IndexOf(':');
            if (colonIndex < 0) continue;

            var key = pair.Substring(0, colonIndex).Trim().Trim('"');
            var value = pair.Substring(colonIndex + 1).Trim();
            result[key] = ParseJsonValue(value);
        }
        return result;
    }

    private static List<string> SplitJsonPairs(string json)
    {
        var pairs = new List<string>();
        int depth = 0;
        int start = 0;
        bool inString = false;

        for (int i = 0; i < json.Length; i++)
        {
            if (json[i] == '"') inString = !inString;
            if (inString) continue;
            if (json[i] == '{') depth++;
            if (json[i] == '}') depth--;
            if (json[i] == ',' && depth == 0)
            {
                pairs.Add(json.Substring(start, i - start));
                start = i + 1;
            }
        }
        if (start < json.Length)
            pairs.Add(json.Substring(start));

        return pairs;
    }

    private static object ParseJsonValue(string value)
    {
        value = value.Trim();
        if (value.StartsWith("\"")) return value.Trim('"');
        if (value == "true") return true;
        if (value == "false") return false;
        if (value == "null") return null;
        if (int.TryParse(value, out int intVal)) return intVal;
        if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatVal)) return floatVal;
        return value;
    }

    private static string GetJsonString(Dictionary<string, object> dict, string key)
    {
        if (dict != null && dict.TryGetValue(key, out var val) && val is string s)
            return s;
        return null;
    }
}
