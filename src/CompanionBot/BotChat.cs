using System;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleCmdBotSay : ConsoleCmdAbstract
{
    public override string[] getCommands() => new[] { "botsay", "bs" };

    public override string getDescription() => "Make companion bot say something in chat. botsay <message>";

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

        try
        {
            var localPlayer = player as EntityPlayerLocal;
            if (localPlayer != null)
            {
                var xui = localPlayer.PlayerUI.xui;
                XUiC_ChatOutput chatOutput = null;

                foreach (var wg in xui.WindowGroups)
                {
                    chatOutput = wg.Controller.GetChildByType<XUiC_ChatOutput>();
                    if (chatOutput != null) break;
                }

                if (chatOutput != null)
                {
                    chatOutput.addMessage(EnumGameMessages.Chat, EChatType.Global, EChatDirection.None, "Quinn", chatMsg, "");
                    return;
                }

                Log.Out("[CB] XUiC_ChatOutput not found in any window group");
            }

            SdtdConsole.Instance.Output(chatMsg);
        }
        catch (Exception ex)
        {
            Log.Error($"[CB] Chat send error: {ex.Message}");
            SdtdConsole.Instance.Output(chatMsg);
        }
    }
}
