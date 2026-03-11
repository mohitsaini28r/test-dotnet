using OpenQA.Selenium;
using OpenQA.Selenium.BiDi;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SeleniumSample
{
    public static class UserAgentHelper
    {
        public static async Task SetUserAgentAsync(IWebDriver driver, string userAgent)
        {
            try
            {
                ConsoleWriter.Information($"[UserAgentHelper] SetUserAgentAsync called with userAgent: {userAgent}");
                ConsoleWriter.Information($"[UserAgentHelper] Driver type: {driver.GetType().FullName}");

                var parameters = new Dictionary<string, object>
                {
                    { "userAgent", userAgent }
                };

                // Case 1: Local ChromiumDriver (Chrome/Edge) - Use CDP directly
                if (driver is ChromiumDriver chromiumDriver)
                {
                    ConsoleWriter.Information("[UserAgentHelper] Using ChromiumDriver CDP");
                    chromiumDriver.ExecuteCdpCommand("Network.enable", new Dictionary<string, object>());
                    chromiumDriver.ExecuteCdpCommand("Network.setUserAgentOverride", parameters);
                    ConsoleWriter.Information($"[UserAgentHelper] ✅ User Agent set via CDP: {userAgent}");
                    return;
                }

                // Case 2: RemoteWebDriver with webSocketUrl:true - use W3C BiDi Emulation module
                var caps = (driver as RemoteWebDriver)?.Capabilities;
                var wsUrl = caps?.GetCapability("webSocketUrl") as string;

                if (!string.IsNullOrEmpty(wsUrl))
                {
                    ConsoleWriter.Information("[UserAgentHelper] Using BiDi Emulation.SetUserAgentOverride");
                    var bidi = await driver.AsBiDiAsync();
                    await bidi.Emulation.SetUserAgentOverrideAsync(userAgent);
                    ConsoleWriter.Information($"[UserAgentHelper] ✅ User Agent set via BiDi Emulation: {userAgent}");
                    return;
                }

                // Case 3: RemoteWebDriver with seCdp:true - use IDevTools CDP session
                if (driver is IDevTools devToolsDriver)
                {
                    ConsoleWriter.Information("[UserAgentHelper] Using IDevTools CDP session");
                    var session = devToolsDriver.GetDevToolsSession();
                    await session.SendCommand("Network.enable", new System.Text.Json.Nodes.JsonObject());
                    await session.SendCommand("Network.setUserAgentOverride",
                        new System.Text.Json.Nodes.JsonObject { ["userAgent"] = userAgent });
                    ConsoleWriter.Information($"[UserAgentHelper] ✅ User Agent set via IDevTools: {userAgent}");
                    return;
                }

                ConsoleWriter.Error("[UserAgentHelper] ❌ No supported path found. Set webSocketUrl:true (BiDi) or seCdp:true (CDP).");
            }
            catch (Exception ex)
            {
                ConsoleWriter.Error($"[UserAgentHelper] ❌ Failed: {ex.Message}");
                if (ex.InnerException != null)
                    ConsoleWriter.Error($"[UserAgentHelper] Inner: {ex.InnerException.Message}");
            }
        }
    }
}
