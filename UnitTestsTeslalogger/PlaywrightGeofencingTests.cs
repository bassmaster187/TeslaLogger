using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.Playwright.Assertions;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class PlaywrightGeofencingTests
    {
        private static readonly string AdminUrl = (Environment.GetEnvironmentVariable("TESLALOGGER_ADMIN_URL") ?? "http://localhost:8888/admin/").TrimEnd('/') + "/";
        private static readonly string ApiUrl = (Environment.GetEnvironmentVariable("TESLALOGGER_API_URL") ?? "http://localhost:5010/").TrimEnd('/') + "/";
        private static readonly string ScreenshotDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestResults", "PlaywrightScreenshots"));
        private static readonly string AdminUser = Environment.GetEnvironmentVariable("TESLALOGGER_ADMIN_USER");
        private static readonly string AdminPassword = Environment.GetEnvironmentVariable("TESLALOGGER_ADMIN_PASSWORD");

        private const string GeofenceFilename = "geofence-private.csv";
        private const string NewGeofenceName = "Tesla Service Center Stuttgart";

        public TestContext TestContext { get; set; }

        private IPlaywright playwright;
        private IBrowser browser;
        private IBrowserContext context;
        private IPage page;
        private string geofenceBackup;
        private readonly List<string> alertMessages = new List<string>();

        [TestInitialize]
        public async Task InitAsync()
        {
            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(ApiUrl + "getfile/" + GeofenceFilename);
                }
                catch (Exception)
                {
                    Assert.Inconclusive("Teslalogger is not reachable (API: " + ApiUrl + ", Admin: " + AdminUrl + ")");
                    return;
                }

                // empty string when the user has no geofence-private.csv yet
                geofenceBackup = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : "";

                string testdata = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "testdata", GeofenceFilename));
                await client.PostAsync(ApiUrl + "writefile/" + GeofenceFilename, new StringContent(testdata, Encoding.UTF8, "text/plain"));
            }

            bool headless = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS") == "1";

            playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless, Channel = "chrome" });
            BrowserNewContextOptions contextOptions = new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1400, Height = 900 } };
            if (!string.IsNullOrEmpty(AdminUser))
                contextOptions.HttpCredentials = new HttpCredentials { Username = AdminUser, Password = AdminPassword };

            context = await browser.NewContextAsync(contextOptions);
            page = await context.NewPageAsync();
            page.Dialog += async (_, dialog) =>
            {
                alertMessages.Add(dialog.Message);
                await dialog.AcceptAsync();
            };
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (page != null)
            {
                try
                {
                    await TakeScreenshotAsync(TestContext.TestName, ColorScheme.Light);
                    await TakeScreenshotAsync(TestContext.TestName + "_dark", ColorScheme.Dark);
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

            // restore the users geofence file
            if (geofenceBackup != null)
            {
                using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    await client.PostAsync(ApiUrl + "writefile/" + GeofenceFilename, new StringContent(geofenceBackup, Encoding.UTF8, "text/plain"));
                }
            }
        }

        private async Task TakeScreenshotAsync(string screenshotName, ColorScheme colorScheme)
        {
            Directory.CreateDirectory(ScreenshotDirectory);
            string screenshotPath = Path.Combine(ScreenshotDirectory, screenshotName + ".png");

            await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = colorScheme });
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
            TestContext.WriteLine("Screenshot: " + screenshotPath);
        }

        [TestMethod]
        public async Task TestGeofencing()
        {
            string[] items = { "Beavers Stadion", "Burger King BC", "Flughafen Stuttgart P0", "Ikea Ulm", "Ladestation Bauhaus Ulm", "Ladestation Norma Ulm" };

            await OpenGeofencingAsync();
            await AssertGeofenceItemsAsync(items);

            // show Ikea Ulm on the map
            int ikeaIndex = Array.IndexOf(items, "Ikea Ulm");
            await GeofenceItem(ikeaIndex).Locator("a[href^='javascript:sf(']").ClickAsync();
            await WaitForJsConditionAsync("window.circle != null && window.circle.getRadius() == 20 && window.map.getZoom() == 17");

            // edit Ikea Ulm
            await GeofenceItem(ikeaIndex).Locator("a[href^='geoadd.php']").ClickAsync();
            await Expect(page.Locator("#text")).ToHaveValueAsync("Ikea Ulm");
            await Expect(page.Locator("#radius")).ToHaveValueAsync("20");
            await Expect(page.Locator("#home")).Not.ToBeCheckedAsync();
            await Expect(page.Locator("#work")).Not.ToBeCheckedAsync();
            await Expect(page.Locator("#charger")).ToBeCheckedAsync();
            await Expect(page.Locator("#ccp")).ToBeCheckedAsync();

            await page.Locator("#text").FillAsync("Ikea Ulm Ladestation");
            await page.Locator("#radius").FillAsync("25");
            await page.Locator("#radius").DispatchEventAsync("change");
            await SaveAsync();

            // the dialog navigates back to the list
            await page.WaitForURLAsync("**/geofencing.php", new PageWaitForURLOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            items[ikeaIndex] = "Ikea Ulm Ladestation";
            await AssertGeofenceItemsAsync(items);

            // check the modified entry
            await GeofenceItem(ikeaIndex).Locator("a[href^='geoadd.php']").ClickAsync();
            await Expect(page.Locator("#text")).ToHaveValueAsync("Ikea Ulm Ladestation");
            await Expect(page.Locator("#radius")).ToHaveValueAsync("25");
            await Expect(page.Locator("#home")).Not.ToBeCheckedAsync();
            await Expect(page.Locator("#work")).Not.ToBeCheckedAsync();
            await Expect(page.Locator("#charger")).ToBeCheckedAsync();
            await Expect(page.Locator("#ccp")).ToBeCheckedAsync();

            // insert a new geofence
            await page.GotoAsync(AdminUrl + "geoadd.php?lat=48.82705&lng=9.0998");
            await Expect(page.Locator("#text")).ToHaveValueAsync("");
            await page.Locator("#text").FillAsync(NewGeofenceName);
            await page.Locator("#radius").FillAsync("55");
            await page.Locator("#radius").DispatchEventAsync("change");
            await SaveAsync();

            await OpenGeofencingAsync();

            string[] itemsNew = { "Beavers Stadion", "Burger King BC", "Flughafen Stuttgart P0", "Ikea Ulm Ladestation", "Ladestation Bauhaus Ulm", "Ladestation Norma Ulm", NewGeofenceName };
            await AssertGeofenceItemsAsync(itemsNew);
        }

        private async Task WaitForJsConditionAsync(string condition, int timeoutMs = 30000, int pollIntervalMs = 100)
        {
            int waited = 0;
            while (true)
            {
                bool ok = await page.EvaluateAsync<bool>("() => (" + condition + ")");
                if (ok)
                    return;

                if (waited >= timeoutMs)
                    Assert.Fail("Timeout waiting for JavaScript condition: " + condition);

                await Task.Delay(pollIntervalMs);
                waited += pollIntervalMs;
            }
        }

        private async Task OpenGeofencingAsync()
        {
            await page.GotoAsync(AdminUrl + "geofencing.php", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForJsConditionAsync("window.map != null");
        }

        private ILocator GeofenceItem(int index)
        {
            return page.Locator(".tl-geo-items > li").Nth(index);
        }

        private async Task AssertGeofenceItemsAsync(string[] items)
        {
            IReadOnlyList<string> names = await page.Locator(".tl-geo-items > li .tl-geo-name").AllInnerTextsAsync();
            List<string> geofenceNames = names.Select(name => name.Trim()).ToList();

            CollectionAssert.AreEqual(items, geofenceNames, "geofence list does not match");
        }

        private async Task SaveAsync()
        {
            await page.WaitForFunctionAsync("() => window.circle != null");
            alertMessages.Clear();

            IResponse response = await page.RunAndWaitForResponseAsync(
                async () => await page.Locator("#btn_save").ClickAsync(),
                "**/geoadd_write.php");

            Assert.IsTrue(response.Ok, "geoadd_write.php failed with status " + response.Status);

            Regex regex = new Regex("Saved|Gespeichert", RegexOptions.IgnoreCase);
            string message = await WaitForConfirmationAsync(regex);
            Assert.IsNotNull(message, "no save confirmation");
        }

        private async Task<string> WaitForConfirmationAsync(Regex regex)
        {
            // save() confirms with the .tl-geo-toast banner, delete() and the old
            // versions used a dialog; the toast is removed by the following navigation
            string toast = await page.EvaluateAsync<string>(
                "() => { var t = document.querySelector('.tl-geo-toast:not(.inactive)'); return t ? t.textContent : ''; }");
            if (regex.IsMatch(toast))
                return toast;

            return await WaitForAlertMessageAsync(regex);
        }

        private async Task<string> WaitForAlertMessageAsync(Regex regex)
        {
            for (int i = 0; i < 100; i++)
            {
                string message = alertMessages.FirstOrDefault(m => regex.IsMatch(m));
                if (message != null)
                    return message;

                await Task.Delay(100);
            }

            return null;
        }
    }
}
