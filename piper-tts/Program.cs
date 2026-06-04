using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class PiperServer
{
    [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, int fdwSound);
    private const int SND_FILENAME = 0x00020000;
    private const int SND_ASYNC = 0x0001;
    private const int SND_NODEFAULT = 0x0002;
    private const int SND_NOSTOP = 0x0010;

    private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "piper-server.log");
    private static readonly string PiperExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "piper.exe");
    private static readonly string VoiceModel = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice.onnx");

    static void Main(string[] args)
    {
        var port = 9092;
        if (args.Length > 0) int.TryParse(args[0], out port);

        Log($"PiperServer starting on port {port}");
        Log($"Piper: {PiperExe}");
        Log($"Model: {VoiceModel}");

        if (!File.Exists(PiperExe))
        {
            Log("ERROR: piper.exe not found");
            Console.WriteLine($"ERROR: piper.exe not found. Place it in {AppDomain.CurrentDomain.BaseDirectory}");
            Environment.Exit(1);
        }
        if (!File.Exists(VoiceModel))
        {
            Log("ERROR: voice.onnx not found");
            Console.WriteLine($"ERROR: voice model not found. Place voice.onnx in {AppDomain.CurrentDomain.BaseDirectory}");
            Environment.Exit(1);
        }

        var listener = new TcpListener(System.Net.IPAddress.Any, port);
        listener.Start();
        Log($"Listening on port {port} (TcpListener)");
        Console.WriteLine($"PiperServer running on port {port}");

        while (true)
        {
            try
            {
                var client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
            catch (Exception ex)
            {
                Log($"Accept error: {ex.Message}");
                Thread.Sleep(1000);
            }
        }
    }

    static void HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buf = new byte[8192];
                var read = stream.Read(buf, 0, buf.Length);
                if (read == 0) return;

                var raw = Encoding.UTF8.GetString(buf, 0, read);
                var lines = raw.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length == 0) return;

                var requestLine = lines[0].Split(' ');
                if (requestLine.Length < 2) return;

                var method = requestLine[0];
                var path = requestLine[1];

                if (method == "GET" && path.TrimEnd('/') == "/ping")
                {
                    RespondHttp(stream, 200, "{\"ok\":true}");
                    return;
                }

                if (method != "POST" || !path.TrimEnd('/').Equals("/speak"))
                {
                    RespondHttp(stream, 404, "{\"error\":\"use POST /speak\"}");
                    return;
                }

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var i = 1;
                for (; i < lines.Length; i++)
                {
                    if (string.IsNullOrEmpty(lines[i])) break;
                    var colon = lines[i].IndexOf(':');
                    if (colon > 0)
                    {
                        var key = lines[i].Substring(0, colon).Trim();
                        var val = lines[i].Substring(colon + 1).Trim();
                        headers[key] = val;
                    }
                }

                var bodyStart = raw.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                string body;
                if (bodyStart >= 0)
                {
                    var bodyRaw = raw.Substring(bodyStart + 4);
                    if (headers.TryGetValue("Content-Length", out var clStr) && int.TryParse(clStr, out var cl) && cl > bodyRaw.Length)
                    {
                        var remaining = cl - bodyRaw.Length;
                        var extra = new byte[remaining];
                        var offset = 0;
                        while (offset < remaining)
                        {
                            var r = stream.Read(extra, offset, remaining - offset);
                            if (r <= 0) break;
                            offset += r;
                        }
                        bodyRaw += Encoding.UTF8.GetString(extra, 0, offset);
                    }
                    body = bodyRaw;
                }
                else
                {
                    body = "";
                }

                var text = ExtractJsonString(body, "text");
                if (string.IsNullOrEmpty(text))
                {
                    RespondHttp(stream, 400, "{\"error\":\"text is required\"}");
                    return;
                }

                Log($"Speak: \"{text}\"");

                var lengthScale = ExtractJsonFloat(body, "length_scale") ?? 1.0f;
                var noiseScale = ExtractJsonFloat(body, "noise_scale") ?? 0.667f;
                var noiseW = ExtractJsonFloat(body, "noise_w") ?? 0.8f;

                var wavPath = Path.Combine(Path.GetTempPath(), $"piper_{Guid.NewGuid():N}.wav");
                var args = $"--model \"{VoiceModel}\" --output-file \"{wavPath}\" --length_scale {lengthScale} --noise_scale {noiseScale} --noise_w {noiseW} --espeak_data \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "espeak-ng-data")}\"";

                try
                {
                    var piper = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = PiperExe,
                            Arguments = args,
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    piper.Start();
                    var utf8Bytes = Encoding.UTF8.GetBytes(text);
                    piper.StandardInput.BaseStream.Write(utf8Bytes, 0, utf8Bytes.Length);
                    piper.StandardInput.Close();
                    var piperExited = piper.WaitForExit(30000);
                    if (!piperExited)
                    {
                        piper.Kill();
                        Log("Piper timed out");
                        RespondHttp(stream, 500, "{\"error\":\"piper timed out\"}");
                        return;
                    }

                    if (File.Exists(wavPath) && new FileInfo(wavPath).Length > 0)
                    {
                        RespondHttp(stream, 200, $"{{\"ok\":true,\"wav\":\"{EscapeJson(wavPath)}\"}}");
                        var wav = wavPath;
                        ThreadPool.QueueUserWorkItem(_ => PlayWav(wav));
                    }
                    else
                    {
                        Log("WAV file missing or empty");
                        RespondHttp(stream, 200, "{\"ok\":true}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Piper error: {ex.Message}");
                    try { if (File.Exists(wavPath)) File.Delete(wavPath); } catch { }
                    RespondHttp(stream, 500, $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Handle error: {ex.Message}");
        }
    }

    static void PlayWav(string wav)
    {
        try
        {
            if (!File.Exists(wav)) return;
            if (PlaySound(wav, IntPtr.Zero, SND_FILENAME | SND_ASYNC | SND_NODEFAULT))
            {
                Log("Audio playing");
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(5000);
                    try { if (File.Exists(wav)) File.Delete(wav); } catch { }
                });
            }
            else
            {
                Log("Audio device busy or unavailable");
                try { if (File.Exists(wav)) File.Delete(wav); } catch { }
            }
        }
        catch (Exception ex)
        {
            Log($"Playback error: {ex.Message}");
            try { if (File.Exists(wav)) File.Delete(wav); } catch { }
        }
    }

    static void RespondHttp(NetworkStream stream, int code, string body)
    {
        var statusText = code == 200 ? "OK" : code == 400 ? "Bad Request" : "Internal Server Error";
        var buf = Encoding.UTF8.GetBytes(body);
        var sb = new StringBuilder();
        sb.Append($"HTTP/1.1 {code} {statusText}\r\n");
        sb.Append("Content-Type: application/json\r\n");
        sb.Append($"Content-Length: {buf.Length}\r\n");
        sb.Append("Connection: close\r\n");
        sb.Append("\r\n");
        var header = Encoding.ASCII.GetBytes(sb.ToString());
        stream.Write(header, 0, header.Length);
        stream.Write(buf, 0, buf.Length);
    }

    static string ExtractJsonString(string json, string key)
    {
        var search = $"\"{key}\":\"";
        var idx = json.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx += search.Length;
        var end = json.IndexOf('"', idx);
        if (end < 0) return null;
        return json.Substring(idx, end - idx);
    }

    static float? ExtractJsonFloat(string json, string key)
    {
        var search = $"\"{key}\":";
        var idx = json.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx += search.Length;
        var end = idx;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-'))
            end++;
        if (end == idx) return null;
        if (float.TryParse(json.Substring(idx, end - idx), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return val;
        return null;
    }

    static string EscapeJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

    static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        Console.WriteLine(line);
        try { File.AppendAllText(LogFile, line + Environment.NewLine); } catch { }
    }
}
