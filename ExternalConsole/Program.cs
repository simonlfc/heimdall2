using System.IO.Pipes;

using (var _pipe = new NamedPipeClientStream(".", "HeimdallPipe", PipeDirection.Out))
{
    _pipe.Connect();
    using StreamWriter writer = new(_pipe);
    while (true)
    {
        Console.Write("cmd: ");
        string? command = Console.ReadLine();
        if (command == null)
            continue;

        writer.WriteLine(command);
        writer.Flush();
    }
}