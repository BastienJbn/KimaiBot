using System.IO.Pipes;
using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace KimaiBotService;

[SupportedOSPlatform("windows")]
class PipeServer
{
    private const string PipeName = "KimaiBotPipe";
    private readonly NamedPipeServerStream pipeServer = new(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

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
        // Allow the current user to read/write
        var pipeSecurity = new PipeSecurity();
        var currentUser = WindowsIdentity.GetCurrent().Name;
        pipeSecurity.AddAccessRule(new PipeAccessRule(currentUser, PipeAccessRights.FullControl, AccessControlType.Allow));

        // Crée un serveur de pipe qui attend la connexion d'un client
        Console.WriteLine("En attente de connexion du client...\n");
        pipeServer.WaitForConnection();
        Console.WriteLine("Client connecté.");

        while (true)
        {
            try
            {
                // Lire la commande du client
                byte[] buffer = new byte[256];
                int bytesRead = pipeServer.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break; // Le client s'est déconnecté

                string command = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Commande reçue: {command}");

                // Stocker la commande dans la liste des commandes
                commandList.Add(command);
            }
            // Catch IOException that is raised if the pipe is broken or disconnected
            catch (IOException e)
            {
                Console.WriteLine($"Erreur de communication avec le client : {e.Message}");
            }
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
