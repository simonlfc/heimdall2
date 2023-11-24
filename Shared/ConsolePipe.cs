using System.Collections.Concurrent;
using System.IO.Pipes;
namespace HeimdallShared;

public class ConsolePipe(dynamic pipe)
{
    private dynamic _pipe = pipe;
    private const int PipeRate = 100;
    private readonly ConcurrentQueue<string> _queue = new();

    public delegate void MessageReceived(string message);
    public event MessageReceived OnMessageReceived;

    private Task _incoming;
    private Task _outgoing;

    public async Task Connect()
    {
        if (_pipe is NamedPipeClientStream)
            await _pipe.ConnectAsync();
        else if (_pipe is NamedPipeServerStream)
            await _pipe.WaitForConnectionAsync();
        else
            throw new Exception("Constructor requires NamedPipeClientStream or NamedPipeServerStream");

        if (_pipe.CanRead)
            _incoming = Task.Run(PipeProcessIncoming);
        if (_pipe.CanWrite)
            _outgoing = Task.Run(PipeProcessOutgoing);
    }

    public void Send(string text) => _queue.Enqueue(text);

    private void Reconnect()
    {
        if (_pipe is NamedPipeClientStream)
        {
            _pipe.Dispose();
        }
        else if (_pipe is NamedPipeServerStream)
        {
            _pipe.Disconnect();
            _pipe.Dispose();
        }

        _ = Connect();
    }

    private async void PipeProcessIncoming()
    {
        using var reader = new StreamReader(_pipe);
        while (true)
        {
            if (!_pipe.IsConnected || reader == null)
                continue;

            var buffer = await reader.ReadLineAsync();
            if (buffer == null)
            {
                Reconnect();
                continue;
            }

            OnMessageReceived.Invoke(buffer);
            await Task.Delay(PipeRate);
        }
    }

    private async void PipeProcessOutgoing()
    {
        using var writer = new StreamWriter(_pipe);
        while (true)
        {
            if (!_pipe.IsConnected || writer == null)
                continue;

            if (_queue.TryDequeue(out var message))
            {
                try
                {
                    await writer.WriteLineAsync(message);
                    await writer.FlushAsync();
                }
                catch (Exception)
                {

                }
            }

            await Task.Delay(PipeRate);
        }
    }
}