using HeimdallShared;
using System.IO.Pipes;

internal class Program
{
    private static ConsolePipe _consolePipe;

    private static async Task Main()
    {
        Console.WriteLine("$ Heimdall external console");
        _consolePipe = new(new NamedPipeClientStream(".", "HeimdallPipe", PipeDirection.InOut));
        Console.WriteLine("$ Attempting connection");
        await _consolePipe.Connect();
        Console.WriteLine("$ Established connection");

        _consolePipe.OnMessageReceived += (text) =>
        {
            Console.WriteLine($"{text}");
        };

        while (true)
        {
            var message = Console.ReadLine();
            _consolePipe.Send(message);
        }
    }
}