using System.Threading;

public class BotModInit : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        Log.Out("[CB] InitMod called via IModApi");

        var server = new BotHttpServer(9090);
        server.Start();

        new Thread(() =>
        {
            Log.Out("[CB] Kill thread waiting for world...");
            while (GameManager.Instance?.World == null)
                Thread.Sleep(1000);
            Log.Out("[CB] World found, killing companions...");
            ConsoleCmdSpawnCompanion.KillAll();
            Log.Out("[CB] Kill all done");
        }) { IsBackground = true }.Start();
    }
}
