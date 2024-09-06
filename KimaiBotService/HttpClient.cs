using System;
using System.Net.Http;
using System.Collections.Generic;

namespace KimaiBotService;

class KimaiHttpClient
{
    private readonly HttpClient client;
    private readonly HttpClientHandler handler;

    private readonly string loginHttpAddress = "http://frapp01/kimai/index.php?a=checklogin";
    private readonly string processorHttpAddress = "http://frapp01/kimai/extensions/ki_timesheets/processor.php";

    public KimaiHttpClient()
    {
        handler = new HttpClientHandler
        {
            CookieContainer = new System.Net.CookieContainer()
        };

        client = new HttpClient(handler)
        {
            BaseAddress = new Uri(processorHttpAddress)
        };
    }

    public int Authentify(string username, string password)
    {
        var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("name", username),
            new KeyValuePair<string, string>("password", password)
        ]);

        try
        {
            // Send the POST request
            HttpResponseMessage response = client.PostAsync(loginHttpAddress, content).Result;

            // Search userId var in the received php Doc
            string responseBody = response.Content.ReadAsStringAsync().Result;
            int index = responseBody.IndexOf("userID");
            if (index == -1)
                return -1; // "userID" not found, something went wrong


            /** Get userID value and return it **/

            // Find the '=' character after "userID"
            int equalsIndex = responseBody.IndexOf('=', index);
            if (equalsIndex == -1)
                return -1; // '=' not found after "userID"

            // Start extracting the numeric value
            int startIndex = equalsIndex + 2; // Skip the "= " characters
            int endIndex = responseBody.IndexOfAny([';', ' '], startIndex);
            if (endIndex == -1)
                endIndex = responseBody.Length; // If there's no delimiter, assume it's at the end of the string
            if( (endIndex - startIndex) > 20)
                return -1; // Too much character between the equal and the end character

            string userIdString = responseBody[startIndex..endIndex].Trim();

            // Attempt to parse the extracted substring as an integer
            if (int.TryParse(userIdString, out int userId))
                return userId;
            else
                return -1; // Parsing failed
        }
        catch {
            return -1;
        }
    }

    public void Logout()
    {
        handler.CookieContainer.SetCookies(new Uri(loginHttpAddress), "");
    }

    public bool AddEntryComboRnD(int userID, TimeSpan startTime, TimeSpan duration)
    {
        var payload = new List<KeyValuePair<string, string>>
        {
            new("axAction",    "add_edit_timeSheetEntry"),
            new("projectID",   "18"),
            new("activityID",  "52"),
            new("description", ""),
            new("start_day",   DateTime.Now.ToString("dd.MM.yyyy")),
            new("end_day",     DateTime.Now.ToString("dd.MM.yyyy")),
            new("start_time",  startTime.ToString(@"hh\:mm\:ss")),
            new("end_time",    (startTime + duration).ToString(@"hh\:mm\:ss")),
            new("duration",    duration.ToString(@"hh\:mm\:ss")),
            new("comment",     ""),
            new("commentType", "0"),
            new("userID[]",    userID.ToString()),
            new("statusID",    "1"),
            new("billable",    "0")
        };

        var content = new FormUrlEncodedContent(payload);
        try
        {
            var response = client.PostAsync(processorHttpAddress, content).Result;
            return response.IsSuccessStatusCode;
        }
        catch
        { 
            return false;
        }
    }
}
