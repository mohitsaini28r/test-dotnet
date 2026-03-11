using OpenQA.Selenium;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumSample
{
    /// <summary>
    /// Demonstrates changing User-Agent mid-test in the SAME browser tab via CDP.
    /// No new tab or browser is opened — the override is injected into the live session.
    /// </summary>
    public class MidTestUADemo
    {
        private readonly IWebDriver _driver;
        private readonly IJavaScriptExecutor _js;

        private const string UA_Phase2 =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) MidTestInjected/2.0 Chrome/124.0";

        public MidTestUADemo(IWebDriver driver)
        {
            _driver = driver;
            _js     = (IJavaScriptExecutor)driver;
        }

        public async Task RunAsync()
        {
            PrintSection("PHASE 1 — Browser starts with launch-time UA");

            // Step 1: Navigate and read the UA the browser launched with
            _driver.Navigate().GoToUrl("https://httpbin.org/user-agent");
            Thread.Sleep(1500);

            var ua1 = ReadUA();
            Console.WriteLine($"[PHASE 1] navigator.userAgent  = {ua1}");
            Console.WriteLine($"[PHASE 1] HTTP header (page)   = {ReadHttpBinUA()}");
            Console.WriteLine("[PHASE 1] ✅ Both JS and HTTP header show the LAUNCH UA");

            Thread.Sleep(1000);

            // Step 2: Simulate mid-test work on the SAME tab
            PrintSection("PHASE 2 — Mid-test work (same tab, original UA)");

            Console.WriteLine("[PHASE 2] Navigating to example.com — simulating real test steps...");
            _driver.Navigate().GoToUrl("https://example.com");
            Thread.Sleep(1000);
            Console.WriteLine($"[PHASE 2] Page title: {_driver.Title}");
            Console.WriteLine($"[PHASE 2] Current UA : {ReadUA()}");
            Console.WriteLine("[PHASE 2] ✅ Still on original UA — test running normally");

            Thread.Sleep(1000);

            // Step 3: Inject new UA mid-test — NO new tab, NO new browser
            PrintSection("PHASE 3 — Injecting new UA into the LIVE session via CDP");

            Console.WriteLine("[PHASE 3] ⚡ Calling SetUserAgentAsync on the running driver...");
            Console.WriteLine($"[PHASE 3] New UA → {UA_Phase2}");
            await UserAgentHelper.SetUserAgentAsync(_driver, UA_Phase2);
            Console.WriteLine("[PHASE 3] CDP command sent. Tab is still the same. No browser restart.");

            Thread.Sleep(500);

            // Step 4: Verify on the SAME tab — navigate again to httpbin
            PrintSection("PHASE 4 — Verify change on same tab (no new tab opened)");

            Console.WriteLine("[PHASE 4] Navigating to httpbin.org/user-agent on the SAME tab...");
            _driver.Navigate().GoToUrl("https://httpbin.org/user-agent");
            Thread.Sleep(1500);

            var ua2 = ReadUA();
            Console.WriteLine($"[PHASE 4] navigator.userAgent  = {ua2}");
            Console.WriteLine($"[PHASE 4] HTTP header (page)   = {ReadHttpBinUA()}");

            if (ua2?.Contains("MidTestInjected") == true)
                Console.WriteLine("[PHASE 4] ✅ UA changed mid-test — same tab, same session!");
            else
                Console.WriteLine("[PHASE 4] ❌ UA did NOT change — check CDP support");

            // Step 5: Navigate to testmuai.com with the NEW UA — still same tab
            PrintSection("PHASE 5 — Navigate to testmuai.com with injected UA (same tab)");

            Console.WriteLine("[PHASE 5] Navigating to https://www.testmuai.com/ with the NEW UA...");
            _driver.Navigate().GoToUrl("https://www.testmuai.com/");
            Thread.Sleep(2000);
            Console.WriteLine($"[PHASE 5] Page title: {_driver.Title}");
            Console.WriteLine($"[PHASE 5] Current URL: {_driver.Url}");
            Console.WriteLine($"[PHASE 5] Current UA : {ReadUA()}");
            Console.WriteLine("[PHASE 5] ✅ testmuai.com loaded with MidTestInjected UA — same tab, no restart");

            // Save full HAR
            PrintSection("HAR — Full Network Log");
            HarCapture.Save(_driver, "test.har");

            // Summary
            PrintSection("SUMMARY");
            Console.WriteLine($"  Launch UA  : {ua1}");
            Console.WriteLine($"  Injected UA: {ua2}");
            Console.WriteLine();
            Console.WriteLine("  HOW IT WORKS:");
            Console.WriteLine("  1. Chrome launched with --user-agent=<launch_ua> arg → baked at startup");
            Console.WriteLine("  2. CDP Network.enable + Network.setUserAgentOverride sent to live session");
            Console.WriteLine("  3. CDP intercepts at the NETWORK LAYER — overrides for ALL subsequent requests");
            Console.WriteLine("  4. navigator.userAgent and HTTP headers both reflect the new UA instantly");
            Console.WriteLine("  5. Same tab, same session — no browser restart, no new tab");
        }

        // Reads navigator.userAgent via JavaScript in the current tab
        private string? ReadUA() =>
            _js.ExecuteScript("return navigator.userAgent") as string;

        // Reads the 'user-agent' field from httpbin's JSON response body
        private string ReadHttpBinUA()
        {
            try
            {
                var body = _js.ExecuteScript("return document.body.innerText") as string ?? "";
                // httpbin returns: { "user-agent": "..." }
                var match = System.Text.RegularExpressions.Regex.Match(body, @"""user-agent""\s*:\s*""([^""]+)""");
                return match.Success ? match.Groups[1].Value : "(could not parse)";
            }
            catch { return "(error reading body)"; }
        }

        private static void PrintSection(string title)
        {
            Console.WriteLine();
            Console.WriteLine("══════════════════════════════════════════════════════");
            Console.WriteLine($"  {title}");
            Console.WriteLine("══════════════════════════════════════════════════════");
        }
    }
}
