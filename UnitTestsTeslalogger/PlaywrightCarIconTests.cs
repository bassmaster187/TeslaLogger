using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static Microsoft.Playwright.Assertions;

namespace UnitTestsTeslalogger
{
    /// <summary>
    /// Renders the car status icon row (the icons next to the car name on the dashboard) with every
    /// icon forced visible, and saves a screenshot for visual review.
    ///
    /// This is a visual regression / design helper: when a new status icon is added to the
    /// .tl-caricons row in www/admin/index.php, it shows up here automatically because the test
    /// force-shows every .caricons element. The mock JSON below only needs a new flag added when the
    /// new icon is driven by a JSON field that is not yet covered.
    ///
    /// Requires a running Teslalogger (Admin http://localhost:8888/admin/, API http://localhost:5010/),
    /// otherwise the test reports Inconclusive. Set PLAYWRIGHT_HEADLESS=1 to run without a visible window.
    /// Screenshots are written to TestResults/PlaywrightScreenshots/.
    /// </summary>
    [TestClass]
    public class PlaywrightCarIconTests
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

        // current body served for the mocked current_json.php; changed at runtime to toggle the icons off
        private string mockJson;

        // A currentjson payload that turns on every status icon: preconditioning, open windows,
        // open frunk / trunk / doors and unlocked. The remaining fields keep the dashboard rendering
        // normally (battery, map marker, trip) so the icons are shown in their real context.
        private const string AllIconsJson = @"{
            ""online"": true,
            ""driving"": false,
            ""charging"": false,
            ""sleeping"": false,
            ""falling_asleep"": false,
            ""plugged_in"": false,
            ""battery_level"": 73,
            ""ideal_battery_range_km"": 320.5,
            ""battery_range_km"": 300.2,
            ""odometer"": 12345.6,
            ""charge_limit_soc"": 80,
            ""inside_temperature"": 21.0,
            ""outside_temp"": 2.0,
            ""is_preconditioning"": true,
            ""battery_heater"": true,
            ""sentry_mode"": false,
            ""open_windows"": 4,
            ""open_doors"": 1,
            ""frunk"": 1,
            ""trunk"": 1,
            ""locked"": false,
            ""tpms_pressure_fl"": 2.4,
            ""tpms_pressure_fr"": 2.9,
            ""tpms_pressure_rl"": 2.9,
            ""tpms_pressure_rr"": 2.9,
            ""latitude"": 47.5,
            ""longitude"": 8.5,
            ""heading"": 90,
            ""car_version"": ""2026.44.1 abcdef0123"",
            ""software_update_status"": """",
            ""software_update_version"": """",
            ""ts"": ""2026-11-24T22:33:53"",
            ""trip_start_dt"": ""2026-11-24T22:00:00"",
            ""trip_kwh"": 5.2,
            ""trip_duration_sec"": 1200,
            ""trip_distance"": 12.3,
            ""trip_max_speed"": 95,
            ""trip_avg_kwh"": 165.0,
            ""TLGeofence"": ""-""
        }";

        // Same dashboard payload but with every status flag turned off, so all icons must disappear.
        private const string NoIconsJson = @"{
            ""online"": true,
            ""driving"": false,
            ""charging"": false,
            ""sleeping"": false,
            ""falling_asleep"": false,
            ""plugged_in"": false,
            ""battery_level"": 73,
            ""ideal_battery_range_km"": 320.5,
            ""battery_range_km"": 300.2,
            ""odometer"": 12345.6,
            ""charge_limit_soc"": 80,
            ""inside_temperature"": 21.0,
            ""outside_temp"": 2.0,
            ""is_preconditioning"": false,
            ""battery_heater"": false,
            ""sentry_mode"": false,
            ""open_windows"": 0,
            ""open_doors"": 0,
            ""frunk"": 0,
            ""trunk"": 0,
            ""locked"": true,
            ""tpms_pressure_fl"": 2.9,
            ""tpms_pressure_fr"": 2.9,
            ""tpms_pressure_rl"": 2.9,
            ""tpms_pressure_rr"": 2.9,
            ""latitude"": 47.5,
            ""longitude"": 8.5,
            ""heading"": 90,
            ""car_version"": ""2026.44.1 abcdef0123"",
            ""software_update_status"": """",
            ""software_update_version"": """",
            ""ts"": ""2026-11-24T22:33:53"",
            ""trip_start_dt"": ""2026-11-24T22:00:00"",
            ""trip_kwh"": 5.2,
            ""trip_duration_sec"": 1200,
            ""trip_distance"": 12.3,
            ""trip_max_speed"": 95,
            ""trip_avg_kwh"": 165.0,
            ""TLGeofence"": ""-""
        }";

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
            // DeviceScaleFactor 3 renders the 28px icons crisp and large enough to judge line weight.
            context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
                DeviceScaleFactor = 3
            });
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
            if (context != null)
                await context.CloseAsync();

            if (browser != null)
                await browser.CloseAsync();

            playwright?.Dispose();
        }

        [TestMethod]
        [DataRow("light")]
        [DataRow("dark")]
        public async Task AllCarIcons_Visible_ContactSheet(string theme)
        {
            this.theme = theme;

            await page.EmulateMediaAsync(new PageEmulateMediaOptions
            {
                ColorScheme = theme == "dark" ? ColorScheme.Dark : ColorScheme.Light
            });

            await page.RouteAsync("**/current_json.php*", route => route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = AllIconsJson
            }));

            await page.GotoAsync(AdminUrl + "index.php?carid=" + carId, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            // Belt and suspenders: the mock already turns every flag on, but force-show every icon in
            // the row so a future icon that is not (yet) covered by the mock still appears.
            await page.WaitForSelectorAsync(".tl-caricons", new PageWaitForSelectorOptions { Timeout = 10000 });
            await page.EvaluateAsync(@"document.querySelectorAll('.caricons').forEach(function(e){ e.style.display = 'inline-block'; });");

            ILocator hero = page.Locator(".tl-hero");
            await hero.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

            Directory.CreateDirectory(ScreenshotDirectory);
            string screenshotPath = Path.Combine(ScreenshotDirectory, $"{TestContext.TestName}_{theme}.png");
            await hero.ScreenshotAsync(new LocatorScreenshotOptions { Path = screenshotPath });
            TestContext.WriteLine("Screenshot: " + screenshotPath);
        }

        [TestMethod]
        [DataRow("light")]
        [DataRow("dark")]
        public async Task CarIcons_ToggleOff_Hidden(string theme)
        {
            this.theme = theme;

            await page.EmulateMediaAsync(new PageEmulateMediaOptions
            {
                ColorScheme = theme == "dark" ? ColorScheme.Dark : ColorScheme.Light
            });

            mockJson = AllIconsJson;
            await page.RouteAsync("**/current_json.php*", route => route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = mockJson
            }));

            await page.GotoAsync(AdminUrl + "index.php?carid=" + carId, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            string[] iconIds = { "#preconditioning", "#battery_heater", "#window_open", "#frunk_open", "#trunk_open", "#door_open", "#unlocked", "#flat_tire" };

            // 1) with all flags on, every icon must be shown by the page's own logic (no forcing here)
            foreach (string id in iconIds)
                await Expect(page.Locator(id)).ToBeVisibleAsync();

            // 2) turn every flag off and let the dashboard refresh its data
            mockJson = NoIconsJson;
            await page.EvaluateAsync("GetCurrentData()");

            // 3) every icon must disappear again -> proves the hide/turn-off branch works
            foreach (string id in iconIds)
                await Expect(page.Locator(id)).ToBeHiddenAsync();

            ILocator hero = page.Locator(".tl-hero");
            await hero.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

            Directory.CreateDirectory(ScreenshotDirectory);
            string screenshotPath = Path.Combine(ScreenshotDirectory, $"{TestContext.TestName}_{theme}.png");
            await hero.ScreenshotAsync(new LocatorScreenshotOptions { Path = screenshotPath });
            TestContext.WriteLine("Screenshot: " + screenshotPath);
        }

        // Builds a minimal currentjson payload with the given tire pressures (bar) and every other status flag off.
        private static string BuildTpmsJson(double fl, double fr, double rl, double rr)
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            return "{" +
                "\"online\": true, \"driving\": false, \"charging\": false, \"sleeping\": false, \"falling_asleep\": false," +
                "\"battery_level\": 73, \"ideal_battery_range_km\": 320.5, \"battery_range_km\": 300.2, \"odometer\": 12345.6," +
                "\"is_preconditioning\": false, \"battery_heater\": false, \"open_windows\": 0, \"open_doors\": 0, \"frunk\": 0, \"trunk\": 0, \"locked\": true," +
                "\"tpms_pressure_fl\": " + fl.ToString(c) + ", \"tpms_pressure_fr\": " + fr.ToString(c) + ", \"tpms_pressure_rl\": " + rl.ToString(c) + ", \"tpms_pressure_rr\": " + rr.ToString(c) + "," +
                "\"latitude\": 47.5, \"longitude\": 8.5, \"heading\": 90, \"ts\": \"2026-11-24T22:33:53\", \"trip_start_dt\": \"2026-11-24T22:00:00\", \"trip_kwh\": 5.2, \"trip_duration_sec\": 1200, \"trip_distance\": 12.3, \"trip_max_speed\": 95, \"trip_avg_kwh\": 165.0" +
                "}";
        }

        // The flat tire icon must appear as soon as one tire with a reading drops below 2.6 bar,
        // and must stay hidden when all tires are fine or when the car has no TPMS readings (0).
        [TestMethod]
        [DataRow(2.9, 2.9, 2.9, 2.9, false)]
        [DataRow(2.4, 2.9, 2.9, 2.9, true)]
        [DataRow(2.9, 2.9, 2.5, 2.9, true)]
        [DataRow(0.0, 0.0, 0.0, 0.0, false)]
        public async Task FlatTire_LowPressure_ShowsIcon(double fl, double fr, double rl, double rr, bool expectLow)
        {
            this.theme = "light";

            await page.RouteAsync("**/current_json.php*", route => route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = BuildTpmsJson(fl, fr, rl, rr)
            }));

            await page.GotoAsync(AdminUrl + "index.php?carid=" + carId, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForSelectorAsync("#flat_tire", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached, Timeout = 10000 });

            ILocator flatTire = page.Locator("#flat_tire");
            if (expectLow)
                await Expect(flatTire).ToBeVisibleAsync();
            else
                await Expect(flatTire).ToBeHiddenAsync();
        }
    }
}
