using System.Net.Sockets;
using System.Net;
using System.Text;

internal class Program
{
    private const int _localPort = 10240;
    private const string _localIP = "127.0.0.1";
    private static Socket _socket;

    private static void Main()
    {
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
                    string userInput = Console.ReadLine();
                    if (userInput.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                        break;

                    SendMessage(userInput);
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