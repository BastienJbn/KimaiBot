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

    private string? username = null;
    private string? password = null;
    private bool isAuthenticated = false;

    DateTime? triggerTime = null;
    TimeSpan timerInterval = new(0, 1, 0); // 1 minute by default

    /**
     * Start the KimaiBot service.
     * @param token The cancellation token.
     */
    public async Task Start(CancellationToken token)
    {
        logger.LogInformation("Starting KimaiBot service.");

        // Init timer
        timer.Elapsed += PeriodicTask;
        timer.AutoReset = true;

        await Task.WhenAll(CommandHandler(token), server.Start(token));
    }

    /**
     * Handle commands received from the client.
     * @param token The cancellation token.
     */
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

    /**
     * Set the time at which the timer should trigger.
     * @param time The time at which the timer should trigger.
     */
    private void StartTimer() {
        if (triggerTime == null)
        {
            logger.LogWarning("Heure de déclenchement non définie. Tous les jours à 10h par défaut.");
            triggerTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 0, 0);
        }

        // Calculate time until trigger
        TimeSpan timeUntilTrigger = triggerTime.Value - DateTime.Now;

        // If time is negative, add a day
        if(timeUntilTrigger.TotalMilliseconds < 0)
        {
            timeUntilTrigger = timeUntilTrigger.Add(new TimeSpan(1, 0, 0, 0));
        }

        // Start timer
        timer.Interval = timeUntilTrigger.TotalMilliseconds;
        timer.Start();
    }

    private void StartTimerByInterval(TimeSpan interval)
    {
        timer.Interval = interval.TotalMilliseconds;
        timer.Start();
    }

    /**
     * Handle the command received from the client.
     * @param request The command received from the client.
     * @return The response to the client.
     */
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
                username = args[1];
                password = args[2];

                logger.LogInformation("Tentative d'authentification...");

                if(httpClient.Authenticate(username, password))
                {
                    isAuthenticated = true;
                    logger.LogInformation("Authentification réussie.");

                    // Set timer to trigger at configured time (triggerTime)
                    StartTimer();
                    return "Successfully logged in.";
                }
                else
                {
                    isAuthenticated = false;
                    logger.LogError("Authentification échouée.");

                    // Set timer to trigger every minute
                    StartTimerByInterval(new TimeSpan(0, 1, 0));

                    return "Failed to log in.";
                }

            case "logout":
                username = null;
                password = null;
                isAuthenticated = false;

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

    /**
     * Periodic task that runs every time the timer elapses.
     * This task is responsible for adding an entry to Kimai.
     */
    private void PeriodicTask(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if(username != null && password != null)
        {
            if(!isAuthenticated)
            {
                // Try to authenticate
                if(httpClient.Authenticate(username, password))
                {
                    isAuthenticated = true;
                    logger.LogInformation("Authentification réussie.");

                    // Add entry
                    if(httpClient.AddEntryComboRnD())
                    {
                        logger.LogInformation("Entrée ajoutée.");
                    }
                    else
                    {
                        logger.LogError("Échec de l'ajout de l'entrée.");
                    }

                    // Set timer to trigger at triggerTime
                    StartTimer();
                }
                else
                {
                    logger.LogError("Authentification échouée.");
                }
            }
            else
            {
                // Add entry
                if(httpClient.AddEntryComboRnD())
                {
                    logger.LogInformation("Entrée ajoutée.");
                }
                else
                {
                    logger.LogError("Échec de l'ajout de l'entrée.");

                    // HttpClient failed to add entry, try to re-authenticate
                    isAuthenticated = false;

                    // Set timer to trigger every minute
                    StartTimerByInterval(new TimeSpan(0, 1, 0));
                }
            }
        }
    }
}