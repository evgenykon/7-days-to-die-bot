using System;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleCmdBotSay : ConsoleCmdAbstract
{
    public override string[] getCommands() => new[] { "botsay", "bs" };

    public override string getDescription() => "Make companion bot say something. botsay <message>";

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        if (_params.Count == 0)
        {
            SdtdConsole.Instance.Output("Usage: botsay <message>");
            return;
        }

        var player = GameManager.Instance.World.GetPrimaryPlayer();
        if (player == null)
        {
            SdtdConsole.Instance.Output("No player found.");
            return;
        }

        var message = string.Join(" ", _params);
        var chatMsg = $"[Quinn] {message}";
        SdtdConsole.Instance.Output(chatMsg);
        Log.Out($"[CB] {chatMsg}");
    }
}
