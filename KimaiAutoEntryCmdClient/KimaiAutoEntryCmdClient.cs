
namespace KimaiAutoEntryCmdClient;
public class KimaiAutoEntryCmdClient
{
    private readonly NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "kimaiPipe", PipeDirection.InOut);

    public KimaiAutoEntryCmdClient()
    {
        pipeClient.Connect();
    }

    public string SendCommand(string command)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(command);
        pipeClient.Write(buffer, 0, buffer.Length);

        buffer = new byte[256];
        int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);

        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }
}
