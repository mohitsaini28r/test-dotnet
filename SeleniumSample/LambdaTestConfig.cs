namespace SeleniumSample;

/// <summary>
/// LambdaTest grid configuration.
/// Set LT_USERNAME and LT_ACCESS_KEY as environment variables,
/// or populate directly here for quick testing.
/// </summary>
public class LambdaTestConfig
{
    public string Username   { get; set; } = Environment.GetEnvironmentVariable("LT_USERNAME")   ?? "";
    public string AccessKey  { get; set; } = Environment.GetEnvironmentVariable("LT_ACCESS_KEY") ?? "";

    // Selenium hub — equivalent of the Java example's hub URL
    public string HubUrl => $"https://{Username}:{AccessKey}@hub.lambdatest.com/wd/hub";

    // LT:Options — mirrors the Java example's ltOptions HashMap
    public string BuildName    { get; set; } = "Selenium C# Build";
    public string TestName     { get; set; } = "Selenium C# Test";
    public string Platform     { get; set; } = Environment.GetEnvironmentVariable("HYPEREXECUTE_PLATFORM") ?? "Windows 10";
    public bool   Tunnel       { get; set; } = false;
    public bool   Network      { get; set; } = true;
    public bool   Console      { get; set; } = true;
    public bool   Visual       { get; set; } = true;
    public string SeleniumVer  { get; set; } = "4.24.0";

    // Accessibility
    public bool   Accessibility    { get; set; } = false;
    public string WcagVersion      { get; set; } = "wcag21a";
    public bool   BestPractice     { get; set; } = false;
    public bool   NeedsReview      { get; set; } = true;
}
