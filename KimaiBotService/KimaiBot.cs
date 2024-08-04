using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;

namespace KimaiBotService;

[SupportedOSPlatform("windows")]
public sealed class KimaiBot(ILogger<KimaiBot> logger)
{
    private readonly System.Timers.Timer timer = new();
    private readonly KimaiHttpClient httpClient = new();
    private readonly PipeServer server = new();
    private readonly ILogger<KimaiBot> logger = logger;

    public async Task Start(CancellationToken token)
    {
        logger.LogInformation("Starting KimaiBot service.");
        await Task.WhenAll(CommandHandler(token), server.Start(token));
    }

    private async Task CommandHandler(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            string request = await server.GetRequestAsync(token);

            if (string.IsNullOrEmpty(request))
            {
                continue;
            }

            string response = HandleCommand(request);
            server.SendResponse(response);
        }
    }

    private string HandleCommand(string request)
    {
        string[] args = request.Split(' ');

        if (args.Length == 0)
        {
            Console.WriteLine("Received empty command.");
            return "No command provided.";
        }

        switch (args[0])
        {
            case "login":
                string username = args[1];
                string password = args[2];

                logger.LogInformation("Tentative d'authentification...");

                if (httpClient.Authenticate(username, password))
                {
                    // Add entry immediately
                    httpClient.AddEntryComboRnD();

                    // Start timer. Trigger each day at 10am
                    timer.Elapsed += (sender, e) => httpClient.AddEntryComboRnD();
                    timer.Interval = 1000 * 60 * 60 * 24;
                    timer.Start();

                    logger.LogInformation("Authentification réussie.");
                    return "Successfully logged in.";
                }
                else
                {
                    logger.LogError("Authentification échouée.");
                    return "Failed to log in.";
                }

            case "logout":
                httpClient.Logout();
                timer.Stop();
                logger.LogInformation("Utilisateur déconnecté.");
                return "Successfully logged out.";

            case "addEntry":
                if(httpClient.AddEntryComboRnD())
                {
                    logger.LogInformation("Entrée ajoutée.");
                    return "Successfully added entry.";
                }
                else
                {
                    logger.LogError("Échec de l'ajout de l'entrée.");
                    return "Failed to add entry.";
                }

            default:
                logger.LogWarning("Commande reçue invalide.");
                return "Invalid command.";
        }
    }
}