using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CompanionBotV2
{
    public class RemoteConsole
    {
        private TcpListener _listener;
        private Thread _thread;
        private volatile bool _running;

        public void Start(int port)
        {
            _running = true;
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            _thread = new Thread(ListenLoop) { IsBackground = true };
            _thread.Start();
            Log.Out($"[CB-Remote] Listening on 127.0.0.1:{port}");
        }

        public void Stop()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
        }

        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    using (var client = _listener.AcceptTcpClient())
                    using (var stream = client.GetStream())
                    {
                        client.ReceiveTimeout = 5000;
                        byte[] buf = new byte[4096];
                        int read = stream.Read(buf, 0, buf.Length);
                        if (read > 0)
                        {
                            string cmd = Encoding.UTF8.GetString(buf, 0, read).Trim();
                            Log.Out($"[CB-Remote] Exec: {cmd}");
                            SdtdConsole.Instance.ExecuteSync(cmd, null);
                            byte[] ack = Encoding.UTF8.GetBytes("OK\n");
                            stream.Write(ack, 0, ack.Length);
                        }
                    }
                }
                catch when (!_running) { }
                catch { }
            }
        }
    }
}
