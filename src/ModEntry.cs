using System.Reflection;

namespace CompanionBotV2
{
    public class ModEntry : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            Log.Out("[CompanionBot v2] Mod loaded");
        }
    }
}
