public partial class GameServerBase(ushort authPort, ushort serverPort, string hostname, string map) : Node
{
    private readonly ushort _authPort = authPort;
    private readonly ushort _serverPort = serverPort;

    private readonly string _hostname = hostname;
    private readonly string _map = map;

    private bool _serverInitialised;
    private bool _serverConnected;

    private HSteamListenSocket _listenSocket;
    private HSteamNetPollGroup _pollGroup;

    private readonly ClientConnectionData[] _clientData = new ClientConnectionData[MaxPlayers];
    private readonly ClientConnectionData[] _pendingClientData = new ClientConnectionData[MaxPlayers];

    private Callback<SteamServersConnected_t> CallbackSteamServersConnected;
    private Callback<SteamServerConnectFailure_t> CallbackSteamServersConnectFailure;
    private Callback<SteamServersDisconnected_t> CallbackSteamServersDisconnected;
    private Callback<GSPolicyResponse_t> CallbackPolicyResponse;
    private Callback<ValidateAuthTicketResponse_t> CallbackGSAuthTicketResponse;
    private Callback<P2PSessionRequest_t> CallbackP2PSessionRequest;
    private Callback<P2PSessionConnectFail_t> CallbackP2PSessionConnectFail;
    private Callback<SteamNetConnectionStatusChangedCallback_t> CallbackSteamNetConnectionStatusChanged;

    public void Shutdown()
    {
        if (!_serverInitialised)
            return;

        SteamGameServerNetworkingSockets.CloseListenSocket(_listenSocket);
        SteamGameServerNetworkingSockets.DestroyPollGroup(_pollGroup);

        SteamGameServer.LogOff();
        SteamGameServer.SetAdvertiseServerActive(false);

        CallbackSteamServersConnected.Dispose();
        CallbackSteamServersConnectFailure.Dispose();
        CallbackSteamServersDisconnected.Dispose();
        CallbackPolicyResponse.Dispose();
        CallbackGSAuthTicketResponse.Dispose();
        CallbackP2PSessionRequest.Dispose();
        CallbackP2PSessionConnectFail.Dispose();
        CallbackSteamNetConnectionStatusChanged.Dispose();

        SteamGameServer.LogOff();
        GameServer.Shutdown();
        _serverInitialised = false;
    }

    public override void _ExitTree()
    {
        Shutdown();
    }

    public override void _Ready()
    {
        Log.Information($"Creating {_hostname} at :{_serverPort}");
        Name = _hostname;
        CallbackSteamServersConnected = Callback<SteamServersConnected_t>.CreateGameServer(OnSteamServersConnected);
        CallbackSteamServersConnectFailure = Callback<SteamServerConnectFailure_t>.CreateGameServer(OnSteamServersConnectFailure);
        CallbackSteamServersDisconnected = Callback<SteamServersDisconnected_t>.CreateGameServer(OnSteamServersDisconnected);
        CallbackPolicyResponse = Callback<GSPolicyResponse_t>.CreateGameServer(OnPolicyResponse);
        CallbackGSAuthTicketResponse = Callback<ValidateAuthTicketResponse_t>.CreateGameServer(OnValidateAuthTicketResponse);
        CallbackP2PSessionRequest = Callback<P2PSessionRequest_t>.CreateGameServer(OnP2PSessionRequest);
        CallbackP2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.CreateGameServer(OnP2PSessionConnectFail);
        CallbackSteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.CreateGameServer(OnSteamNetConnectionStatusChanged);

        EServerMode eMode = EServerMode.eServerModeAuthentication;
        _serverInitialised = GameServer.Init(0, _authPort, _serverPort, eMode, "r1");
        if (!_serverInitialised)
        {
            Log.Error("Failed to initialise server");
            return;
        }

        SteamGameServer.SetModDir("heimdall");
        SteamGameServer.SetProduct("Heimdall");
        SteamGameServer.SetGameDescription("Heimdall Test Builds");
        SteamGameServer.SetAdvertiseServerActive(true);
        SteamGameServer.LogOnAnonymous();

        for (int i = 0; i < MaxPlayers; i++)
        {
            _clientData[i] = new ClientConnectionData();
            _pendingClientData[i] = new ClientConnectionData();
        }

        _listenSocket = SteamGameServerNetworkingSockets.CreateListenSocketP2P(0, 0, null);
        _pollGroup = SteamGameServerNetworkingSockets.CreatePollGroup();
    }

    public override void _Process(double delta)
    {
        if (!_serverInitialised)
            return;

        GameServer.RunCallbacks();
        ReceiveNetworkData();

        if (_serverConnected)
            SendUpdatedServerDetailsToSteam();
    }

    private long _msgNumber;

