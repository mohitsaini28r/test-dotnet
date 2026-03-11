using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SeleniumSample
{
    /// <summary>
    /// Builds a HAR (HTTP Archive) file from Chrome's CDP performance log.
    /// Requires opts.SetLoggingPreference(LogType.Performance, LogLevel.All) in ChromeOptions.
    /// </summary>
    public static class HarCapture
    {
        public static void Save(IWebDriver driver, string outputPath)
        {
            Console.WriteLine($"[HAR] Collecting network events from performance log...");

            var rawLogs = driver.Manage().Logs.GetLog("performance");
            Console.WriteLine($"[HAR] Total performance log entries: {rawLogs.Count}");

            // Indexed by requestId
            var requests  = new Dictionary<string, JsonObject>();
            var responses = new Dictionary<string, JsonObject>();
            var timings   = new Dictionary<string, double>();

            foreach (var entry in rawLogs)
            {
                try
                {
                    var msg = JsonNode.Parse(entry.Message)?["message"];
                    if (msg == null) continue;

                    var method = msg["method"]?.GetValue<string>() ?? "";
                    var @params = msg["params"]?.AsObject();
                    if (@params == null) continue;

                    var requestId = @params["requestId"]?.GetValue<string>() ?? "";
                    if (string.IsNullOrEmpty(requestId)) continue;

                    switch (method)
                    {
                        case "Network.requestWillBeSent":
                            requests[requestId] = @params;
                            timings[requestId]  = new DateTimeOffset(entry.Timestamp).ToUnixTimeMilliseconds();
                            break;

                        case "Network.responseReceived":
                            responses[requestId] = @params;
                            break;
                    }
                }
                catch { /* skip malformed entries */ }
            }

            Console.WriteLine($"[HAR] Requests captured : {requests.Count}");
            Console.WriteLine($"[HAR] Responses captured: {responses.Count}");

            var entries = new JsonArray();

            foreach (var (id, req) in requests)
            {
                var request  = req["request"]?.AsObject();
                if (request == null) continue;

                var url      = request["url"]?.GetValue<string>() ?? "";
                var method   = request["method"]?.GetValue<string>() ?? "GET";
                var reqHdrs  = BuildHeaders(request["headers"]?.AsObject());
                var userAgent = reqHdrs.FirstOrDefault(h =>
                    h["name"]?.GetValue<string>()?.Equals("user-agent", StringComparison.OrdinalIgnoreCase) == true
                )?["value"]?.GetValue<string>() ?? "(none)";

                responses.TryGetValue(id, out var resp);
                var response  = resp?["response"]?.AsObject();
                var status    = response?["status"]?.GetValue<int>() ?? 0;
                var statusTxt = response?["statusText"]?.GetValue<string>() ?? "";
                var respHdrs  = BuildHeaders(response?["headers"]?.AsObject());
                var mimeType  = response?["mimeType"]?.GetValue<string>() ?? "";

                timings.TryGetValue(id, out var startMs);
                var started = startMs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds((long)startMs).ToString("o")
                    : DateTimeOffset.UtcNow.ToString("o");

                entries.Add(new JsonObject
                {
                    ["startedDateTime"] = started,
                    ["time"]            = -1,
                    ["_requestId"]      = id,
                    ["_userAgent"]      = userAgent,   // surfaced for quick inspection
                    ["request"] = new JsonObject
                    {
                        ["method"]      = method,
                        ["url"]         = url,
                        ["httpVersion"] = "HTTP/1.1",
                        ["headers"]     = reqHdrs,
                        ["queryString"] = new JsonArray(),
                        ["cookies"]     = new JsonArray(),
                        ["headersSize"] = -1,
                        ["bodySize"]    = -1,
                    },
                    ["response"] = new JsonObject
                    {
                        ["status"]      = status,
                        ["statusText"]  = statusTxt,
                        ["httpVersion"] = "HTTP/1.1",
                        ["headers"]     = respHdrs,
                        ["cookies"]     = new JsonArray(),
                        ["content"]     = new JsonObject
                        {
                            ["size"]     = 0,
                            ["mimeType"] = mimeType,
                        },
                        ["redirectURL"] = "",
                        ["headersSize"] = -1,
                        ["bodySize"]    = -1,
                    },
                    ["cache"]   = new JsonObject(),
                    ["timings"] = new JsonObject { ["send"] = 0, ["wait"] = 0, ["receive"] = 0 },
                });
            }

            var har = new JsonObject
            {
                ["log"] = new JsonObject
                {
                    ["version"] = "1.2",
                    ["creator"] = new JsonObject
                    {
                        ["name"]    = "SeleniumSample CDP",
                        ["version"] = "1.0",
                    },
                    ["entries"] = entries,
                }
            };

            var json = JsonSerializer.Serialize(har, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, json);

            Console.WriteLine($"[HAR] ✅ Saved {entries.Count} entries → {Path.GetFullPath(outputPath)}");
            Console.WriteLine();
            Console.WriteLine("[HAR] User-Agent per request:");
            foreach (var e in entries.OfType<JsonObject>())
            {
                var ua  = e["_userAgent"]?.GetValue<string>() ?? "";
                var url = e["request"]?["url"]?.GetValue<string>() ?? "";
                if (!url.StartsWith("data:"))
                    Console.WriteLine($"       {url.PadRight(60).Substring(0, Math.Min(60, url.Length))}  UA: {ua}");
            }
        }

        private static JsonArray BuildHeaders(JsonObject? headersObj)
        {
            var arr = new JsonArray();
            if (headersObj == null) return arr;
            foreach (var kv in headersObj)
                arr.Add(new JsonObject { ["name"] = kv.Key, ["value"] = kv.Value?.GetValue<string>() ?? "" });
            return arr;
        }
    }
}
