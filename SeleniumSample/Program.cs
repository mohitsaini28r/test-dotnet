using SeleniumSample;

// ─────────────────────────────────────────────────────────────────────────────
// Toggle: set useGrid = true to run on LambdaTest grid (needs env vars set)
//         set useGrid = false to run locally
// ─────────────────────────────────────────────────────────────────────────────
bool useGrid = args.Contains("--grid");

var browserConfig = new BrowserConfig
{
    BrowserName    = "chrome",   // "chrome" | "microsoftedge" | "firefox"
    BrowserVersion = "latest",
    UserAgent      = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) MyCustomAgent/1.0",
    Headless       = !useGrid,   // headless locally; LambdaTest handles its own display
};

LambdaTestConfig? ltConfig = null;

if (useGrid)
{
    ltConfig = new LambdaTestConfig
    {
        BuildName   = "[HyperExecute] Selenium C# Demo",
        TestName    = "TodoApp - AddAndCheck",
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
    var test = new TodoAppTest(driver);
    test.Run();
}
finally
{
    driver.Quit();
    Console.WriteLine("[INFO] Driver closed.");
}

return 0;
