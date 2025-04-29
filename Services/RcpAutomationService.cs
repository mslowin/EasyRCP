using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
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
    /// <returns>true if work has already started; false if nor; null if internet connection was down the whole time.</returns>
    public static async Task<bool?> CheckIfWorkAlreadyStartedWithRetryAsync()
    {
        try
        {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            bool result = await CheckIfWorkAlreadyStartedAsync();
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
    public static void StartWork()
    {
        try
        {
            var service = ChromeDriverService.CreateDefaultService();
            var options = new ChromeOptions();

#if !DEBUG
            service.HideCommandPromptWindow = true;
            options.AddArgument("--headless");
#endif

            // Creating the chrome driver
            using var driver = new ChromeDriver(service, options);
            bool wasLoginSuccessful = LogIntoTheRcpAccount(driver);

            if (!wasLoginSuccessful)
            {
                return;
            }

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.Id("remote_select")));
            Thread.Sleep(500);

            driver.ExecuteScript(@"
                var select = document.getElementById('remote_select');
                select.value = '0';
                select.dispatchEvent(new Event('change'));
            ");

            driver.ExecuteScript(@"
                var select = document.getElementById('event_project');
                select.value = ""01 Administracja"";
                select.dispatchEvent(new Event('change'));
            ");

            Console.ReadLine(); // stop the browser for debugging

            // Make sure everything is loaded
            Thread.Sleep(2000);
            IWebElement button = driver.FindElement(By.CssSelector("button.start-work-button"));
            driver.ExecuteScript("arguments[0].click();", button);

            // Just a small debounce to be sure it registers
            Thread.Sleep(1000);

            MessageBox.Show(
                $"Zarejestrowano początek pracy w systemie RCP",
                "EasyRCP - Sukces",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Błąd: {ex.Message}",
                "EasyRCP - Błąd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Checks if work has already started by interacting with the RCP system.
    /// </summary>
    /// <returns>true if work has already started; otherwise, false.</returns>
    private static Task<bool> CheckIfWorkAlreadyStartedAsync()
    {
        return Task.Run(() =>
        {
            var service = ChromeDriverService.CreateDefaultService();
            var options = new ChromeOptions();

#if !DEBUG
            service.HideCommandPromptWindow = true;
            options.AddArgument("--headless");
#endif

            // Creating the chrome driver
            using var driver = new ChromeDriver(service, options);
            bool wasLoginSuccessful = LogIntoTheRcpAccount(driver);

            if (!wasLoginSuccessful)
            {
                return false;
            }

            // Make sure everything is loaded
            Thread.Sleep(2000);

            var startWorkButton = driver.FindElement(By.CssSelector("button.start-work-button"));
            string? classAttribute = startWorkButton.GetAttribute("class");

            return classAttribute?.Contains("disabled") ?? false;
        });
    }

    /// <summary>
    /// Logs into the RCP account using the provided credentials.
    /// </summary>
    /// <param name="driver">The ChromeDriver instance used for automation.</param>
    /// <returns>true if login was successful; otherwise, false.</returns>
    private static bool LogIntoTheRcpAccount(ChromeDriver driver)
    {
        // Navigating to login page
        driver.Navigate().GoToUrl("https://panel.rcponline.pl/login/");

        // Getting the credentials from settings and typing them in
        var credentials = UserCredentialsService.LoadCredentials();
        if (credentials != null)
        {
            driver.FindElement(By.Name("_username")).SendKeys($"{credentials.Value.Email}");
            driver.FindElement(By.Id("password")).SendKeys($"{credentials.Value.Password}");
        }

        // Clicking the login button
        driver.FindElement(By.Id("kt_login_signin_submit")).Click();

        // Checking if login was successful or not
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(3));
        try
        {
            var errorPopup = wait.Until(driver =>
            {
                var element = driver.FindElement(By.ClassName("swal2-title"));
                if (element.Displayed && element.Text.Contains("Nieprawidłowe dane."))
                {
                    // Login failed
                    MessageBox.Show(
                        "Logowanie nie powiodło się, nieprawidłowe dane. Sprawdź ustawienia aplikacji.",
                        "EasyRCP - Błąd logowania",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return (IWebElement?)element;
                }
                else
                {
                    // Login succeeded
                    return null;
                }
            });

            if (errorPopup != null)
            {
                return false;
            }
        }
        catch (WebDriverTimeoutException)
        {
            // The popup was not displayed after 3 seconds - this means login was successful
            return true;
        }

        return false;
    }
}
