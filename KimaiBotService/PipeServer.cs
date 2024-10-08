﻿using System;
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

    private readonly Queue<string> commandList = [];
    private readonly SemaphoreSlim commandListSem = new(0);

    public Task Start(CancellationToken token)
    {
        return Task.Run(() => ServerLoop(token), token);
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
        if (pipeServer == null)
        {
            Console.WriteLine("Error: pipeServer is null.");
            return;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(response);
        pipeServer.Write(buffer, 0, buffer.Length);
        //pipeServer.Flush();
    }

    public async Task SendResponseAsync(string response)
    {
        if (pipeServer == null)
        {
            Console.WriteLine("Error: pipeServer is null.");
            return;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(response);
        await pipeServer.WriteAsync(buffer, 0, buffer.Length);
        await pipeServer.FlushAsync();
    }

    private async Task ServerLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // Create a new stream for each connection
            try
            {
                // Allow the current user to read/write
                var pipeSecurity = new PipeSecurity();
                var currentUser = WindowsIdentity.GetCurrent().Name;
                var authenticatedUsersSid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                pipeSecurity.AddAccessRule(new PipeAccessRule(authenticatedUsersSid, PipeAccessRights.FullControl, AccessControlType.Allow));

                pipeServer = NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, pipeSecurity);
            }
            // Catch IOException if the pipe is already in use
            catch (IOException e)
            {
                Console.WriteLine($"Erreur ! Serveur déjà en cours d'exécution : {e.Message}");
                return;
            }

            // Wait for connection
            Console.WriteLine("\nEn attente de connexion du client...\n");
            await pipeServer.WaitForConnectionAsync(token);
            Console.WriteLine("Client connecté.");

            byte[] buffer = new byte[256];
            int bytesRead;

            while (pipeServer.IsConnected && !token.IsCancellationRequested)
            {
                try
                {
                    // Read command asynchronously
                    bytesRead = await pipeServer.ReadAsync(buffer, token);
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
                    lock (commandList)
                    {
                        commandList.Enqueue(command);
                    }
                    commandListSem.Release();
                }
            }

            // Close Server after client disconnection
            pipeServer.Close();
        }
    }

    public async Task<string> GetRequestAsync(CancellationToken token)
    {
        // Check for cancellation before processing
        token.ThrowIfCancellationRequested();

        // Wait for a command to be available
        await commandListSem.WaitAsync(token); // Wait asynchronously until an item is available

        lock (commandList)
        {
            string command = commandList.Dequeue();
            return command;
        }
    }
}
