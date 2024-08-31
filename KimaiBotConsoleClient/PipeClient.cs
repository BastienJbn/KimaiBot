using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

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
            // Wait for response with a timeout
            byte[] rxBuffer = new byte[8 * 1000];  // 1000 time 64bits
            var cts = new CancellationTokenSource(5000); // 5 seconds timeout
            Task<int> readTask = pipeStream.ReadAsync(rxBuffer, 0, rxBuffer.Length, cts.Token);

            if (readTask.Wait(5000)) // Wait for the task to complete or timeout
            {
                int bytesRead = readTask.Result;
                ret = Encoding.UTF8.GetString(rxBuffer, 0, bytesRead);
            }
            else
                ret = "Timeout: No response received within 5 seconds.";
        }
        // Catch IOException that is raised if the pipe is broken or disconnected
        catch (IOException e)
        {
            ret = "!Error: " + e.Message;
        }
        // Catch OperationCanceledException if the read operation is canceled
        catch (OperationCanceledException)
        {
            ret = "Timeout: No response received within 5 seconds.";
        }

        return ret;
    }
}
