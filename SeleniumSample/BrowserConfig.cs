namespace SeleniumSample;

public class BrowserConfig
{
    public string BrowserName { get; set; } = "chrome";
    public string BrowserVersion { get; set; } = "latest";
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    public bool Headless { get; set; } = false;
}
