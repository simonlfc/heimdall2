using System.Net.Sockets;
using System.Net;
using System.Text;
using Terminal.Gui;

internal class Program
{
    private const int _localPort = 10240;
    private const string _localIP = "127.0.0.1";
    private static Socket _socket;

    private static void Main()
    {
        Application.Init();
        Application.IsMouseDisabled = true;

        var win = new Window("kekkedy")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Border = new Border()
            {
                BorderStyle = BorderStyle.None
            }
        };
        Application.Top.Add(win);

        var consolePrefix = new Label("heimdall2>")
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            AutoSize = true,
            Height = 1,
        };
        Application.Top.Add(consolePrefix);

        var consoleInput = new TextField("haha")
        {
            X = 11,
            Y = Pos.AnchorEnd(1),
            AutoSize = true,
            Height = 1,
        };

        Application.Top.Add(consoleInput);
        //Application.Run();
        consoleInput.SetFocus();

        while (true)
        {
            try
            {
                var localhost = IPAddress.Parse(_localIP);
                var endpoint = new IPEndPoint(localhost, _localPort);

                _socket = new Socket(localhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(endpoint);

                Console.WriteLine("Connected to console");
                _ = Task.Run(ReceiveMessages);

                while (true)
                {
                    var input = Console.ReadLine();
                    if (input.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                        break;

                    SendMessage(input);
                }

                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                Console.WriteLine("Disconnected from game console.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
                Console.WriteLine($"Reconnecting in 5 seconds...");
                Thread.Sleep(5000);
            }
        }
    }

    private static void SendMessage(string message)
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
            Console.WriteLine($"SendMessage: {se.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"SendMessage: {e.Message}");
        }
    }

    private static void ReceiveMessages()
    {
        try
        {
            while (true)
            {
                byte[] buffer = new byte[1024];
                var data = _socket.Receive(buffer);

                if (data == 0)
                {
                    Console.WriteLine("Lost connection to game.");
                    break;
                }

                var message = Encoding.ASCII.GetString(buffer, 0, data);
                Console.WriteLine(message);
            }
        }
        catch (SocketException se)
        {
            Console.WriteLine($"ReceiveMessages: {se.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"ReceiveMessages: {e.Message}");
        }
        finally
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
    }
}