using System.Net;

public partial class Heimdall : Control
{
	public static bool Dedicated;
    public static int MaxPlayers = 18;

    private static CSteamID AutoJoinID;
	public static AppId_t AppID = new(480);

	public static LoggingBase Logging = new();
	public static SteamBase Steam = new();
	public static DiscordBase Discord = new();

	public async override void _Ready()
    {
        ParseCommandLine();
		Input.UseAccumulatedInput = false;

		AddChild(Logging);
		AddChild(Steam);
		AddChild(Discord);

		var connect = await Steam.Connect();
		if (!connect)
		{
			GetTree().Quit();
			return;
		}

		Discord.Connect();

        if (AutoJoinID.m_SteamID != 0)
        {
            Log.Information($"Auto-connecting to {AutoJoinID.m_SteamID}");
            // connect to lobby
        }

        if (Dedicated)
        {

        }
    }

    private static void ParseCommandLine()
    {
        var argv = OS.GetCmdlineArgs();
        if (argv.Length > 0)
        {
            for (var i = 0; i < argv.Length; i++)
            {
                if (argv[i].Contains("+dedicated"))
                {
                    Dedicated = true;
                    return;
                }

                if (argv[i] != "+connect_lobby")
                    continue;

                AutoJoinID = (CSteamID)ulong.Parse(argv[i + 1]);
                break;
            }
        }
    }
}
