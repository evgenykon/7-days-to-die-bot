using System;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleCmdBotHttp : ConsoleCmdAbstract
{
    public override string[] getCommands() => new[] { "bothttp" };

    public override string getDescription() => "Start/stop the bot HTTP server. Usage: bothttp [port]";

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        int port = 8080;
        if (_params.Count > 0 && int.TryParse(_params[0], out int p))
            port = p;

        if (BotHttpServer.Instance != null)
        {
            BotHttpServer.Instance.Stop();
            BotHttpServer.Instance = null;
            SdtdConsole.Instance.Output("HTTP server stopped.");
            return;
        }

        var server = new BotHttpServer(port);
        server.Start();
        SdtdConsole.Instance.Output($"HTTP server started on port {port}");
    }
}
