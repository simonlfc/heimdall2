public partial class Heimdall : Control
{
	public static AppId_t AppID = new(480);

	public static LoggingBase Logging = new();
	public static SteamBase Steam = new();
	public static DiscordBase Discord = new();

	public async override void _Ready()
	{
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
	}
}
