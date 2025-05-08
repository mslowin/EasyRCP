using Polly;
using Polly.Retry;

namespace EasyRCP.Services;

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
            
            // TODO: sprawdzić czy przypadkiem nie wystarczy wysyłać tylko project eventu, żeby rozpocząć pracę
            bool wasProjectChangeRegistered = await api.SendProjectEventAsync(
                empId: 0,           // as above the empId is not needed here at all
                zone: 2,
                eventTypeId: 1,     // 1 = start of the work (this value seems out of place here but just to be sure) 
                project: "01 Administracja",
                remote: 0           // 0 = work on the spot (same as eventTypeId)
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
    /// Checks if work has already started by interacting with the RCP system.
    /// </summary>
    /// <param name="api">The api to connect to RCP.</param>
    /// <returns>true if work has already started; otherwise, false.</returns>
    private static async Task<bool> CheckIfWorkAlreadyStartedAsync(RcpApiClient api)
    {
        return await api.CheckIfWorkAlreadyStarted();
    }
}
