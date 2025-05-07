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
                Console.WriteLine($"[Retry {retryCount}] Błąd: {exception.Message}. Próba ponownie za {timeSpan.TotalSeconds} sek.");
            });

    /// <summary>
    /// Checks if work has already started with retry logic.
    /// </summary>
    /// <param name="api">The api to connect to RCP client.</param>
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
            Console.WriteLine($"Wszystkie retry zakończone niepowodzeniem (pewnie brak internetu): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Starts the work by interacting with the RCP system.
    /// </summary>
    public static async Task StartWorkAsync()
    {
        throw new NotImplementedException();
////        try
////        {
////            var service = ChromeDriverService.CreateDefaultService();
////            var options = new ChromeOptions();

////#if !DEBUG
////                service.HideCommandPromptWindow = true;
////                options.AddArgument("--headless");
////#endif

////            // Creating the chrome driver
////            using var driver = new ChromeDriver(service, options);
////            string? applicationHtml = LogIntoTheRcpAccount();

////            if (applicationHtml == null)
////            {
////                return;
////            }

////            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
////            wait.Until(d => d.FindElement(By.Id("remote_select")));
////            Thread.Sleep(500);

////            var cookies = driver.Manage().Cookies.AllCookies;
////            var phpsess = cookies.First(c => c.Name == "PHPSESSID").Value;

////            var api = new RcpApiClient(phpsess);

////            bool wasStartWorkRegistered = await api.SendClockEventAsync(
////                empId: 0,           // it turns out that the empId is not needed at all. Propably PHPSESSID cookie does the job
////                zone: 2,
////                eventTypeId: 1,     // 1 = start of the work
////                project: "",
////                remote: 0           // 0 = work on the spot
////            );

////            bool wasProjectChangeRegistered = await api.SendProjectEventAsync(
////                empId: 0,           // as above the empId is not needed here at all
////                zone: 2,
////                eventTypeId: 1,     // 1 = start of the work (this value seems out of place here but just to be sure) 
////                project: "01 Administracja",
////                remote: 0           // 0 = work on the spot (same as eventTypeId)
////            );

////            if (!wasStartWorkRegistered || !wasProjectChangeRegistered)
////            {
////                // messagebox handling is done in the api class so here we just return
////                return;
////            }

////            // Just a small debounce to be sure it registers
////            Thread.Sleep(1000);

////            MessageBox.Show(
////                $"Zarejestrowano początek pracy w systemie RCP",
////                "EasyRCP - Sukces",
////                MessageBoxButtons.OK,
////                MessageBoxIcon.Information);
////        }
////        catch (Exception ex)
////        {
////            MessageBox.Show(
////                $"Błąd: {ex.Message}",
////                "EasyRCP - Błąd",
////                MessageBoxButtons.OK,
////                MessageBoxIcon.Error);
////        }
    }

    /// <summary>
    /// Checks if work has already started by interacting with the RCP system.
    /// </summary>
    /// <param name="api">The api to connect to RCP client.</param>
    /// <returns>true if work has already started; otherwise, false.</returns>
    private static Task<bool> CheckIfWorkAlreadyStartedAsync(RcpApiClient api)
    {
        return Task.Run(async () =>
        {
            bool isWorkAlreadyStarted = await api.CheckIfWorkAlreadyStarted();

            return isWorkAlreadyStarted;
        });
    }
}
