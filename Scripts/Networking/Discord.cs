using DiscordRPC.Message;

public partial class DiscordBase : Node
{
    private const string _clientID = "1133122335528996995";
    private DiscordRpcClient _client;
    private bool _connected;

    public override void _Ready()
    {
        _client = new(_clientID);
        _client.OnConnectionFailed += (_, _) => _ExitTree();
        _client.OnError += (_, _) => _ExitTree();
        _client.OnReady += OnConnected;
    }

    public void Connect()
    {
        _client.Initialize();
    }

    private void OnConnected(object sender, ReadyMessage args)
    {
        Log.Information("Opened Discord pipe");
        UpdatePresence(new RichPresence()
        {
            Details = "Dev"
        });
    }

    public override void _ExitTree()
    {
        _client?.Dispose();
    }

    public override void _Process(double delta)
    {
        if (!_connected)
            return;

        _client?.Invoke();
    }

    public void UpdatePresence(RichPresence presence)
    {
        presence.Timestamps = new Timestamps()
        {
            Start = DateTime.UtcNow
        };
        presence.Assets = new Assets()
        {
            LargeImageKey = "heimdall"
        };

        _client?.SetPresence(presence);
    }
}