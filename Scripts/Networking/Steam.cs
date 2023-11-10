public partial class SteamBase : Node
{
    private bool _connected;

    public async Task<bool> Connect()
    {
        if (!SteamAPI.IsSteamRunning())
            return false;

        if (SteamAPI.RestartAppIfNecessary(Heimdall.AppID))
        {
            GetTree().Quit();
            return false;
        }

        // attempt to initialise Steam API
        var connectionString = "Connecting to Steam";
        for (int i = 0; i < 10; i++)
        {
            Log.Information($"{connectionString}");
            connectionString += ".";

            _connected = SteamAPI.Init();
            if (_connected)
            {
                Log.Information($"Established connection with Steam");
                SteamNetworkingUtils.InitRelayNetworkAccess();
                break;
            }

            await Task.Delay(1000);
        }

        return _connected;
    }

    public override void _Process(double delta)
    {
        if (!_connected)
            return;

        SteamAPI.RunCallbacks();
    }
}