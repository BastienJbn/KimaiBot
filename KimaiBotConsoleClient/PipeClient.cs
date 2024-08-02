using System.IO;
using System.IO.Pipes;
using System.Text;

namespace KimaiBotCmdLine;
class PipeClient
{
    private NamedPipeClientStream? pipeStream;

    public bool Connect()
    {
        try
        {
            pipeStream = new(".", "KimaiBotPipe", PipeDirection.InOut);
            pipeStream.ConnectAsync(5000).Wait();
        }
        catch
        {
            return false;
        }

        return true;
    }

    public void Disconnect()
    {
        if(pipeStream != null && pipeStream.IsConnected)
        {
            pipeStream.Close();
        }
    }

    // Send a command and expect a response in async
    public string SendReceive(string command)
    {
        string ret;

        if (pipeStream == null)
            return "Pipe not connected!";

        if (!pipeStream.IsConnected)
            return "Server disconnected!";

        byte[] txBuffer = Encoding.UTF8.GetBytes(command);
        pipeStream.Write(txBuffer, 0, txBuffer.Length);
        pipeStream.Flush();

        try
        {
            // Wait for response
            byte[] rxBuffer = new byte[256];
            int bytesRead = pipeStream.Read(rxBuffer, 0, rxBuffer.Length);

            ret = Encoding.UTF8.GetString(rxBuffer, 0, bytesRead);
        }
        // Catch IOException that is raised if the pipe is broken or disconnected
        catch (IOException e)
        {
            ret = "!Error: " + e.Message;
        }

        return ret;
    }
}
