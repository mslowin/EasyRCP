using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasyRCP.Services;

/// <summary>
/// A api for interacting with the RCP Online via api requests.
/// </summary>
public class RcpApiClient
{
    private readonly HttpClient _client;

    private readonly string _email;

    private readonly string _password;

    /// <summary>
    /// Gets a value indicating whether the login was successful.
    /// </summary>
    public bool LoginSuccessful { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RcpApiClient"/> class.
    /// </summary>
    /// <param name="email">The email of the user used to log into the RCP.</param>
    /// <param name="password">The password of the user used to log into the RCP.</param>
    private RcpApiClient(string email, string password)
    {
        // Cookie container handler to add PHPSESSID cookie
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
        };

        _client = new HttpClient(handler);
        _client.BaseAddress = new Uri("https://panel.rcponline.pl");

        // Headers from the browser to exactly mimic everything
        _client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        _client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pl-PL,pl;q=0.9");
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        _client.DefaultRequestHeaders.Referrer = new Uri("https://panel.rcponline.pl/app/zdarzenia");
        _client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        _email = email;
        _password = password;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RcpApiClient"/> class and tries to log into the RCP.
    /// </summary>
    /// <param name="email">The email of the user used to log into the RCP.</param>
    /// <param name="password">The password of the user used to log into the RCP.</param>
    /// <returns>Created <see cref="RcpApiClient"/> object with LoginSuccessful parameter set (if true, login sucesfull; otherwise false).</returns>
    public static async Task<RcpApiClient> CreateApiClientAsync(string email, string password)
    {
        var api = new RcpApiClient(email, password);
        
        // Application tries to log in the user every time the object is created
        var applicationHtml = await api.SendLoginEventAsync();
        if (applicationHtml == null)
        {
            // If html is null that means the login was unsuccessful
            api.LoginSuccessful = false;
        }
        else
        {
            api.LoginSuccessful = true;
        }

        return api;
    }

    private string? ExtractCsrfToken(string html)
    {
        var pattern = "name=\"_csrf_token\"[^>]*value=\"(.*?)\""; // pattern to find the csrf token needed for login
        var match = Regex.Match(html, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Sends a login event to the RCP that tries to log in the user.
    /// </summary>
    /// <returns>If login successful the application html is returned; otherwise null.</returns>
    public async Task<string?> SendLoginEventAsync()
    {
        var loginPageResp = await _client.GetAsync("/login/");
        string json = await ReadResponseAsDecompressedString(loginPageResp);

        var csrfToken = ExtractCsrfToken(json);
        if (csrfToken == null)
        {
            // Will be caught in Program.cs
            throw new InvalidOperationException("Coś poszło nie tak, nie udało się znaleźć tokenu csrf");
        }

        var data = new[]
        {
            new KeyValuePair<string,string>("_username", _email),
            new KeyValuePair<string,string>("_password", _password),
            new KeyValuePair<string,string>("_csrf_token", csrfToken)
        };

        var content = new FormUrlEncodedContent(data);

        var loginResp = await _client.PostAsync("/login_check/", content);
        string loginHtml = await ReadResponseAsDecompressedString(loginResp);

        var appResp = await _client.GetAsync("/app/");
        string appHtml = await ReadResponseAsDecompressedString(appResp);

        if (loginHtml.Contains("Panel | RCPonline") && appHtml.Contains("Panel | RCPonline"))
        {
            // If login successful then the returned html in both login and app endpoints
            // is just the application itself and contains "Panel | RCPonline"
            return appHtml;
        }
        else if (loginHtml.Contains("Zaloguj się w systemie rejestracji czasu pracy ‐ RCPonline | RCPonline")
                 && appHtml.Contains("Zaloguj się w systemie rejestracji czasu pracy ‐ RCPonline | RCPonline"))
        {
            // If login unsuccessful then the returned html in both login and app endpoints is just the login view
            return null;
        }
        else
        {
            // Sth not right, throw error, will be caught in Program.cs
            throw new InvalidOperationException("Coś poszło nie tak, nie udało się zalogować do aplikacji");
        }
    }

    /// <summary>
    /// Checks if work has already started through 'getMyStatus' endpoint of the RCP.
    /// </summary>
    /// <returns>true if work has already started; otherwise false.</returns>
    public async Task<bool> CheckIfWorkAlreadyStarted()
    {
        try
        {
            var myStatusResponse = await _client.PostAsync("/dashboard/getMyStatus/1", null);
            string json = await ReadResponseAsDecompressedString(myStatusResponse);
            var parsed = JsonDocument.Parse(json);
            var rawHtml = parsed.RootElement.GetProperty("body").GetString();
            var readableHtml = WebUtility.HtmlDecode(rawHtml);

            if (readableHtml == null)
            {
                throw new InvalidOperationException("Nie udało się odczytać HTMLa z getMyStatus");
            }

            var match = Regex.Match(readableHtml, @"Twój status obecności.*?<span class=""fw-bolder fs-2"">\s*(.*?)\s*</span>", RegexOptions.Singleline);
            if (!match.Success)
            {
                throw new InvalidOperationException("Nie udało się znaleźć statusu obecności w HTMLu z getMyStatus");
            }

            var workStatus = match.Groups[1].Value.Trim();
            if (workStatus != null && (workStatus == "Na stanowisku"
                                       || workStatus == "Praca zdalna"
                                       || workStatus == "W terenie"))
            {
                // User is already at work
                return true;
            }
            else if (workStatus != null && workStatus == "Nie ma")
            {
                // User is not at work
                return false;
            }
            else
            {
                throw new InvalidOperationException("Nie udało się rozpoznać statusu pracy w HTMLu z getMyStatus");
            }
        }
        catch (Exception ex)
        {
            // Sth not right but cannot throw error to Program.cs as this is in Polly retry policy. Needs to be handled here
            SentrySdk.CaptureException(ex);
            File.AppendAllText("output.txt", $"[{DateTime.Now}] Coś poszło nie tak, nie udało się sprawdzić," +
                $"czy użytkownik jest już w pracy.\nMożliwe, że zmieniło się coś w zwracanym z /dashboard/getMyStatus/ HTMLu.\n" +
                $"Metoda: RcpApiClient -> CheckIfWorkAlreadyStarted()\n ex.Message: {ex.Message}\n\n");
            MessageBox.Show(
                "Wystąpił nieoczekiwany błąd. Szczegóły zapisano w pliku output.txt, proszę skonsultować się z administratorem.",
                "EasyRCP - Błąd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            // TODO: tutaj może mail jeszcze do mnie z informacją że coś poszło komuś nie tak - komu i co poszło nie tak

            // Killing the application completly because there is no reason to continue if this fails
            Environment.Exit(1);
            return false;
        }
    }

    /// <summary>
    /// Gets users last activity through 'getMyStatus' endpoint of the RCP.
    /// </summary>
    /// <returns>DateTime of the last activity time.</returns>
    public async Task<DateTime?> GetLatestActivity()
    {
        var myStatusResponse = await _client.PostAsync("/dashboard/getMyStatus/1", null);
        string json = await ReadResponseAsDecompressedString(myStatusResponse);
        var parsed = JsonDocument.Parse(json);
        var rawHtml = parsed.RootElement.GetProperty("body").GetString();
        var readableHtml = WebUtility.HtmlDecode(rawHtml);

        if (readableHtml == null)
        {
            return null;
        }

        var match = Regex.Match(readableHtml, @"Ostatnia aktywność:</a><h3 class=""mb-2"">\s*Dzisiaj\s*(\d{2}:\d{2})\s*</h3>");
        if (match.Success && TimeSpan.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var time))
        {
            var today = DateTime.Today.Add(time);
            return today;
        }

        return null;
    }

    /// <summary>
    /// Sends a start work clock event to the RCP.
    /// </summary>
    /// <param name="empId">The employee ID.</param>
    /// <param name="zone">The zone ID.</param>
    /// <param name="eventTypeId">The event type ID (start work/end work/...).</param>
    /// <param name="project">The project name.</param>
    /// <param name="remote">Indicates whether the event is remote (1 for true, 0 for false).</param>
    /// <returns>True if success; otherwise, false.</returns>
    public async Task<bool> SendClockEventAsync(
        int empId,
        int zone,
        int eventTypeId,
        string project,
        int remote)
    {
        var data = new[]
        {
            new KeyValuePair<string,string>("empId", empId.ToString()),
            new KeyValuePair<string,string>("zone", zone.ToString()),
            new KeyValuePair<string,string>("eventTypeId", eventTypeId.ToString()),
            new KeyValuePair<string,string>("comment", ""),
            new KeyValuePair<string,string>("project", project),
            new KeyValuePair<string,string>("gps", ""), // 51.1082496;16.9803776
            new KeyValuePair<string,string>("remote", remote.ToString()),
            new KeyValuePair<string,string>("photo", ""),
            new KeyValuePair<string,string>("customTime", "")
        };

        var content = new FormUrlEncodedContent(data);
        var success = await SendPostAndHandleResponseAsync("/event/clockEvent", content);
        return success;
    }

    /// <summary>
    /// Sends a project event to the RCP.
    /// </summary>
    /// <param name="empId">The employee ID.</param>
    /// <param name="zone">The zone ID.</param>
    /// <param name="eventTypeId">The event type ID (start work/end work/...).</param>
    /// <param name="project">The project name.</param>
    /// <param name="remote">Indicates whether the event is remote (1 for true, 0 for false).</param>
    /// <returns>True if success; otherwise, false.</returns>
    public async Task<bool> SendProjectEventAsync(
        int empId,
        int zone,
        int eventTypeId,
        string project,
        int remote)
    {
        var data = new[]
        {
            new KeyValuePair<string,string>("empId", empId.ToString()),
            new KeyValuePair<string,string>("zone", zone.ToString()),
            new KeyValuePair<string,string>("eventTypeId", eventTypeId.ToString()),
            new KeyValuePair<string,string>("comment", ""),
            new KeyValuePair<string,string>("project", project),
            new KeyValuePair<string,string>("remote", remote.ToString()),
            new KeyValuePair<string,string>("gps", ""), // 51.1082496;16.9803776
            new KeyValuePair<string,string>("photo", ""),
            new KeyValuePair<string,string>("customTime", "")
        };

        var content = new FormUrlEncodedContent(data);
        var success = await SendPostAndHandleResponseAsync("/event/projectEvent", content);
        return success;
    }

    /// <summary>
    /// Sends a POST request to the specified URI and handles the response.
    /// </summary>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="content">The content to send in the POST request.</param>
    /// <returns>True if success; otherwise, false.</returns>
    private async Task<bool> SendPostAndHandleResponseAsync(string requestUri, FormUrlEncodedContent content)
    {
        var resp = await _client.PostAsync(requestUri, content);
        string json = await ReadResponseAsDecompressedString(resp);

        if (!resp.IsSuccessStatusCode)
        {
            // TODO: tu może wystarczy zrobić throw do Program.cs
            SentrySdk.CaptureException(new Exception($"[{DateTime.Now}] HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}\n{json}\n" +
                $"Metoda: RcpApiClient -> SendPostAndHandleResponseAsync()\n\n"));
            File.AppendAllText("output.txt", $"[{DateTime.Now}] HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}\n{json}\n" +
                $"Metoda: RcpApiClient -> SendPostAndHandleResponseAsync()\n\n");
            MessageBox.Show(
                $"Błąd, szczegóły zostały zapisane w pliku output.txt, proszę skonsultować się z administratorem.",
                "EasyRCP - Błąd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        using var doc = JsonDocument.Parse(json);
        bool success = doc.RootElement.GetProperty("success").GetBoolean();
        if (!success)
        {
            // TODO: tu może wystarczy zrobić throw do Program.cs
            SentrySdk.CaptureException(new Exception($"[{DateTime.Now}] API zwróciło success = false\n{json}\n" +
                $"Metoda: RcpApiClient -> SendPostAndHandleResponseAsync()\n\n"));
            File.AppendAllText("output.txt", $"[{DateTime.Now}] API zwróciło success = false\n{json}\n" +
                $"Metoda: RcpApiClient -> SendPostAndHandleResponseAsync()\n\n");
            MessageBox.Show(
                $"Błąd, szczegóły zostały zapisane w pliku output.txt, proszę skonsultować się z administratorem.",
                "EasyRCP - Błąd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        return success;
    }

    // method to read response as readable json
    private async Task<string> ReadResponseAsDecompressedString(HttpResponseMessage response)
    {
        using var responseStream = await response.Content.ReadAsStreamAsync();
        using var decompressed = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
        using var reader = new StreamReader(decompressed);
        string stringContent = await reader.ReadToEndAsync();
        return stringContent;
    }
}