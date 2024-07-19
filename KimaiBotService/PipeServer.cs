using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KimaiBotService;

class PipeServer
{
    private const string PipeName = "KimaiBotPipe";
    private NamedPipeServerStream? pipeServer;

    private List<string> commandList = [];

    public void StartServer()
    {
        Task.Run(() => ServerLoop());
    }
    
    public string? popCommand()
    {
        if (commandList.Count == 0)
        {
            return null;
        }
        else
        {
            string command = commandList.First();
            commandList.RemoveAt(0);
            return command;
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
            // Crée un serveur de pipe qui attend la connexion d'un client
            using (pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Console.WriteLine("En attente de connexion du client...");
                pipeServer.WaitForConnection();
                Console.WriteLine("Client connecté.");

                try
                {
                    // Boucle de traitement des commandes du client
                    while (true)
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
                }
                // Catch IOException that is raised if the pipe is broken or disconnected
                catch (IOException e)
                {
                    Console.WriteLine($"Erreur de communication avec le client : {e.Message}");
                }
                // Ensure the pipe is disconnected when the client disconnects
                finally
                {
                    if (pipeServer.IsConnected)
                    {
                        pipeServer.Disconnect();
                    }
                }
                Console.WriteLine("Client déconnecté.");
            }
        }
    }
}
