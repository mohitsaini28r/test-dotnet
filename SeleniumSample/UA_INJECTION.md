# Runtime User-Agent Injection via CDP on LambdaTest Grid

## Problem

Standard Selenium tests set the User-Agent once at browser launch via `--user-agent` arg. There was no way to change it mid-test on a remote grid without restarting the browser session — which breaks test continuity.

## Goal

Change the User-Agent **during an active test session** on the LambdaTest grid, in the same browser tab, without creating a new session or tab.

---

## Why CDP over BiDi

| | CDP (`seCdp:true`) | BiDi (`webSocketUrl:true`) |
|---|---|---|
| UA override command | `Network.setUserAgentOverride` | No equivalent in W3C BiDi spec |
| LambdaTest support | Returns `se:cdp: "ws://..."` | `GetDevToolsSession()` fails |
| Selenium .NET maturity | Stable since Selenium 4.0 | Still evolving |
| Performance | One-time command, permanent | Intercepts every request |

---

## How It Works

### Step 1 — Capability: `seCdp: true`

```csharp
// WebDriverFactory.cs → BuildLtOptions()
["seCdp"] = true
```

LambdaTest returns `se:cdp: "ws://..."` in the session response.
Without this, the CDP WebSocket URL is never returned and nothing works.

### Step 2 — Open CDP session via `IDevTools`

```csharp
// UserAgentHelper.cs → Case 2 (RemoteWebDriver on grid)
var session = ((IDevTools)driver).GetDevToolsSession();
```

Selenium uses the `se:cdp` URL to open a WebSocket directly to Chrome's DevTools running on the LambdaTest VM.

### Step 3 — Send CDP commands

```csharp
await session.SendCommand("Network.enable", new JsonObject());
await session.SendCommand("Network.setUserAgentOverride",
    new JsonObject { ["userAgent"] = newUA });
```

- `Network.enable` — activates Chrome's network interception layer
- `Network.setUserAgentOverride` — patches the UA at the **network layer**, not just in JavaScript, so every subsequent HTTP request carries the new value

---

## Test Flow (MidTestUADemo)

```
Browser launch  →  MyCustomAgent/1.0        --user-agent arg, baked at startup
httpbin.org     →  MyCustomAgent/1.0        Phase 1: read and confirm launch UA
example.com     →  MyCustomAgent/1.0        Phase 2: simulate mid-test work
──── CDP inject via Network.setUserAgentOverride ────────────────────────────
httpbin.org     →  MidTestInjected/2.0      Phase 4: verify UA changed server-side
testmuai.com    →  MidTestInjected/2.0      Phase 5: real navigation with new UA
```

Same browser tab throughout — no new session, no new window.

---

## Verification

- `navigator.userAgent` (JS) reflects new value immediately after CDP command
- `httpbin.org/user-agent` confirms the HTTP request header changed server-side
- Full HAR captured via Chrome performance log — every request shows the exact UA used

---

## Key Files

| File | Role |
|---|---|
| `WebDriverFactory.cs` | Sets `seCdp:true` in LT:Options, enables performance log |
| `UserAgentHelper.cs` | CDP dispatcher — Case 1: local ChromiumDriver, Case 2: grid IDevTools |
| `MidTestUADemo.cs` | 5-phase demo orchestrating the UA flip mid-test |
| `HarCapture.cs` | Reads CDP performance log events and saves full HAR file |
| `new.yaml` | HyperExecute config — runs `--grid --demo`, posts HAR in logs |

---

## Running Locally

```bash
dotnet run -- --demo
```

Uses `ChromiumDriver.ExecuteCdpCommand()` directly — no WebSocket needed.

## Running on LambdaTest Grid via HyperExecute

```bash
./hyperexecute --user <username> --key <accesskey> --config new.yaml
```

Uses `IDevTools.GetDevToolsSession()` over the `se:cdp` WebSocket URL returned by LambdaTest.

---

## UA Lifecycle Summary

| When | Value | Set by |
|---|---|---|
| Browser launch | `MyCustomAgent/1.0` | `--user-agent` Chrome arg |
| After CDP inject | `MidTestInjected/2.0` | `Network.setUserAgentOverride` |
| JS `navigator.userAgent` | `MidTestInjected/2.0` | CDP network layer override |
| HTTP request headers | `MidTestInjected/2.0` | CDP network layer override |
| After `driver.Quit()` | gone | Session ends |
