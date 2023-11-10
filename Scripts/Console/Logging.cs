public partial class LoggingBase : Node
{
    private RichTextLabel _minicon;
    public override void _Ready()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.ConsoleSink()
            .CreateLogger();

        _minicon = GetTree().Root.GetNode<RichTextLabel>("Heimdall/Console");
    }

    public void Print(string text)
    {
        if (text == null)
            return;

        if (_minicon == null)
            return;

        _minicon.AppendText(text + "\n");

        var lineCount = _minicon.GetLineCount();
        if (lineCount >= 500)
            _minicon.Clear();
    }
}