    private void OnSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t result)
    {
        var handle = result.m_hConn;
        var info = result.m_info;
        var previous = result.m_eOldState;

        if (previous == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None &&
            info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
        {
            for (var i = 0; i < MaxPlayers; i++)
            {
                if (!_clientData[i].Active && _pendingClientData[i].Handle == HSteamNetConnection.Invalid)
                {
                    var res = SteamGameServerNetworkingSockets.AcceptConnection(handle);
                    if (res != EResult.k_EResultOK)
                    {
                        SteamGameServerNetworkingSockets.CloseConnection(handle, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_AppException_Generic, "Failed to accept connection", false);
                        return;
                    }

                    _pendingClientData[i].Handle = handle;
                    SteamGameServerNetworkingSockets.SetConnectionPollGroup(handle, _pollGroup);
                    Log.Information($"Accepted connection from {info.m_identityRemote.GetSteamID()}");

                    var serverInfo = new ServerSendInfo_t
                    {
                        SteamId = GameServer.GetSteamID().m_SteamID,
                        Secure = GameServer.BSecure(),
                        Hostname = _hostname,
                        Mapname = _map,
                        Gametype = "deathmatch"
                    };

                    var msg = new GameMessage()
                    {
                        ServerSendInfo = serverInfo
                    }.ToSteamMessage();

                    SteamGameServerNetworkingSockets.SendMessageToConnection(handle, msg.m_pData, (uint)msg.m_cbSize, (int)ESendType.Reliable, out _msgNumber);
                    return;
                }
            }

            Log.Information($"We have to reject {info.m_identityRemote.GetSteamID()}, there's no slots.");
            SteamGameServerNetworkingSockets.CloseConnection(handle, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_AppException_Generic, "Server full.", false);
        }
        else if ((previous == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting || previous == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
            && info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer)
        {
            for (var i = 0; i < MaxPlayers; i++)
            {
                if (!_clientData[i].Active)
                    continue;

                if (_clientData[i].SteamID == info.m_identityRemote.GetSteamID())
                {
                    Log.Information($"Disconnecting dropped user {info.m_identityRemote.GetSteamID()}");
                    SteamGameServerNetworkingSockets.CloseConnection(handle, (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_AppException_Generic, "Client disconnected", false);
                    SteamGameServer.EndAuthSession(_clientData[i].SteamID);
                    break;
                }
            }
        }
    }

    private void ReceiveNetworkData()
    {
        var messages = new IntPtr[128];
        var count = SteamGameServerNetworkingSockets.ReceiveMessagesOnPollGroup(_pollGroup, messages, 128);
        for (var i = 0; i < count; i++)
        {
            var msg = SteamNetworkingMessage_t.FromIntPtr(messages[i]);
            var sender = msg.m_identityPeer;
            var connection = msg.m_conn;

            var message = msg.ToGameMessage();

            switch (message.TypeCase)
            {
                case GameMessage.TypeOneofCase.ClientBeginAuthentication:
                    OnClientBeginAuthentication(sender.GetSteamID(), connection, message.ClientBeginAuthentication.Token.ToByteArray(), (int)message.ClientBeginAuthentication.TokenLength);
                    break;
            }

            SteamNetworkingMessage_t.Release(messages[i]);
        }
    }

    private void OnClientBeginAuthentication(CSteamID steamID, HSteamNetConnection connection, byte[] token, int tokenLength)
    {
        for (var i = 0; i < MaxPlayers; i++)
        {
            if (_clientData[i].Handle == connection)
                return;
        }

        var pending = 0;
        for (var i = 0; i < MaxPlayers; i++)
        {
            if (_pendingClientData[i].Active || _clientData[i].Active)
                pending++;
        }

        if (pending >= MaxPlayers)
            SteamGameServerNetworkingSockets.CloseConnection(connection, 1004, "Server full.", false);

        for (var i = 0; i < MaxPlayers; i++)
        {
            if (_pendingClientData[i].Active)
                continue;

            _pendingClientData[i].LastTick = Time.GetTicksMsec();
            var result = SteamGameServer.BeginAuthSession(token, tokenLength, steamID);
            if (result != EBeginAuthSessionResult.k_EBeginAuthSessionResultOK)
            {
                SteamGameServerNetworkingSockets.CloseConnection(connection, 1003, "Authentication failed.", false);
                break;
            }

            _pendingClientData[i].SteamID = steamID;
            _pendingClientData[i].Active = true;
            _pendingClientData[i].Handle = connection;
            break;
        }

    }

    private void OnSteamServersConnected(SteamServersConnected_t result)
    {
        Log.Information($"Server connected to Steam");
        _serverConnected = true;
        SendUpdatedServerDetailsToSteam();
    }
    private void OnSteamServersConnectFailure(SteamServerConnectFailure_t result)
    {
        _serverConnected = false;
        Log.Information($"Server failed to connect to Steam");
    }

    private void OnSteamServersDisconnected(SteamServersDisconnected_t result)
    {
        _serverConnected = false;
        Log.Information($"Server lost connection to Steam");
    }

    private void OnPolicyResponse(GSPolicyResponse_t result)
    {
        Log.Information($"VAC {(SteamGameServer.BSecure() ? "Enabled" : "Disabled")}");
        Log.Information($"Server registered with SteamID {SteamGameServer.GetSteamID()}");
    }

    private void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t result)
    {
        if (result.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseOK)
        {
            Log.Information($"Accepted authticket for {result.m_SteamID}");
            for (var i = 0; i < MaxPlayers; i++)
            {
                if (!_pendingClientData[i].Active)
                    continue;

                else if (_pendingClientData[i].SteamID == result.m_SteamID)
                {
                    OnAuthCompleted(true, i);
                    return;
                }
            }
        }
        else
        {
            Log.Information($"Rejected authticket for {result.m_SteamID}");
            for (var i = 0; i < MaxPlayers; i++)
            {
                if (!_pendingClientData[i].Active)
                    continue;
                else if (_pendingClientData[i].SteamID == result.m_SteamID)
                {
                    OnAuthCompleted(false, i);
                    return;
                }
            }
        }
    }

    private void OnAuthCompleted(bool success, int pendingIndex)
    {
        if (!_pendingClientData[pendingIndex].Active)
            return;

        if (!success)
        {
            SteamGameServer.EndAuthSession(_pendingClientData[pendingIndex].SteamID);

            var msg = new GameMessage()
            {
                ServerFailAuthentication = new ServerFailAuthentication_t()
            }.ToSteamMessage();

            SteamGameServerNetworkingSockets.SendMessageToConnection(_pendingClientData[pendingIndex].Handle, msg.m_pData, (uint)msg.m_cbSize, (int)ESendType.Reliable, out _msgNumber);
            _pendingClientData[pendingIndex] = new ClientConnectionData();
            return;
        }

        for (var i = 0; i < MaxPlayers; i++)
        {
            if (!_clientData[i].Active)
            {
                _clientData[i] = _pendingClientData[i];
                _pendingClientData[pendingIndex] = new ClientConnectionData();
                _clientData[i].LastTick = Time.GetTicksMsec();

                var msg = new GameMessage()
                {
                    ServerPassAuthentication = new ServerPassAuthentication_t()
                }.ToSteamMessage();

                SteamGameServerNetworkingSockets.SendMessageToConnection(_clientData[i].Handle, msg.m_pData, (uint)msg.m_cbSize, (int)ESendType.Reliable, out _msgNumber);
                break;
            }
        }
    }

    private void OnP2PSessionRequest(P2PSessionRequest_t result)
    {
        Log.Information($"OnP2PSesssionRequest from {result.m_steamIDRemote}");
        SteamGameServerNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote);
    }
    private void OnP2PSessionConnectFail(P2PSessionConnectFail_t result)
    {
        Log.Information($"OnP2PSessionConnectFail from {result.m_steamIDRemote}");
    }

    private void SendUpdatedServerDetailsToSteam()
    {
        SteamGameServer.SetMaxPlayerCount(MaxPlayers);
        SteamGameServer.SetPasswordProtected(false);
        SteamGameServer.SetServerName(_hostname);
        SteamGameServer.SetMapName(_map);

        for (var i = 0; i < MaxPlayers; i++)
        {
            if (_clientData[i].Active)
                SteamGameServer.BUpdateUserData(_clientData[i].SteamID, "dont know yet", 0);
        }
    }
}

