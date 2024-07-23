using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Collections.Generic;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace KimaiBotService;

[SupportedOSPlatform("windows")]
class PipeServer
{
    private const string PipeName = "KimaiBotPipe";
    private NamedPipeServerStream? pipeServer;

    private readonly List<string> commandList = [];

    public Task Start(CancellationToken token)
    {
        return Task.Run(() => ServerLoop(), token);
    }
    
    public void Stop()
    {
        if (pipeServer != null)
        {
            pipeServer.Close();
        }
    }

    public void SendResponse(string response)
    {
        if(pipeServer == null)
        {
            Console.WriteLine("Error: pipeServer is null.");
            return;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(response);
        pipeServer.Write(buffer, 0, buffer.Length);
    }

    private void ServerLoop()
    {
        while (true)
        {
            // Create a new stream for each connexion
            try
            {
                // Allow the current user to read/write
                var pipeSecurity = new PipeSecurity();
                var currentUser = WindowsIdentity.GetCurrent().Name;
                pipeSecurity.AddAccessRule(new PipeAccessRule(currentUser, PipeAccessRights.FullControl, AccessControlType.Allow));

                pipeServer = NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, pipeSecurity);
            }
            // Catch IOexception if the pipe is already in use
            catch (IOException e)
            {
                Console.WriteLine($"Erreur ! Serveur déjà en cours d'exécution : {e.Message}");
                return;
            }

            // Wait for connexion
            Console.WriteLine("\nEn attente de connexion du client...\n");
            pipeServer.WaitForConnection();
            Console.WriteLine("Client connecté.");

            byte[] buffer = new byte[256];
            int bytesRead = 0;

            do
            {
                try
                {
                    // Read command
                    bytesRead = pipeServer.Read(buffer, 0, buffer.Length);
                }
                // Catch IOException that is raised if the pipe is broken or disconnected
                catch (IOException e)
                {
                    Console.WriteLine($"Erreur de communication avec le client : {e.Message}");
                    break;
                }

                if (bytesRead != 0)
                {
                    string command = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Commande reçue: {command}");

                    // Store command
                    commandList.Add(command);
                }
            }
            while (bytesRead != 0);

            // Close Server after client disconnection
            pipeServer.Disconnect();
            pipeServer.Close();
        }
    }

    public string GetRequest()
    {
        if (commandList.Count == 0)
        {
            return string.Empty;
        }

        string command = commandList[0];
        commandList.RemoveAt(0);
        return command;
    }
}
