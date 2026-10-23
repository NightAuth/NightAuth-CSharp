# NightAuth C# .NET SDK — Star us on GitHub ⭐

Official **NightAuth** C# / .NET SDK & Reference Client Suite (Modern Desktop UI in **WPF** and Interactive Terminal CLI). Protect your .NET software against piracy, reverse engineering, and tampering with cloud licensing, HWID locking, realtime chat, and memory streaming in minutes.

Official Portal: [https://nightauth.org](https://nightauth.org)  
Documentation: [https://nightauth.org/docs](https://nightauth.org/docs)  
Technical Architecture Benchmark: [NightAuth vs KeyAuth vs Auth.gg](https://nightauth.org/blog/nightauth-vs-keyauth-authgg-comparison)

---

**Languages / Idiomas / Langues / Sprachen:**  
[English](README.md) • [Español](lang/es.md) • [Français](lang/fr.md) • [Deutsch](lang/de.md) • [Italiano](lang/it.md) • [Português](lang/pt.md) • [Русский](lang/ru.md) • [Türkçe](lang/tr.md) • [Polski](lang/pl.md) • [中文](lang/zh.md)

---

> [!NOTE]
> ### 📺 Video Tutorials & System Setup Guides
> Official video tutorials and detailed step-by-step guides explaining how to configure applications, plans, subscriptions, cloud variables, webhooks, and licenses in the NightAuth Dashboard **will be published soon** on our official portal and YouTube channel.

---

## 📸 Screenshots

### Desktop Application (WPF Enterprise Dark Theme)

| Start / Authentication Screen | Dashboard & Subscription Management |
| :---: | :---: |
| ![NightAuth Desktop Login](image/desktop.png) | ![NightAuth Desktop Dashboard](image/desktop-dashboard.png) |

### Console Application (Interactive Terminal CLI)

| Terminal Login Screen | Interactive Console Dashboard |
| :---: | :---: |
| ![NightAuth Console Login](image/console.png) | ![NightAuth Console Dashboard](image/console-dashboard.png) |

---

## 🛡️ Recommended Security Practices

* **Native AOT & Compilation:** C# IL bytecode can be easily decompiled by tools like dnSpy or ILSpy. For production releases, publish using **Native AOT** (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`) or ahead-of-time compilation.
* **Enterprise Code Virtualization:** Protect compiled assemblies using commercial protectors and code virtualizers such as **VMProtect**, **Themida**, **Eazfuscator.NET**, or **Obfuscar**.
* **Memory Integrity & Anti-Debugging:** NightAuth ships a `SecurityProtections` helper with checks against system proxies, debuggers, and blacklisted analysis tools (`HTTPDebugger`, `CheatEngine`, `x64dbg`, `Wireshark`, `dnSpy`). By default only the system proxy check runs; invoke `CheckDebugger()` and `CheckBlacklistedProcesses()` manually at startup for full protection (see [SecurityProtections](#22-securityprotections)).
* **In-Memory Secure File Execution:** When downloading protected payloads or binaries with `NightAuthApp.GetFileAsync()`, **do not write bytes to physical disk**. Load and execute payloads directly in RAM (e.g. `Assembly.Load()` or reflective memory execution) and clear buffers (`CryptographicOperations.ZeroMemory()`) immediately after execution.

---

## 📦 Requirements & Installation

Requires **.NET 10.0 SDK** (or later).

Core dependencies (`NightAuth.SDK.csproj`):
* `BouncyCastle.Cryptography` (Modern cryptography primitives & X25519)
* `System.Management` (Hardware identifier collection)

```bash
dotnet build NightAuth.sln
```

---

## ⚙️ Configuration & Initialization

Define your application credentials from the **Manage Apps** tab of the NightAuth dashboard:

```csharp
public static api NightAuthApp = new api(
    name: "",
    apikey: "",
    version: "",
    token: null
);
```

### Initialize Cryptographic Handshake (`InitializeAsync`)

You must call `InitializeAsync()` prior to executing any other SDK functions. This establishes an ephemeral Diffie-Hellman (X25519) key exchange and an encrypted session:

```csharp
var init = await NightAuthApp.InitializeAsync();

if (!init.Success)
{
    Console.WriteLine($"Initialization Failed: {init.DisplayMessage}");
    Environment.Exit(1);
}
```

---

## 🚀 SDK Usage Guide

### 1. Check Session Validation & Blacklist Status

```csharp
// Validate active session status with the server
var check = await NightAuthApp.CheckAsync();
Console.WriteLine($"Session Status: {check.DisplayMessage}");

// Check if client HWID or IP address is blacklisted
var blacklist = await NightAuthApp.CheckBlacklistAsync();
if (blacklist.Success && blacklist.Payload["blacklisted"]?.GetValue<bool>() == true)
{
    Console.WriteLine("Access Denied: Machine or IP is blacklisted.");
    Environment.Exit(0);
}
```

### 2. Login with Username & Password (and 2FA)

```csharp
var result = await NightAuthApp.LoginAsync(username, password, totpCode);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Welcome back, {user.Username}!");
}
else
{
    Console.WriteLine($"Login failed: {result.DisplayMessage}");
}
```

### 3. Register with License Key

```csharp
var result = await NightAuthApp.RegisterAsync(username, password, licenseKey, email);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Registered successfully: {user.Username}");
}
else
{
    Console.WriteLine($"Registration failed: {result.DisplayMessage}");
}
```

### 4. Authenticate with License Key Only

```csharp
var result = await NightAuthApp.LicenseAsync(licenseKey);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine("License verified successfully. Access granted.");
}
else
{
    Console.WriteLine($"Access denied: {result.DisplayMessage}");
}
```

### 5. Upgrade Account / Claim License Key

```csharp
var result = await NightAuthApp.UpgradeAsync(username, newLicenseKey);

if (result.Success)
{
    Console.WriteLine("Subscription upgraded successfully.");
}
else
{
    Console.WriteLine($"Upgrade failed: {result.DisplayMessage}");
}
```

### 6. Reset Hardware ID (HWID Lock)

```csharp
var result = await NightAuthApp.ResetHwidAsync(username, password, totpCode);

if (result.Success)
{
    Console.WriteLine("HWID reset successfully. You can now log in on this machine.");
}
else
{
    Console.WriteLine($"HWID reset failed: {result.DisplayMessage}");
}
```

### 7. Forgot Password / Account Recovery

```csharp
// Request password reset email
var forgot = await NightAuthApp.ForgotPasswordAsync(email);
if (forgot.Success)
{
    Console.WriteLine("Password recovery email sent. Check your inbox.");
}

// Complete password reset with token
var reset = await NightAuthApp.ResetPasswordAsync(token, newPassword);
if (reset.Success)
{
    Console.WriteLine("Password reset successfully.");
}
```

### 8. User Profile Data (`result.ToUserData()`)

```csharp
UserData user = result.ToUserData();

Console.WriteLine($"Username:    {user.Username}");
Console.WriteLine($"Client IP:   {user.Ip}");
Console.WriteLine($"Hardware ID: {user.Hwid}");
Console.WriteLine($"Registered:  {user.CreatedAt}");
Console.WriteLine($"Last Login:  {user.LastLoginAt}");

foreach (var sub in user.Subscriptions)
{
    Console.WriteLine($"Plan: {sub.Subscription} | Expires: {sub.Expiry} | Key: {sub.Key}");
}
```

### 9. Cloud Variables (Global)

```csharp
var res = await NightAuthApp.GetGlobalVarAsync("server_motd");
string motd = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Cloud Variable: {motd}");
```

### 10. User Variables

```csharp
// Set a user variable
var setRes = await NightAuthApp.SetUserVarAsync("discord_tag", "user#0001");

// Retrieve a user variable
var getRes = await NightAuthApp.GetUserVarAsync("discord_tag");
string tag = getRes.Success ? getRes.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Stored Discord: {tag}");
```

### 11. Application Audit Logs

```csharp
await NightAuthApp.LogAsync("Suspicious memory modification attempt detected.");
```

### 12. Ban User & Blacklist Machine

```csharp
await NightAuthApp.BanAsync("Security tampering detected by Anti-Cheat.");
```

### 13. Server-Sided Webhooks

```csharp
var body = new JsonObject { ["content"] = "Client notification" };
var resp = await NightAuthApp.TriggerWebhookAsync("7kR0UedlVI", body);
Console.WriteLine($"Webhook Response: HTTP {(int)resp.StatusCode}");
```

### 14. Secure In-Memory File Download

```csharp
var res = await NightAuthApp.GetFileAsync("385624");

if (res.Success)
{
    byte[] fileBytes = Convert.FromBase64String(res.Payload["data"]?.ToString() ?? "");
    Console.WriteLine($"Downloaded {fileBytes.Length} bytes securely.");
    // Process or execute fileBytes directly in memory (e.g. Assembly.Load)
}
```

### 15. Realtime Chat Channels

```csharp
// Send a message to a channel
await NightAuthApp.SendChatMessageAsync("Hello community!", channel: "general");

// Fetch latest messages
var res = await NightAuthApp.GetChatMessagesAsync("general");
if (res.Success && res.Payload["messages"] is JsonArray messages)
{
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg?["author"]}]: {msg?["message"]}");
    }
}
```

### 16. Realtime Event Handlers (Server-Sent Events)

```csharp
using var cts = new CancellationTokenSource();

await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
{
    Console.WriteLine($"[EVENT] {evt}");
}
```

### 17. Two-Factor Authentication (2FA)

```csharp
// Setup TOTP 2FA
var setup = await NightAuthApp.Setup2FAAsync();
string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";

// Confirm with 6-digit Authenticator code
var conf = await NightAuthApp.Confirm2FAAsync("123456");

// Disable 2FA
var disable = await NightAuthApp.Disable2FAAsync(password, "123456");
```

### 18. Session Lifecycle & Logout

```csharp
// Heartbeat ping to keep session alive
await NightAuthApp.HeartbeatAsync();

// Revoke and terminate session
await NightAuthApp.LogoutAsync();
```

### 19. Sync API

Every authentication and management call also has a synchronous counterpart on `api` (e.g. `NightAuthApp`): `Init`, `Login`, `Register`, `License`, `Upgrade`, `Heartbeat`, `Check`, `Logout`, `ChangeUsername`, `ResetHwid`, `Ban`, `CheckBlacklist`, `VerifyFileHash`, `ForgotPassword`, `ResetPassword`, `Setup2FA`, `Confirm2FA`, `Disable2FA`, `GetGlobalVar`, `GetUserVar`, `SetUserVar`, `DownloadFile`, `GetChatMessages`, `SendChatMessage`, `Log`, `GetStats`, `GetOnlineUsers`, and `TriggerWebhook`. They block the current thread and report through `Response`/`User`/`App` instead of returning a `NightAuthResult`:

```csharp
NightAuthApp.Init();

if (NightAuthApp.Login(username, password, totpCode))
{
    Console.WriteLine($"Welcome back, {NightAuthApp.User.Username}!");
}
else
{
    Console.WriteLine($"Login failed: {NightAuthApp.Response.Message}");
}
```

Useful state exposed by the synchronous API: `Response`, `User` (alias `user_data`), `App` (alias `app_data`), `LatestVersion`, `ApiUrl`, `PendingDownloadUrl`, and `ToOptions()` to derive a `NightAuthOptions` from the current instance.

### 20. Realtime Event Handlers

`api` exposes high-level events fired by the realtime engine, plus explicit control over the background listener:

```csharp
NightAuthApp.OnSessionInvalidated += reason => { Console.WriteLine($"Session invalidated: {reason}"); Environment.Exit(0); };
NightAuthApp.OnAppBanned += reason => Console.WriteLine($"App banned: {reason}");
NightAuthApp.OnAppStatusChanged += status => Console.WriteLine($"App status changed: {status}");
NightAuthApp.OnVersionUpdated += version => Console.WriteLine($"New version available: {version}");
NightAuthApp.OnChatMessageReceived += (msg, channel) => Console.WriteLine($"[{channel}] {msg.Author}: {msg.Message}");

NightAuthApp.StartRealtimeEventListener(); // background SSE listener
// ... later
NightAuthApp.StopRealtimeEventListener();
```

For full control you can also consume the SSE stream manually with `ReadEventsAsync` (see section 16).

### 21. Standalone `NightAuthClient`

The modern async engine is available as an independent client, decoupled from the legacy `api` wrapper:

```csharp
var options = new NightAuthOptions(
    BaseUri: new Uri("https://nightauth.org"),
    Name: "my-app",
    ApiKey: "...",
    SigningPublicKey: NightAuthOptions.DefaultSigningPublicKey,
    SealedBoxPublicKey: NightAuthOptions.DefaultSealedBoxPublicKey)
{
    Version = "1.0",
    Token = null,
    RequestTimeout = TimeSpan.FromSeconds(30),
    MaxResponseBytes = 16 * 1024 * 1024,
    AllowInsecureLoopback = true,
    HardwareIdProvider = SecurityProtections.GetHardwareId
};

using var client = new NightAuthClient(options);
var init = await client.InitializeAsync();
var login = await client.LoginAsync(username, password);

if (login.Success)
{
    // NightAuthResult exposes Payload (JsonObject), RetryAfter and Error
    string tag = login.Payload["user"]?["username"]?.GetValue<string>() ?? "";
}
```

Additional async helpers available on both `NightAuthClient` and `api`: `ChangeUsernameAsync`, `GetUserVariablesAsync` (list all user variables), `VerifyFileHashAsync`, `GetStatsAsync`, `GetOnlineUsersAsync`, and three `TriggerWebhookAsync` overloads (JSON payload, JSON with custom `content`/`parameters`, and multipart upload with a file: `TriggerWebhookAsync(id, fileBytes, fileName, payload, content, parameters)`).

### 22. `SecurityProtections`

Static anti-tampering helpers. `RunSecurityChecks()` (invoked automatically by `Init`) only performs the system proxy check; call the other checks explicitly:

```csharp
SecurityProtections.CheckSystemProxy();          // terminate if a system HTTP proxy is active
SecurityProtections.CheckDebugger();             // terminate if a debugger (local or remote) is attached
SecurityProtections.CheckBlacklistedProcesses(); // scan for analysis tools (HTTPDebugger, Cheat Engine, x64dbg, Wireshark, dnSpy, ...)

string hwid = SecurityProtections.GetHardwareId(); // stable hardware identifier used for HWID locking
```

> [!NOTE]
> Set the environment variable `NIGHTAUTH_IGNORE_SECURITY=1` to skip the blacklisted-process scan during development/testing.

### 23. Signed Self-Updates

Unsigned auto-update (`ApplyUpdate`) is obsolete and disabled. Download and verify signed releases instead:

```csharp
// One-shot helper: fetches the manifest over HTTPS, verifies the Ed25519 signature
// against your pinned release key, rejects stale sequences, downloads the artifact
// and returns the installed file path.
string path = await SignedUpdateInstaller.DownloadAsync(
    manifestUri: new Uri("https://releases.example.com/manifest.json"),
    pinnedReleaseKeyHex: "YOUR_PINNED_ED25519_PUBLIC_KEY_HEX",
    application: "my-app",
    destination: "updates/my-app.exe");

// Or drive the pipeline manually:
var verified = SignedUpdate.Verify(envelopeJson, pinnedKeyHex, expectedApplication, minimumSequence);
Console.WriteLine($"Verified {verified.Application} v{verified.Version} (seq {verified.Sequence})");
await SignedUpdate.DownloadAsync(verified, "updates/my-app.exe");
```

---

## 🖥️ Run the Console and Desktop Clients

Launch either reference client from the `source` directory:

```bash
# Interactive Terminal Console Client
dotnet run --project console/NightAuth.Console.csproj

# Modern WPF Desktop Client (Windows)
dotnet run --project desktop/NightAuth.Desktop.csproj
```

---

---

## 🆚 Why NightAuth? (KeyAuth & Auth.gg Comparison)

| Feature | NightAuth | KeyAuth | Auth.gg |
| :--- | :---: | :---: | :---: |
| **Cryptography** | **End-to-End Curve25519 (X25519) + ChaCha20-Poly1305** | Static AES-CBC (Vulnerable to MITM & Proxy Interception) | Legacy HTTPS / Plain JSON |
| **Hardware ID (HWID)** | **Multi-Component Cryptographic Hash (Anti-Spoofing)** | Raw WMI Strings (Trivially Spoofed) | Simple Registry / MAC |
| **Edge Network Latency** | **< 35ms Global Anycast Edge Network** | Single-Origin VPS (Frequent Network Outages) | Unmaintained / High Latency |
| **Reseller System & Bots** | **Built-in Multi-Tier Credit Balances + Discord & Telegram Bots** | Manual API / Third-Party Scripts | Incomplete / Deprecated |
| **In-Memory Streaming** | **Zero-Disk Decryption with Direct Memory Execution & Buffer Wiping** | Disk Writes Required | Not Supported |
| **Internationalization** | **10 Languages Supported Out of the Box across Documentation & SDKs** | English Only | English Only |

Read the full architectural benchmark: [NightAuth vs KeyAuth vs Auth.gg](https://nightauth.org/blog/nightauth-vs-keyauth-authgg-comparison)

---

## 📄 License

This project is licensed under the **Elastic License 2.0**.

* You may not provide the software to third parties as a hosted or managed service.
* You may not move, change, disable, or circumvent the license key functionality.
* You may not alter, remove, or obscure any licensing or copyright notices.

See the [LICENSE](LICENSE) file for the full legal text.
