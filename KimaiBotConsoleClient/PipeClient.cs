using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KimaiBotService;
class PipeClient
{
    private const string PipeName = "KimaiBotPipe";
    private NamedPipeClientStream pipeStream = new(".", "KimaiBotPipe", PipeDirection.InOut);

    public bool Connect()
    {
        try
        {
            pipeStream.ConnectAsync(2000).Wait();
        }
        catch
        {
            return false;
        }

        return true;
    }

    public void Disconnect()
    {
        pipeStream.Close();
    }

    // Send a command and expect a response in async
    public string SendReceive(string command)
    {
        string ret = "";

        if (!pipeStream.IsConnected)
        {
            return "Server not found!";
        }

        byte[] txBuffer = Encoding.UTF8.GetBytes(command);
        pipeStream.Write(txBuffer, 0, txBuffer.Length);

        try
        {
            // Wait for response
            while (true)
            {
                byte[] rxBuffer = new byte[256];
                int bytesRead = pipeStream.Read(rxBuffer, 0, rxBuffer.Length);
                if (bytesRead == 0)
                    break; // Le client s'est déconnecté

                string response = Encoding.UTF8.GetString(rxBuffer, 0, bytesRead);
                Console.WriteLine($"Response: {response}");

                ret = response;
            }
        }
        // Catch IOException that is raised if the pipe is broken or disconnected
        catch (IOException e)
        {
            ret = "!Error: " + e.Message;
        }
        finally
        {
            pipeStream.Close();
        }

        return ret;
    }
}
