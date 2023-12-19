public class LobbyManagerBase
{
    protected CallResult<LobbyCreated_t> CallbackLobbyCreated;
    protected CallResult<LobbyEnter_t> CallbackLobbyEntered;
    protected Callback<LobbyDataUpdate_t> CallbackLobbyDataUpdate;
    protected Callback<LobbyChatUpdate_t> CallbackLobbyChatUpdate;

    public ELobbyType Privacy;
    public CSteamID ID;
    public CSteamID Owner;

    public int MemberCount => SteamMatchmaking.GetNumLobbyMembers(ID);
    public int MemberLimit => SteamMatchmaking.GetLobbyMemberLimit(ID);

    public LobbyManagerBase()
    {
        CallbackLobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        CallbackLobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
        CallbackLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
        CallbackLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }

    public void Create(ELobbyType privacy)
    {
        Privacy = privacy;
        CallbackLobbyCreated.Set(SteamMatchmaking.CreateLobby(Privacy, 8));
    }

    public void Join(CSteamID id)
    {
        if (id == ID)
            return;

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
    private void OnLobbyDataUpdate(LobbyDataUpdate_t param)
    {
        Log.Information("OnLobbyDataUpdate");
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t param)
    {
        Log.Information("OnLobbyChatUpdate");

        for (var i = 0; i < MemberCount; i++)
        {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(ID, i);
            Log.Information($"- Member: {SteamFriends.GetFriendPersonaName((CSteamID)member.m_SteamID)}");
        }
    }

    private void OnLobbyEntered(LobbyEnter_t result, bool failure)
    {
        Log.Information("OnLobbyEntered");

        if (failure)
            return;

        SteamMatchmaking.LeaveLobby(ID);
        ID = (CSteamID)result.m_ulSteamIDLobby;
        Owner = SteamMatchmaking.GetLobbyOwner(ID);
        Log.Information($"Joined lobby {ID}");

        SetRichPresence();
    }

    private void OnLobbyCreated(LobbyCreated_t result, bool failure)
    {
        Log.Information("OnLobbyCreated");

        if (failure || result.m_eResult != EResult.k_EResultOK)
            return;

        ID = (CSteamID)result.m_ulSteamIDLobby;
        Owner = SteamUser.GetSteamID();
        SteamMatchmaking.SetLobbyData(ID, "id", ID.ToString());
        SteamMatchmaking.SetLobbyJoinable(ID, true);
        Log.Information($"Created lobby with ID {ID}");

        SetRichPresence();
    }
}