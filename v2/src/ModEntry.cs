using System.Reflection;

namespace CompanionBotV2
{
    public class ModEntry : IModApi
    {
        private RemoteConsole _remote;

        public void InitMod(Mod _modInstance)
        {
            Log.Out("[CompanionBot v2] Mod loaded");
            _remote = new RemoteConsole();
            _remote.Start(9876);
        }
    }
}
