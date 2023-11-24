using Google.Protobuf.WellKnownTypes;

public partial class DiscordBase : Node
{
    private const string _clientID = "1133122335528996995";
    private DiscordRpcClient _client;
    private bool _connected;

    private readonly Timestamps _timestamp = new(DateTime.UtcNow);

    private RichPresence _current = null;
    private RichPresence _next = null;

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
        _connected = true;
        Log.Information("Opened Discord pipe");
        if (_next == null)
        {
            SetRichPresence(new RichPresence
            { 
                Details = "Waiting for lobby" 
            });
        }
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

        if (_current != _next)
        {
            _current = _next;
            _client?.SetPresence(_current);
        }
    }

    public void SetRichPresence(RichPresence newPresence)
    {
        newPresence.Timestamps = _timestamp;
        newPresence.Assets = new Assets()
        {
            LargeImageKey = "heimdall"
        };

        _next = newPresence;
    }
}