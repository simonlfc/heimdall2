public partial class Heimdall : Control
{
	public static AppId_t AppID = new(480);

	public static LoggingBase Logging = new();
	public static SteamBase Steam = new();
	public static DiscordBase Discord = new();

	public static Lobby PersistentLobby = new();

	public async override void _Ready()
	{
		var kek = GetNode<Label>("Test");
        Input.UseAccumulatedInput = false;

        AddChild(Logging);
		AddChild(Steam);
		AddChild(Discord);
		//Logging.CreatePipe();

        var connect = await Steam.Connect();
		if (!connect)
		{
			GetTree().Quit();
			return;
		}

		kek.Text = "Connected to Steam";

		Discord.Connect();

		PersistentLobby.Create(ELobbyType.k_ELobbyTypeFriendsOnly);
	}
}
