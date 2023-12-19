public partial class LoggingBase : Node
{
    private RichTextLabel _minicon;
    private const int _localPort = 10240;
    private Socket _socket;

    public override void _Ready()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.ConsoleSink()
            .CreateLogger();

        _minicon = GetTree().Root.GetNode<RichTextLabel>("Heimdall/Console");

        var localhost = IPAddress.Parse("127.0.0.1");
        var endpoint = new IPEndPoint(localhost, _localPort);
        _socket = new Socket(localhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(endpoint);
        _socket.Listen(10);

        _ = Task.Run(MonitorConnections);
    }
    private void MonitorConnections()
    {
        try
        {
            Log.Information("Listening for remote console connections");

            while (true)
            {
                var handler = _socket.Accept();
                Log.Information($"New console connection from: {((IPEndPoint)handler.RemoteEndPoint).Address}");
                _ = Task.Run(() => ReceiveMessages(handler));
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private static void ReceiveMessages(Socket handler)
    {
        try
        {
            while (true)
            {
                byte[] buffer = new byte[1024];
                var data = handler.Receive(buffer);
                if (data == 0)
                {
                    Log.Information("Client disconnected.");
                    break;
                }

                var message = Encoding.ASCII.GetString(buffer, 0, data);
                Log.Information(message);
            }
        }
        catch (SocketException se)
        {
            Log.Error($"ReceiveMessages: {se.Message}");
        }
        catch (Exception e)
        {
            Log.Error($"ReceiveMessages: {e.Message}");
        }
        finally
        {
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }

    public void SendToSocket(string message)
    {
        if (!_socket.Connected)
            return;

        try
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            _socket.Send(buffer);
        }
        catch (SocketException se)
        {
            Log.Error($"SendToSocket: {se.Message}");
        }
        catch (Exception e)
        {
            Log.Error($"SendToSocket: {e.Message}");
        }
    }

    public void Print(string text)
    {
        if (text == null)
            return;

        if (_minicon == null)
            return;

        _minicon.CallDeferred("append_text", text + "\n");

        var lineCount = _minicon.CallDeferred("get_line_count").AsInt32();
        if (lineCount >= 500)
            _minicon.CallDeferred("clear");
    }
}