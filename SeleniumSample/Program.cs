using SeleniumSample;

// ─────────────────────────────────────────────────────────────────────────────
// Toggle: set useGrid = true to run on LambdaTest grid (needs env vars set)
//         set useGrid = false to run locally
// ─────────────────────────────────────────────────────────────────────────────
bool useGrid = args.Contains("--grid");
bool useDemo = args.Contains("--demo");

var browserConfig = new BrowserConfig
{
    BrowserName    = "chrome",
    BrowserVersion = "latest",
    UserAgent      = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) MyCustomAgent/1.0",
    Headless       = !useGrid && !useDemo,  // visible browser for --demo
};

LambdaTestConfig? ltConfig = null;

if (useGrid)
{
    ltConfig = new LambdaTestConfig
    {
        BuildName   = "[HyperExecute] Selenium C# Demo",
        TestName    = useDemo ? "MidTest UA Injection Demo" : "TodoApp - AddAndCheck",
        Platform    = Environment.GetEnvironmentVariable("HYPEREXECUTE_PLATFORM") ?? "Windows 10",
        Network     = true,
        Console     = true,
        Visual      = true,
        Accessibility = false,
    };

    if (string.IsNullOrWhiteSpace(ltConfig.Username) || string.IsNullOrWhiteSpace(ltConfig.AccessKey))
    {
        Console.Error.WriteLine("ERROR: LT_USERNAME and LT_ACCESS_KEY environment variables must be set.");
        return 1;
    }

    Console.WriteLine($"[GRID] Hub      : https://hub.lambdatest.com/wd/hub");
    Console.WriteLine($"[GRID] User     : {ltConfig.Username}");
    Console.WriteLine($"[GRID] Build    : {ltConfig.BuildName}");
    Console.WriteLine($"[GRID] Platform : {ltConfig.Platform}");
}
else
{
    Console.WriteLine("[LOCAL] Running locally");
}

Console.WriteLine($"[INFO] Browser  : {browserConfig.BrowserName}");
Console.WriteLine($"[INFO] UserAgent: {browserConfig.UserAgent}");
Console.WriteLine();

var factory = new WebDriverFactory(browserConfig, ltConfig);
using var driver = factory.CreateDriver();

try
{
    if (useDemo)
    {
        // ── Mid-test UA injection demo (local, visible browser) ──────────────
        var demo = new MidTestUADemo(driver);
        await demo.RunAsync();
    }
    else
    {
        // ── Normal test run (grid or headless local) ──────────────────────────
        const string runtimeUA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 RuntimeChanged/1.0";

        await UserAgentHelper.SetUserAgentAsync(driver, runtimeUA);

        driver.Navigate().GoToUrl("about:blank");
        var actualUA = ((OpenQA.Selenium.IJavaScriptExecutor)driver)
            .ExecuteScript("return navigator.userAgent") as string;
        Console.WriteLine($"[VERIFY] navigator.userAgent = {actualUA}");

        if (actualUA != null && actualUA.Contains("RuntimeChanged"))
        {
            Console.WriteLine("[VERIFY] ✅ UA override confirmed — proceeding to test.");
            var test = new TodoAppTest(driver);
            test.Run();
        }
        else
        {
            Console.WriteLine("[VERIFY] ❌ UA override did not apply — skipping test.");
            ((OpenQA.Selenium.IJavaScriptExecutor)driver)
                .ExecuteScript("lambda-status=failed");
        }
    }
}
finally
{
    driver.Quit();
    Console.WriteLine("[INFO] Driver closed.");
}

return 0;
