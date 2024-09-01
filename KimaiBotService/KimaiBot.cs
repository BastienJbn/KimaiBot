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

    private DateTime? triggerTime = null;
    private TimeSpan timerInterval = new(0, 1, 0); // 1 minute by default
    private int nbTry = 0;
    private const int MAX_TRIES = 5;

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
            //await server.SendResponseAsync(response);
            server.SendResponse(response);
        }
    }

    /**
     * Set the time at which the timer should trigger.
     * @param time The time at which the timer should trigger.
     */
    private void SetUserInterval() {
        if (triggerTime == null)
        {
            logger.LogWarning("Heure de déclenchement non définie. Tous les jours à 10h par défaut.");
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
                // Save credential
                userPrefs.Username = args[1];
                userPrefs.Password = args[2];

                logger.LogInformation("Tentative d'authentification...");

                if(httpClient.Authenticate(userPrefs.Username, userPrefs.Password))
                {
                    isAuthenticated = true;
                    logger.LogInformation("Authentification réussie.");


                    // Add entry
                    if (httpClient.AddEntryComboRnD())
                    {
                        logger.LogInformation("Entrée ajoutée.");
                    }
                    else
                    {
                        logger.LogError("Échec de l'ajout de l'entrée.");
                    }

                    // Set timer to trigger at configured time (triggerTime)
                    SetUserInterval();
                    return "Successfully logged in.";
                }
                else
                {
                    isAuthenticated = false;
                    logger.LogError("Authentification échouée.");

                    // Set timer to trigger every 10secs
                    timer.Interval = 10000;
                    timer.Start();

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
                timer.Enabled = false;

                logger.LogInformation("Utilisateur déconnecté.");
                return "Successfully logged out.";

            case "addEntry":
                if(userPrefs.Username == null || userPrefs.Password == null)
                {
                    logger.LogWarning("Utilisateur non authentifié.");
                    return "User not authenticated. Use \"login\" command before.";
                }
                if (httpClient.Authenticate(userPrefs.Username, userPrefs.Password))
                {
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
                }
                else
                {
                    logger.LogError("Échec de l'authentification.");
                    return "Failed to authenticate.";
                }

            case "interval":
                int val = int.Parse(args[1]);
                timer.Interval = val;
                timer.Enabled = true;
                return $"interval set to {val}";

            default:
                logger.LogWarning("Commande reçue invalide.");
                return "Invalid command.";
        }
    }

    /**
     * Periodic task that runs every time the timer elapses.
     * This task is responsible for adding an entry to Kimai.
     */
    private void PeriodicTask(object? sender, System.Timers.ElapsedEventArgs? e)
    {
        // Check that user has provided credentials
        if (userPrefs.Username == null || userPrefs.Password == null)
        {
            // Stop timer
            timer.Enabled = false;
            return;
        }

        if (!isAuthenticated)
        {
            // Try to authenticate
            if(httpClient.Authenticate(userPrefs.Username, userPrefs.Password))
            {
                isAuthenticated = true;
                logger.LogInformation("Authentification réussie.");

                // Set timer to trigger at triggerTime
                SetUserInterval();
            }
            else
            {
                logger.LogError("Authentification échouée.");

                if (nbTry < MAX_TRIES)
                {
                    if (nbTry == 0)
                    {
                        timer.Interval = 10000;  // Set timer to trigger every 
                        timer.Enabled = true;
                    }
                    nbTry++;
                }
                else
                {
                    // Reset try counter
                    nbTry = 0;

                    // Stop timer
                    timer.Enabled = false;

                    // TODO: windows notif
                }
                return;
            }
        }

        // Last entry not today ?
        if (userPrefs.LastEntryAdded == null || (userPrefs.LastEntryAdded.Value.Date != DateTime.Now.Date))
        {
            if (httpClient.AddEntryComboRnD())
            {
                logger.LogInformation("Entrée ajoutée.");
                userPrefs.LastEntryAdded = DateTime.Now;
                SetUserInterval();
            }
            else
            {
                // HttpClient failed to add entry, try to re-authenticate
                isAuthenticated = false;

                // Set timer to 10secs
                timer.Interval = 10000;
                timer.Enabled = true;
            }
        }
        else
        {
            SetUserInterval();
        }
    }
}