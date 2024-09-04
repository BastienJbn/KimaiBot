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
        if(!logger.IsEnabled(LogLevel.Information))
        {
            logger.LogWarning("LogLevel Information is not enabled!");
        }

        logger.LogTrace("D�marrage service KimaiBot.");

        // Load user preferences
        userPrefs = UserPrefs.Load();
        logger.LogWarning("Chemin du fichier de pr�f�rences utilisateur: {UserPrefs.FilePath}", UserPrefs.FilePath);

        // Init timer
        timer.Elapsed += PeriodicTask;
        timer.AutoReset = false;
        timer.Start();

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
     * Start timer with user pref interval
     */
    private void StartTimer()
    {
        if (userPrefs.TriggerTime == null)
        {
            userPrefs.TriggerTime = new TimeSpan(10, 0, 0);
            logger.LogWarning("Heure de d�clenchement non d�finie. {heure} par d�faut.", userPrefs.TriggerTime.ToString());
        }

        // Calculate time until trigger
        TimeSpan timeUntilTrigger = userPrefs.TriggerTime.Value - DateTime.Now.TimeOfDay;

        // If time is negative or entry was already added today, add a day
        if (timeUntilTrigger.TotalMilliseconds < 0 || userPrefs.LastEntryAdded?.Day == DateTime.Now.Day)
            timeUntilTrigger = timeUntilTrigger.Add(new TimeSpan(1, 0, 0, 0));

        // Start timer if not already started
        timer.Interval = timeUntilTrigger.TotalMilliseconds;
        
        if (!timer.Enabled)
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

                logger.LogWarning("Tentative d'authentification...");

                int userId = httpClient.Authenticate(userPrefs.Username, userPrefs.Password);

                if (userId > 0)
                {
                    userPrefs.UserId = userId;
                    isAuthenticated = true;
                    logger.LogWarning("Authentification r�ussie.");

                    // Last entry not today ?
                    if (userPrefs.LastEntryAdded == null || (userPrefs.LastEntryAdded.Value.Date != DateTime.Now.Date))
                    {
                        // Add entry
                        if (httpClient.AddEntryComboRnD(userId))
                        {
                            userPrefs.LastEntryAdded = DateTime.Now;
                            // Set timer to trigger at configured time (userPrefs.TriggerTime)
                            StartTimer();
                            logger.LogWarning("Entr�e ajout�e.");
                        }
                        else
                        {
                            logger.LogError("�chec de l'ajout de l'entr�e.");

                            // Set timer trigger at 10s
                            timer.Interval = 10000;
                            timer.Start();
                        }
                    }

                    return "Successfully logged in.";
                }
                else
                {
                    isAuthenticated = false;
                    nbTry++;
                    logger.LogError("Authentification �chou�e. Essai num�ro {nbTry}", nbTry);

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

                logger.LogWarning("Utilisateur d�connect�.");
                return "Successfully logged out.";

            case "addEntry":
                if((userPrefs.Username == null || userPrefs.Password == null) || !isAuthenticated || userPrefs.UserId == null)
                {
                    logger.LogWarning("Utilisateur non authentifi�.");
                    return "User not authenticated. Use \"login\" command before.";
                }

                if(httpClient.AddEntryComboRnD(userPrefs.UserId.Value))
                {
                    userPrefs.LastEntryAdded = DateTime.Now;
                    logger.LogWarning("Entr�e ajout�e.");
                    return "Successfully added entry.";
                }
                else
                {
                    // HttpClient failed to add entry, try to re-authenticate
                    isAuthenticated = false;

                    // Set timer to 10secs
                    timer.Interval = 10000;
                    timer.Enabled = true;

                    logger.LogError("Ajout d'entr�e �chou�e. Essai de r�-Authentification.");
                    return "Failed to add entry.";
                }

            case "interval":
                int val = int.Parse(args[1]);
                timer.Interval = val;
                timer.Enabled = true;
                return $"interval set to {val}";

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
            int userId = httpClient.Authenticate(userPrefs.Username, userPrefs.Password);

            if (userId > 0)
            {
                userPrefs.UserId = userId;
                isAuthenticated = true;
                logger.LogWarning("Authentification r�ussie.");

                // Set timer to trigger at triggerTime
                StartTimer();
            }
            else
            {
                if (nbTry < MAX_TRIES)
                {
                    // Restart timer with 10secs
                    timer.Interval = 10000;
                    timer.Enabled = true;

                    nbTry++;
                    
                    logger.LogError("Authentification �chou�e. Essai num�ro {nbTry}", nbTry);
                }
                else
                {
                    // Reset try counter
                    nbTry = 0;

                    // Stop timer
                    timer.Enabled = false;

                    // TODO: windows notif

                    logger.LogError("Authentification �chou�e. Nombre maximum d'essai atteint.");
                }
                return;
            }
        }

        // Last entry not today ? UserId known ?
        if ((userPrefs.LastEntryAdded == null || (userPrefs.LastEntryAdded.Value.Date != DateTime.Now.Date)) && userPrefs.UserId != null)
        {                
            if (httpClient.AddEntryComboRnD(userPrefs.UserId.Value))
            {
                logger.LogWarning("Entr�e ajout�e. D�marrage du timer pour un d�clanchement � {heure}.", userPrefs.TriggerTime.ToString());
                userPrefs.LastEntryAdded = DateTime.Now;
                StartTimer();
            }
            else
            {
                // HttpClient failed to add entry, try to re-authenticate
                isAuthenticated = false;

                // Set timer to 10secs
                timer.Interval = 10000;
                timer.Enabled = true;

                logger.LogError("Ajout d'entr�e �chou�e. Essai de r�-Authentification.");
            }
        }
        else
        {
            logger.LogWarning("Entr�e d�j� ajout�e aujourd'hui � {heure}. D�marrage du timer pour un d�clanchement � {heure}.", 
                userPrefs.LastEntryAdded.ToString(), userPrefs.TriggerTime.ToString());

            StartTimer();
        }
    }
}
