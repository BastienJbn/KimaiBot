using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KimaiBotService
{
    class KimaiServer
    {
        private readonly NamedPipeServerStream pipeServer =
            new("KimaiAutoEntryPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        KimaiServer()
        {
            pipeServer.BeginWaitForConnection(WaitForConnectionCallback, null);
        }

        public async Task HandlePipeServer()
        {
            await pipeServer.WaitForConnectionAsync();

            using var reader = new StreamReader(pipeServer);
            using var writer = new StreamWriter(pipeServer);

            writer.AutoFlush = true;

            string? request = await reader.ReadLineAsync();

            string response = HandleCommand(request);

            await writer.WriteLineAsync(response);

            pipeServer.Disconnect();
        }
    }
}
