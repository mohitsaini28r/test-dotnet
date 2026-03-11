using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;

namespace SeleniumSample;

public class WebDriverFactory
{
    private readonly BrowserConfig _config;
    private readonly LambdaTestConfig? _ltConfig;

    /// <param name="config">Browser settings (name, user-agent, headless).</param>
    /// <param name="ltConfig">When provided the driver runs on the LambdaTest grid instead of locally.</param>
    public WebDriverFactory(BrowserConfig config, LambdaTestConfig? ltConfig = null)
    {
        _config   = config;
        _ltConfig = ltConfig;
    }

    public IWebDriver CreateDriver()
    {
        var options = BuildOptions();

        if (_ltConfig is not null)
        {
            // ── Remote / LambdaTest grid ──────────────────────────────────────
            // Mirrors the Java RemoteWebDriver constructor:
            //   new RemoteWebDriver(new URL("https://user:key@hub.lambdatest.com/wd/hub"), browserOptions)
            return new RemoteWebDriver(new Uri(_ltConfig.HubUrl), options);
        }

        // ── Local execution ───────────────────────────────────────────────────
        return _config.BrowserName.ToLower() switch
        {
            "microsoftedge" => new EdgeDriver((EdgeOptions)options),
            "chrome"        => new ChromeDriver((ChromeOptions)options),
            "firefox"       => new FirefoxDriver((FirefoxOptions)options),
            _               => throw new NotSupportedException($"Browser '{_config.BrowserName}' is not supported.")
        };
    }

    private DriverOptions BuildOptions()
    {
        switch (_config.BrowserName.ToLower())
        {
            // ── Microsoft Edge ────────────────────────────────────────────────
            case "microsoftedge":
            {
                var opts = new EdgeOptions();
                opts.AddArgument($"--user-agent={_config.UserAgent}");
                // ms:edgeChromium is implicit in Selenium 4 EdgeOptions,
                // but kept here to match the original pattern explicitly
                opts.AddAdditionalOption("ms:edgeChromium", true);
                if (_config.Headless)
                    opts.AddArgument("--headless=new");
                if (_ltConfig is not null)
                    opts.AddAdditionalOption("LT:Options", BuildLtOptions());
                return opts;
            }

            // ── Chrome ────────────────────────────────────────────────────────
            case "chrome":
            {
                var opts = new ChromeOptions();
                opts.AddArgument($"--user-agent={_config.UserAgent}");
                if (_config.Headless)
                    opts.AddArgument("--headless=new");
                // Enable CDP network events via performance log (used for HAR capture)
                opts.SetLoggingPreference(LogType.Performance, LogLevel.All);
                if (_ltConfig is not null)
                    opts.AddAdditionalOption("LT:Options", BuildLtOptions());
                return opts;
            }

            // ── Firefox ───────────────────────────────────────────────────────
            case "firefox":
            {
                var opts = new FirefoxOptions();
                // Firefox uses a preference instead of a CLI flag for the UA
                opts.SetPreference("general.useragent.override", _config.UserAgent);
                if (_config.Headless)
                    opts.AddArgument("--headless");
                if (_ltConfig is not null)
                    opts.AddAdditionalOption("LT:Options", BuildLtOptions());
                return opts;
            }

            default:
                throw new NotSupportedException($"Browser '{_config.BrowserName}' is not supported.");
        }
    }

    /// Builds the LT:Options dictionary — C# equivalent of the Java ltOptions HashMap.
    private Dictionary<string, object> BuildLtOptions()
    {
        var lt = new Dictionary<string, object>
        {
            ["build"]            = _ltConfig!.BuildName,
            ["name"]             = _ltConfig.TestName,
            ["platformName"]     = _ltConfig.Platform,
            ["browserVersion"]   = _config.BrowserVersion,
            ["tunnel"]           = _ltConfig.Tunnel,
            ["network"]          = _ltConfig.Network,
            ["console"]          = _ltConfig.Console,
            ["visual"]           = _ltConfig.Visual,
            ["selenium_version"] = _ltConfig.SeleniumVer,
            ["w3c"]              = true,
            ["seCdp"]            = true,   // exposes se:cdp URL so GetDevToolsSession() works for CDP UA override
        };

        if (_ltConfig.Accessibility)
        {
            lt["accessibility"]             = true;
            lt["accessibility.wcagVersion"] = _ltConfig.WcagVersion;
            lt["accessibility.bestPractice"]= _ltConfig.BestPractice;
            lt["accessibility.needsReview"] = _ltConfig.NeedsReview;
        }

        return lt;
    }
}
