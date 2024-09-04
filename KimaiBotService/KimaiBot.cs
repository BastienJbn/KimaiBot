using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using Windows.System;

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
        if (!logger.IsEnabled(LogLevel.Information))
        {
            logger.LogWarning("LogLevel Information is not enabled!");
        }

        logger.LogTrace("Démarrage service KimaiBot.");

        // Load user preferences
        userPrefs = UserPrefs.Load();

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
        if (userPrefs.AddTime == null)
        {
            userPrefs.AddTime = new TimeSpan(0, 1, 0);
            logger.LogWarning("Heure de déclenchement non définie. {heure} par défaut.", userPrefs.AddTime.ToString());
        }

        // Calculate time until trigger
        TimeSpan timeUntilTrigger = userPrefs.AddTime.Value - DateTime.Now.TimeOfDay;

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
                    logger.LogWarning("Authentification réussie.");

                    SafeAddEntry();

                    return "Successfully logged in.";
                }
                else
                {
                    isAuthenticated = false;
                    nbTry++;
                    logger.LogError("Authentification échouée. Essai numéro {nbTry}", nbTry);

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

                logger.LogWarning("Utilisateur déconnecté.");
                return "Successfully logged out.";

            case "addEntry":
                if ((userPrefs.Username == null || userPrefs.Password == null) || !isAuthenticated || userPrefs.UserId == null)
                {
                    logger.LogWarning("Utilisateur non authentifié.");
                    return "User not authenticated. Use \"login\" command before.";
                }

                if (AddEntry())
                    return "Successfully added entry.";
                else
                    return "Failed to add entry.";

            case "configure":
                TimeSpan _startTime, _duration, _addTime;

                if (!TimeSpan.TryParse(args[1], out _startTime))
                    return "Wrong time format for [StartTime].";

                if (!TimeSpan.TryParse(args[2], out _duration))
                    return "Wrong time format for [Duration].";

                if (!TimeSpan.TryParse(args[3], out _addTime))
                    return "Wrong time format for [AddTime].";

                userPrefs.StartTime = _startTime;
                userPrefs.Duration = _duration;
                userPrefs.AddTime = _addTime;

                StartTimer();

                return "Configuration saved.";

#if DEBUG
            case "interval":
                int val = int.Parse(args[1]);
                timer.Interval = val;
                timer.Enabled = true;
                return $"interval set to {val}";
#endif

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
            int userId = httpClient.Authenticate(userPrefs.Username, userPrefs.Password);

            if (userId > 0)
            {
                userPrefs.UserId = userId;
                isAuthenticated = true;
                logger.LogWarning("Authentification réussie.");

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

                    logger.LogError("Authentification échouée. Essai numéro {nbTry}", nbTry);
                }
                else
                {
                    // Reset try counter
                    nbTry = 0;

                    // Stop timer
                    timer.Enabled = false;

                    // TODO: windows notif

                    logger.LogError("Authentification échouée. Nombre maximum d'essai atteint.");
                }
                return;
            }
        }

        // Last entry not today ? UserId known ?
        if ((userPrefs.LastEntryAdded == null || (userPrefs.LastEntryAdded.Value.Date != DateTime.Now.Date)) && userPrefs.UserId != null)
        {

        }
        else
        {
            logger.LogWarning("Entrée déjà ajoutée aujourd'hui à {heure}. Démarrage du timer pour un déclanchement à {heure}.",
                userPrefs.LastEntryAdded.ToString(), userPrefs.AddTime.ToString());

            StartTimer();
        }
    }

    private bool SafeAddEntry()
    {
        // Last entry was today ?
        if ((userPrefs.LastEntryAdded != null && userPrefs.LastEntryAdded.Value.Date == DateTime.Now.Date))
            return true;  // Do nothing and return

        // Add entry
        return AddEntry();
    }

    private bool AddEntry()
    {
        if (userPrefs.StartTime == null)
        {
            userPrefs.StartTime = new(0, 0, 0);
            logger.LogWarning("StartTime non définie. {heure} par défaut.", userPrefs.StartTime.ToString());
        }

        if (userPrefs.Duration == null)
        {
            userPrefs.Duration = new(7, 24, 0);
            logger.LogWarning("Durée non définie. {heure} par défaut.", userPrefs.Duration.ToString());
        }

        // UserId not received, can't add entry
        if (userPrefs.UserId == null)
            return false;

        if (httpClient.AddEntryComboRnD(userPrefs.UserId.Value, userPrefs.StartTime.Value, userPrefs.Duration.Value))
        {
            logger.LogWarning("Entrée ajoutée. Démarrage du timer pour un déclanchement à {heure}.", userPrefs.AddTime.ToString());
            userPrefs.LastEntryAdded = DateTime.Now;
            StartTimer();
            return true;
        }
        else
        {
            // HttpClient failed to add entry, try to re-authenticate
            isAuthenticated = false;

            // Set timer to 10secs
            timer.Interval = 10000;
            timer.Enabled = true;

            logger.LogError("Ajout d'entrée échouée. Essai de ré-Authentification.");
            return false;
        }
    }
}
