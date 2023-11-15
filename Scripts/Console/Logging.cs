using Serilog.Core;

public partial class LoggingBase : Node
{
    private RichTextLabel _minicon;
    private NamedPipeServerStream _pipe;

    public override void _Ready()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.ConsoleSink()
            .CreateLogger();

        _minicon = GetTree().Root.GetNode<RichTextLabel>("Heimdall/Console");
    }

    public async void CreatePipe()
    {
        _pipe = new NamedPipeServerStream("HeimdallPipe", PipeDirection.InOut);
        await _pipe.WaitForConnectionAsync();
        Log.Information("External client connected to console pipe");
        _ = Task.Run(ReceiveFromPipe);
    }

    private async void ReceiveFromPipe()
    {
        using var reader = new StreamReader(_pipe);
        while (true)
        {
            if (!_pipe.IsConnected || reader == null)
                continue;

            string buffer;
            while ((buffer = await reader.ReadLineAsync()) != null)
                Log.Information($"Received message {buffer}");

        }
    }

    public void Print(string text)
    {
        if (text == null)
            return;

        if (_minicon == null)
            return;

        _minicon.CallDeferred("append_text", text + "\n");

        var lineCount = (int)_minicon.CallDeferred("get_line_count");
        if (lineCount >= 500)
            _minicon.CallDeferred("clear");
    }
}