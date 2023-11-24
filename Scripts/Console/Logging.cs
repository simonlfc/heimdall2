public partial class LoggingBase : Node
{
    private RichTextLabel _minicon;
    public ConsolePipe Pipe;

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
        Pipe = new ConsolePipe(new NamedPipeServerStream("HeimdallPipe", PipeDirection.InOut));
        await Pipe.Connect();

        Pipe.OnMessageReceived += (text) =>
        {
            Log.Information($"We got a message {text}");
        };
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