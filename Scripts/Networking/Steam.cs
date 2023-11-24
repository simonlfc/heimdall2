public partial class SteamBase : Node
{
    public bool Connected;

    public async Task<bool> Connect()
    {
        if (!SteamAPI.IsSteamRunning())
            return false;

        if (SteamAPI.RestartAppIfNecessary(AppID))
        {
            GetTree().Quit();
            return false;
        }

        var connectionString = "Connecting to Steam";
        for (int i = 0; i < 10; i++)
        {
            Log.Information($"{connectionString}");
            connectionString += ".";

            Connected = SteamAPI.Init();
            if (Connected)
            {
                Log.Information("Established connection with Steam");
                SteamNetworkingUtils.InitRelayNetworkAccess();
                break;
            }

            await Task.Delay(1000);
        }

        return Connected;
    }

    public override void _Process(double delta)
    {
        if (!Connected)
            return;

        SteamAPI.RunCallbacks();
    }
}