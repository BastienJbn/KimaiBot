using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace KimaiBotService;

[SupportedOSPlatform("windows")]
public sealed class KimaiBot(ILogger<KimaiBot> logger)
{
    private readonly System.Timers.Timer timer = new();
    private readonly KimaiHttpClient httpClient = new();
    private readonly PipeServer server = new();
    private readonly ILogger<KimaiBot> logger = logger;

    DateTime? triggerTime = null;
    TimeSpan timerInterval = new(0, 1, 0); // 1 minute by default

    private bool isAuthenticated = false;
    private UserPrefs userPrefs = new();

    /**
     * Start the KimaiBot service.
     * @param token The cancellation token.
     */
    public async Task Start(CancellationToken token)
    {
        logger.LogInformation("Starting KimaiBot service.");

        // Load user preferences
        userPrefs = UserPrefs.Load();

        // Init timer
        timer.Elapsed += PeriodicTask;
        timer.AutoReset = true;

        // Execute periodic task once to add an entry immediately if possible
        PeriodicTask(null, null);

        // Start command handler and server
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
            logger.LogWarning("Heure de d�clenchement non d�finie. Tous les jours � 10h par d�faut.");
            triggerTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 0, 0);
        }

        // Calculate time until trigger
        TimeSpan timeUntilTrigger = triggerTime.Value - DateTime.Now;

        // If time is negative, add a day
        if(timeUntilTrigger.TotalMilliseconds < 0)
            timeUntilTrigger = timeUntilTrigger.Add(new TimeSpan(1, 0, 0, 0));

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
                userPrefs.Username = args[1];
                userPrefs.Password = args[2];

                logger.LogInformation("Tentative d'authentification...");

                if(httpClient.Authenticate(userPrefs.Username, userPrefs.Password))
                {
                    isAuthenticated = true;
                    logger.LogInformation("Authentification r�ussie.");

                    // Set timer to trigger at configured time (triggerTime)
                    StartTimer();
                    return "Successfully logged in.";
                }
                else
                {
                    isAuthenticated = false;
                    logger.LogError("Authentification �chou�e.");

                    // Set timer to trigger every minute
                    StartTimerByInterval(new TimeSpan(0, 1, 0));

                    return "Failed to log in.";
                }

            case "logout":
                // Clear user credentials
                userPrefs.Username = null;
                userPrefs.Password = null;
                isAuthenticated = false;

                // Log out of Kimai
                httpClient.Logout();

                // Stop timer
                timer.Stop();

                logger.LogInformation("Utilisateur d�connect�.");
                return "Successfully logged out.";

            case "addEntry":
                if(httpClient.AddEntryComboRnD())
                {
                    logger.LogInformation("Entr�e ajout�e.");
                    return "Successfully added entry.";
                }
                else
                {
                    logger.LogError("�chec de l'ajout de l'entr�e.");
                    return "Failed to add entry.";
                }

            default:
                logger.LogWarning("Commande re�ue invalide.");
                return "Invalid command.";
        }
    }

    /**
     * Periodic task that runs every time the timer elapses.
     * This task is responsible for adding an entry to Kimai.
     */
    private void PeriodicTask(object? sender, System.Timers.ElapsedEventArgs? e)
    {
        if(userPrefs.Username != null && userPrefs.Password != null)
        {
            if(!isAuthenticated)
            {
                // Try to authenticate
                if(httpClient.Authenticate(userPrefs.Username, userPrefs.Password))
                {
                    isAuthenticated = true;
                    logger.LogInformation("Authentification r�ussie.");

                    // Add entry
                    if(httpClient.AddEntryComboRnD())
                    {
                        logger.LogInformation("Entr�e ajout�e.");
                    }
                    else
                    {
                        logger.LogError("�chec de l'ajout de l'entr�e.");
                    }

                    // Set timer to trigger at triggerTime
                    StartTimer();
                }
                else
                {
                    logger.LogError("Authentification �chou�e.");
                }
            }
            else
            {
                // Add entry
                if(httpClient.AddEntryComboRnD())
                {
                    logger.LogInformation("Entr�e ajout�e.");
                }
                else
                {
                    logger.LogError("�chec de l'ajout de l'entr�e.");

                    // HttpClient failed to add entry, try to re-authenticate
                    isAuthenticated = false;

                    // Set timer to trigger every minute
                    StartTimerByInterval(new TimeSpan(0, 1, 0));
                }
            }
        }
    }
}