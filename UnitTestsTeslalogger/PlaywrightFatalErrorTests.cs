using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.Playwright.Assertions;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class PlaywrightFatalErrorTests
    {
        private static readonly string AdminUrl = (Environment.GetEnvironmentVariable("TESLALOGGER_ADMIN_URL") ?? "http://localhost:8888/admin/").TrimEnd('/') + "/";
        private static readonly string ApiUrl = (Environment.GetEnvironmentVariable("TESLALOGGER_API_URL") ?? "http://localhost:5010/").TrimEnd('/') + "/";
        private static readonly string ScreenshotDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestResults", "PlaywrightScreenshots"));

        public TestContext TestContext { get; set; }

        private IPlaywright playwright;
        private IBrowser browser;
        private IBrowserContext context;
        private IPage page;
        private int carId;
        private string theme;

        [TestInitialize]
        public async Task InitAsync()
        {
            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
            {
                try
                {
                    carId = await GetActiveCarIdAsync(client);
                    await client.GetAsync(AdminUrl + "index.php?carid=" + carId);
                }
                catch (Exception)
                {
                    Assert.Inconclusive("Teslalogger admin panel is not reachable at " + AdminUrl + " (API: " + ApiUrl + ")");
                }
            }

            bool headless = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS") == "1";

            playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, Channel = "chrome" });
            context = await browser.NewContextAsync(new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1400, Height = 900 } });
            page = await context.NewPageAsync();
        }

        private static async Task<int> GetActiveCarIdAsync(HttpClient client)
        {
            string json = await client.GetStringAsync(ApiUrl + "getallcars");
            JArray cars = JArray.Parse(json);

            JToken activeCar = cars.FirstOrDefault(c => c["inactive"]?.ToString() != "1");
            if (activeCar == null)
                throw new InvalidOperationException("No active car found.");

            return activeCar["id"].Value<int>();
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (page != null)
            {
                try
                {
                    Directory.CreateDirectory(ScreenshotDirectory);
                    string screenshotName = TestContext.TestName;
                    if (!string.IsNullOrEmpty(theme))
                        screenshotName += "_" + theme;
                    string screenshotPath = Path.Combine(ScreenshotDirectory, screenshotName + ".png");
                    await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
                    TestContext.WriteLine("Screenshot: " + screenshotPath);
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine("Screenshot failed: " + ex.Message);
                }
            }

            if (context != null)
                await context.CloseAsync();

            if (browser != null)
                await browser.CloseAsync();

            playwright?.Dispose();
        }

        [TestMethod]
        [DataRow("light")]
        [DataRow("dark")]
        public async Task FatalErrorMaxConfigs_ShowsTranslatedMessage(string theme)
        {
            await SetThemeAsync(theme);
            await MockCurrentJsonAsync(@"{ ""FatalError"": ""max_configs"" }");

            await OpenDashboardAsync();

            await Expect(page.Locator("#car_statusLabel")).ToHaveAttributeAsync("data-state", "error");

            ILocator details = page.Locator("#car_status");
            await Expect(details).ToContainTextAsync(new Regex("Drittanbieter|third-party", RegexOptions.IgnoreCase));
            await Expect(details).Not.ToContainTextAsync("max_configs");
            await Expect(details).Not.ToContainTextAsync("FatalErrorMaxConfigs");
        }

        [TestMethod]
        [DataRow("light")]
        [DataRow("dark")]
        public async Task FatalErrorMissingKey_ShowsVirtualKeyLink(string theme)
        {
            await SetThemeAsync(theme);
            await MockCurrentJsonAsync(@"{ ""FatalError"": ""missing_key"" }");

            await OpenDashboardAsync();

            await Expect(page.Locator("#car_statusLabel")).ToHaveAttributeAsync("data-state", "error");

            ILocator details = page.Locator("#car_status");
            await Expect(details).ToContainTextAsync(new Regex("Virtual Key", RegexOptions.IgnoreCase));
            await Expect(details.Locator("a[href='https://www.tesla.com/_ak/teslalogger.de']")).ToBeVisibleAsync();
        }

        [TestMethod]
        [DataRow("light")]
        [DataRow("dark")]
        public async Task FatalErrorGeneric_ShowsRawMessage(string theme)
        {
            await SetThemeAsync(theme);
            await MockCurrentJsonAsync(@"{ ""FatalError"": ""Telemetry Login Error!!! Check Logfile!"" }");

            await OpenDashboardAsync();

            await Expect(page.Locator("#car_statusLabel")).ToHaveAttributeAsync("data-state", "error");
            await Expect(page.Locator("#car_status")).ToContainTextAsync("Telemetry Login Error!!! Check Logfile!");
        }

        private async Task SetThemeAsync(string theme)
        {
            this.theme = theme;
            await page.EmulateMediaAsync(new PageEmulateMediaOptions
            {
                ColorScheme = theme == "dark" ? ColorScheme.Dark : ColorScheme.Light
            });
        }

        private async Task MockCurrentJsonAsync(string json)
        {
            await page.RouteAsync("**/current_json.php*", route => route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = json
            }));
        }

        private async Task OpenDashboardAsync()
        {
            await page.GotoAsync(AdminUrl + "index.php?carid=" + carId, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        }
    }
}
