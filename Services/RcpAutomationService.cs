using Polly;
using Polly.Retry;

namespace EasyRCP.Services;

/// <summary>
/// Provides automation services for interacting with the RCP system, including work start/end registration and status checks.
/// </summary>
public static class RcpAutomationService
{
    private static readonly AsyncRetryPolicy RetryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            retryCount: 6,
            sleepDurationProvider: retryAttempt => TimeSpan.FromMinutes(1),
            onRetry: (exception, timeSpan, retryCount, _) =>
            {
                File.AppendAllText("output.txt", $"[{DateTime.Now}] [Retry {retryCount}] Błąd: {exception.Message}. Próba ponownie za {timeSpan.TotalSeconds} sek.\n\n");
            });

    /// <summary>
    /// Checks if work has already started with retry logic.
    /// </summary>
    /// <param name="api">The api to connect to RCP.</param>
    /// <returns>true if work has already started; false if nor; null if internet connection was down the whole time.</returns>
    public static async Task<bool?> CheckIfWorkAlreadyStartedWithRetryAsync(RcpApiClient api)
    {
        try
        {
            return await RetryPolicy.ExecuteAsync(async () =>
            {
                bool result = await CheckIfWorkAlreadyStartedAsync(api);
                return result;
            });
        }
        catch (Exception ex)
        {
            File.AppendAllText("output.txt", $"[{DateTime.Now}] {ex}\n\n");
            Console.WriteLine($"Wszystkie retry zakończone niepowodzeniem (pewnie brak internetu). Szczegóły błędu zapisano w pliku output.txt");
            return null;
        }
    }

    /// <summary>
    /// Starts the work by interacting with the RCP system.
    /// </summary>
    /// <param name="api">The api to connect to RCP.</param>
    public static async Task StartWorkAsync(RcpApiClient api)
    {
        try
        {
            bool wasStartWorkRegistered = await api.SendClockEventAsync(
                empId: 0,           // it turns out that the empId is not needed at all. Propably PHPSESSID cookie does the job
                zone: 2,
                eventTypeId: 1,     // 1 = start of the work
                project: "",
                remote: 0           // 0 = work on the spot
            );

            // TODO: wyrzucić to do osobnej metody, może do RcpApiClient (i użyć _client może po prostu)
            using var httpClient = new HttpClient();
            string ip = await httpClient.GetStringAsync("https://api.ipify.org");

            // 0 = work on the spot, 1 = remote work
            int remote = (ip.Trim() == "95.143.240.2") ? 0 : 1;

            // TODO: sprawdzić czy przypadkiem nie wystarczy wysyłać tylko project eventu, żeby rozpocząć pracę
            // TODO: wygląda na to że serio wystarczy wysłać tylko project event xD, ale posprawdzać i tak
            bool wasProjectChangeRegistered = await api.SendProjectEventAsync(
                empId: 0,           // as above the empId is not needed here at all
                zone: 2,
                eventTypeId: 1,     // 1 = start of the work
                project: "01 Administracja",
                remote: remote
            );

            if (!wasStartWorkRegistered || !wasProjectChangeRegistered)
            {
                // messagebox handling is done in the api class so here we just return
                return;
            }

            MessageBox.Show(
                $"Zarejestrowano początek pracy w systemie RCP",
                "EasyRCP - Sukces",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            // TODO: error handling jest w PRogram.cs - sprawdzić, czy bez try catcha tutaj będą ładnie szły błędy do
            // Program.cs właśnie w każdym przypadku (zarówno z metody wywoływanej w Program.cs, jak i z opcji w tray menu)

            // TODO: tutaj może mail jeszcze do mnie z informacją że coś poszło komuś nie tak - komu i co poszło nie tak
            File.AppendAllText("output.txt", $"[{DateTime.Now}] {ex}\n\n");
            MessageBox.Show(
                "Wystąpił nieoczekiwany błąd, nie udało się zarejestrować początku pracy. Szczegóły błędu zapisano w pliku output.txt",
                "EasyRCP - Błąd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Ends work by interacting with the RCP system.
    /// </summary>
    /// <param name="api">The api to connect to RCP.</param>
    public static async Task EndWork(RcpApiClient api)
    {
        try
        {
            // TODO: wyrzucić to do osobnej metody, może do RcpApiClient (i użyć _client może po prostu)
            // TODO: bardzo prawdopodobne, że to nie jest potrzebne tutaj, ale sprawdzić lepiej
            using var httpClient = new HttpClient();
            string ip = await httpClient.GetStringAsync("https://api.ipify.org");

            // 0 = work on the spot, 1 = remote work
            int remote = (ip.Trim() == "95.143.240.2") ? 0 : 1;

            bool wasEndWorkRegistered = await api.SendClockEventAsync(
                empId: 0,           // empId is not needed here at all
                zone: 2,
                eventTypeId: 6,     // 6 = end of the work
                project: "01 Administracja",
                remote: remote
            );

            if (!wasEndWorkRegistered)
            {
                // messagebox handling is done in the api class so here we just return
                return;
            }

            MessageBox.Show(
                $"Zarejestrowano koniec pracy w systemie RCP",
                "EasyRCP - Sukces",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            // TODO: error handling jest w PRogram.cs - sprawdzić, czy bez try catcha tutaj będą ładnie szły błędy do
            // Program.cs właśnie w każdym przypadku (zarówno z metody wywoływanej w Program.cs, jak i z opcji w tray menu)

            // TODO: tutaj może mail jeszcze do mnie z informacją że coś poszło komuś nie tak - komu i co poszło nie tak
            File.AppendAllText("output.txt", $"[{DateTime.Now}] {ex}\n\n");
            MessageBox.Show(
                "Wystąpił nieoczekiwany błąd, nie udało się zarejestrować końca pracy. Szczegóły błędu zapisano w pliku output.txt",
                "EasyRCP - Błąd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Checks if work has already started by interacting with the RCP system.
    /// </summary>
    /// <param name="api">The api to connect to RCP.</param>
    /// <returns>true if work has already started; otherwise, false.</returns>
    public static async Task<bool> CheckIfWorkAlreadyStartedAsync(RcpApiClient api)
    {
        return await api.CheckIfWorkAlreadyStarted();
    }

    /// <summary>
    /// Gets users last activity by interacting with the RCP system.
    /// </summary>
    /// <returns>DateTime of the last activity time.</returns>
    public static async Task<DateTime?> GetLatestActivityAsync(RcpApiClient api)
    {
        return await api.GetLatestActivity();
    }
}
