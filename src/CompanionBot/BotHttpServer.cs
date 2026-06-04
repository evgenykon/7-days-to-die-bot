using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class BotHttpServer
{
    public static BotHttpServer Instance { get; set; }

    private TcpListener _listener;
    private Thread _acceptThread;
    private readonly int _port;
    private bool _running;

    private const int DefaultPort = 9090;

    public BotHttpServer(int port = 8080)
    {
        _port = port;
        Instance = this;
    }

    public void Start()
    {
        if (_running) return;

        try
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _running = true;
            _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            _acceptThread.Start();
            Log.Out($"[CB] TCP server started on 0.0.0.0:{_port}");
        }
        catch (Exception ex)
        {
            Log.Error($"[CB] TCP server start error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void Stop()
    {
        _running = false;
        try { _listener?.Stop(); } catch { }
        Log.Out("[CB] TCP server stopped");
    }

    private void AcceptLoop()
    {
        while (_running)
        {
            try
            {
                var client = _listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
            catch (Exception ex)
            {
                if (_running) Log.Error($"[CB] Accept error: {ex.Message}");
            }
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var headerBuf = new byte[8192];
                int totalRead = 0;
                int headerEnd = -1;
                int contentLength = 0;

                while (headerEnd < 0)
                {
                    int r = stream.Read(headerBuf, totalRead, headerBuf.Length - totalRead);
                    if (r <= 0) return;
                    totalRead += r;
                    string s = Encoding.UTF8.GetString(headerBuf, 0, totalRead);
                    headerEnd = s.IndexOf("\r\n\r\n");
                    if (headerEnd >= 0)
                    {
                        foreach (var line in s.Split('\n'))
                        {
                            var l = line.Trim().ToLower();
                            if (l.StartsWith("content-length:"))
                                int.TryParse(l.Substring(15).Trim(), out contentLength);
                        }
                    }
                }

                int bodyRead = totalRead - headerEnd - 4;
                while (bodyRead < contentLength)
                {
                    int r = stream.Read(headerBuf, totalRead, headerBuf.Length - totalRead);
                    if (r <= 0) break;
                    totalRead += r;
                    bodyRead = totalRead - headerEnd - 4;
                }

                var raw = Encoding.UTF8.GetString(headerBuf, 0, totalRead);
                var lines = raw.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length < 1) return;

                var requestLine = lines[0].Split(' ');
                if (requestLine.Length < 2) return;

                var method = requestLine[0].ToUpper();
                var path = requestLine[1].ToLower().TrimEnd('/');
                if (path == "") path = "/";

                string body = headerEnd >= 0 ? raw.Substring(headerEnd + 4) : "";

                int statusCode = 200;
                string responseBody = HandleRequest(method, path, body, out statusCode);
                byte[] responseBytes = Encoding.UTF8.GetBytes(
                    $"HTTP/1.1 {statusCode} {(statusCode == 200 ? "OK" : "Error")}\r\n" +
                    "Content-Type: application/json\r\n" +
                    $"Content-Length: {responseBody.Length}\r\n" +
                    "Connection: close\r\n" +
                    "\r\n" +
                    responseBody);

                stream.Write(responseBytes, 0, responseBytes.Length);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[CB] Client error: {ex.Message}");
        }
    }

    private string HandleRequest(string method, string path, string body, out int statusCode)
    {
        try
        {
            if (path == "/chat" && method == "POST")
            {
                statusCode = 200;
                var data = ParseJson(body);
                var message = GetJsonString(data, "message");
                var sender = GetJsonString(data, "sender") ?? "Quinn";
                SendChatMessage(sender, message);
                return SerializeJson(Dict("ok", true, "message", $"Sent: {message}"));
            }
            else if (path == "/follow" && method == "POST")
            {
                statusCode = 200;
                SetFollowState(true);
                return SerializeJson(Dict("ok", true, "action", "follow"));
            }
            else if (path == "/stop" && method == "POST")
            {
                statusCode = 200;
                SetFollowState(false);
                return SerializeJson(Dict("ok", true, "action", "stop"));
            }
            else if (path == "/status" && method == "GET")
            {
                statusCode = 200;
                var player = GameManager.Instance.World?.GetPrimaryPlayer();
                var pos = player?.position ?? Vector3.zero;
                return SerializeJson(Dict("ok", true,
                    "playerX", pos.x, "playerY", pos.y, "playerZ", pos.z,
                    "botCount", GetBotCount()));
            }
            else if (path == "/health" && method == "GET")
            {
                statusCode = 200;
                return SerializeJson(Dict("ok", true, "status", "alive"));
            }
            else
            {
                statusCode = 404;
                return SerializeJson(Dict("error", $"Unknown endpoint: {path}"));
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[CB] Request error: {ex.Message}");
            statusCode = 500;
            return SerializeJson(Dict("error", ex.Message));
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
                                chatOutput.addMessage(EnumGameMessages.Chat, EChatType.Global, EChatDirection.None, message, sender, "");
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
                    bot.SetFollowMode(follow);
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
                if (kv.Value is CompanionEntity) count++;
            return count;
        }
        catch { return 0; }
    }

    private static Dictionary<string, object> Dict(params object[] args)
    {
        var d = new Dictionary<string, object>();
        for (int i = 0; i < args.Length - 1; i += 2)
            d[args[i].ToString()] = args[i + 1];
        return d;
    }

    private static string GetJsonString(Dictionary<string, object> dict, string key)
    {
        if (dict != null && dict.TryGetValue(key, out var val) && val is string s)
            return s;
        return null;
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
        int depth = 0, start = 0;
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
        if (start < json.Length) pairs.Add(json.Substring(start));
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
}
