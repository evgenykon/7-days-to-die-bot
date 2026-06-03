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
                var hit = false;

                foreach (var wg in xui.WindowGroups)
                {
                    if (wg.ID == "chatoutput" || wg.ID == "chat")
                    {
                        var co = wg.Controller as XUiC_ChatOutput;
                        if (co != null)
                        {
                            Log.Out($"[CB] Try XUiC_ChatOutput on '{wg.ID}'");
                            co.addMessage(EnumGameMessages.Chat, EChatType.Global, EChatDirection.None, "Quinn", chatMsg, "");
                            hit = true;
                        }

                        var ch = wg.Controller as XUiC_Chat;
                        if (ch != null)
                        {
                            Log.Out($"[CB] Try XUiC_Chat on '{wg.ID}'");
                            ch.TextInput_OnSubmitHandler(ch, chatMsg);
                            hit = true;
                        }

                        var children = wg.Controller.children;
                        if (children != null)
                        {
                            foreach (var child in children)
                            {
                                var childCo = child as XUiC_ChatOutput;
                                if (childCo != null)
                                {
                                    Log.Out($"[CB] Try child XUiC_ChatOutput on '{wg.ID}'");
                                    childCo.addMessage(EnumGameMessages.Chat, EChatType.Global, EChatDirection.None, "Quinn", chatMsg, "");
                                    hit = true;
                                }
                                var childCh = child as XUiC_Chat;
                                if (childCh != null)
                                {
                                    Log.Out($"[CB] Try child XUiC_Chat on '{wg.ID}'");
                                    childCh.TextInput_OnSubmitHandler(childCh, chatMsg);
                                    hit = true;
                                }
                            }
                        }
                    }
                }

                if (XUiC_Chat.messagingHandlers != null)
                {
                    foreach (var handler in XUiC_Chat.messagingHandlers)
                    {
                        if (handler != null && handler.SendMessageDelegate != null)
                        {
                            Log.Out($"[CB] Try SendMessageDelegate");
                            handler.SendMessageDelegate(EChatType.Global, localPlayer.entityId.ToString(), chatMsg);
                            hit = true;
                            break;
                        }
                    }
                }

                if (!hit)
                    Log.Out("[CB] No chat output found");
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
