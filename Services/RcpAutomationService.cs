using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace EasyRCP.Services;
public static class RcpAutomationService
{
    public static void StartWork()
    {
        try
        {
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();

#if !DEBUG
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

            MessageBox.Show($"Zarejestrowano początek pracy", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static bool CheckIfWorkAlreadyStarted()
    {
        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;

        var options = new ChromeOptions();

#if !DEBUG
            options.AddArgument("--headless");
#endif

        // Creating the chrome driver
        using var driver = new ChromeDriver(service, options);
        bool wasLoginSuccessful = LogIntoTheRcpAccount(driver);

        if (!wasLoginSuccessful)
        {
            return false; // Handle login failure gracefully
        }

        // Make sure everything is loaded
        Thread.Sleep(2000);

        var startWorkButton = driver.FindElement(By.CssSelector("button.start-work-button"));

        // Safely handle the possibility of a null value
        string? classAttribute = startWorkButton.GetAttribute("class");

        // Use null-coalescing operator to ensure a non-null value
        return classAttribute?.Contains("disabled") ?? false;
    }

    /// <summary>
    /// Logs into the RCP account using the provided credentials.
    /// </summary>
    /// <param name="driver"></param>
    /// <returns>true if login was successful, otherwise false.</returns>
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
                        "Błąd logowania",
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