public class ClientConnectionData
{
    public bool Active = false;
    public ulong LastTick = 0;
    public CSteamID SteamID = new();
    public HSteamNetConnection Handle = HSteamNetConnection.Invalid;
}

public enum ESendType
{
    Unreliable = 0,
    NoNagle = 1,
    NoDelay = 4,
    UnreliableNoDelay = Unreliable | NoDelay | NoNagle,
    Reliable = 8,
    ReliableNoNagle = Reliable | NoNagle,
    UseCurrentThread = 16,
    AutoRestartBrokenSession = 32
}

public static class Messaging
{
    public static SteamNetworkingMessage_t ToSteamMessage(this GameMessage message)
    {
        var buffer = message.ToByteArray();

        SteamNetworkingMessage_t steamMessage = new()
        {
            m_cbSize = buffer.Length
        };
        steamMessage.m_pData = Marshal.AllocHGlobal(steamMessage.m_cbSize);
        steamMessage.m_nChannel = 0;
        Marshal.Copy(buffer, 0, steamMessage.m_pData, steamMessage.m_cbSize);
        return steamMessage;
    }

    public static GameMessage ToGameMessage(this SteamNetworkingMessage_t message)
    {
        var buffer = new byte[message.m_cbSize];
        Marshal.Copy(message.m_pData, buffer, 0, message.m_cbSize);
        return GameMessage.Parser.ParseFrom(buffer);
    }
}