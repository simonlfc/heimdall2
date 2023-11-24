public class ConsoleSink(IFormatProvider formatProvider) : ILogEventSink
{
    private readonly IFormatProvider _formatProvider = formatProvider;

    public void Emit(LogEvent logEvent)
    {
        var color = logEvent.Level switch
        {
            LogEventLevel.Verbose => Colors.Pink.ToHtml(),
            LogEventLevel.Debug => Colors.Wheat.ToHtml(),
            LogEventLevel.Information => Colors.White.ToHtml(),
            LogEventLevel.Warning => Colors.Gold.ToHtml(),
            LogEventLevel.Error => Colors.IndianRed.ToHtml(),
            LogEventLevel.Fatal => Colors.DarkRed.ToHtml(),
            _ => Colors.LightGray.ToHtml()
        };

        var level = logEvent.Level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => Colors.LightGray.ToHtml()
        };

        var message = logEvent.RenderMessage(_formatProvider);
        GD.PrintRich($"[{DateTime.Now:HH:mm} {level}] {message}");
        Logging.Print($"[color=darkgray][{DateTime.Now:HH:mm}][/color] [color=#{color}]{message}[/color]");
        Logging.Pipe?.Send(message);
    }
}

public static class ConsoleSinkExtensions
{
    public static LoggerConfiguration ConsoleSink(
        this LoggerSinkConfiguration loggerConfiguration,
        IFormatProvider formatProvider = null)
    {
        return loggerConfiguration.Sink(new ConsoleSink(formatProvider));
    }
}