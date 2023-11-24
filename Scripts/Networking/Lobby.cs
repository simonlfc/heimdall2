public class Lobby
{
    protected CallResult<LobbyCreated_t> CallbackLobbyCreated;
    protected CallResult<LobbyEnter_t> CallbackLobbyEntered;

    public ELobbyType Privacy;
    public CSteamID ID;
    public CSteamID Owner;

    public int MemberCount => SteamMatchmaking.GetNumLobbyMembers(ID);
    public int MemberLimit => SteamMatchmaking.GetLobbyMemberLimit(ID);

    public Lobby()
    {
        CallbackLobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        CallbackLobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void Create(ELobbyType privacy)
    {
        Privacy = privacy;
        CallbackLobbyCreated.Set(SteamMatchmaking.CreateLobby(Privacy, 8));
    }

    public void Join(CSteamID id)
    {
        CallbackLobbyEntered.Set(SteamMatchmaking.JoinLobby(id));
    }

    private void SetRichPresence()
    {
        SteamFriends.SetRichPresence("connect", $"+connect_lobby {ID}");
        Discord.SetRichPresence(new RichPresence()
        {
            Details = "In lobby",
            State = $"{MemberCount} of {MemberLimit} players"
        });
    }

    private void OnLobbyEntered(LobbyEnter_t result, bool failure)
    {
        if (failure)
            return;

        SteamMatchmaking.LeaveLobby(ID);
        ID = new CSteamID(result.m_ulSteamIDLobby);
        Owner = SteamMatchmaking.GetLobbyOwner(ID);
        Log.Information($"Joined lobby {ID}");
        SetRichPresence();
    }

    private void OnLobbyCreated(LobbyCreated_t result, bool failure)
    {
        if (failure || result.m_eResult != EResult.k_EResultOK)
            return;

        ID = new CSteamID(result.m_ulSteamIDLobby);
        Owner = SteamUser.GetSteamID();
        SteamMatchmaking.SetLobbyData(ID, "id", ID.ToString());
        SteamMatchmaking.SetLobbyJoinable(ID, true);
        Log.Information($"Created lobby with ID {ID}");
        SetRichPresence();
    }
}