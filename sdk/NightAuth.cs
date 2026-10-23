

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NightAuth.ProtocolTests")]

namespace NightAuth;

#region Classic API (NightAuth.Api) & Data Models

#region Data & Response Models

public sealed class Response
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public sealed class UserData
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Hwid { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string LastLoginAt { get; set; } = string.Empty;
    public bool TwoFactorEnabled { get; set; }
    public List<SubscriptionData> Subscriptions { get; set; } = new();

    public string username => Username;
    public string email => Email;
    public string hwid => Hwid;
    public string ip => Ip;
    public string status => Status;
    public string Subscription => Subscriptions.Count > 0 ? Subscriptions[0].Subscription : string.Empty;
    public string subscription => Subscription;
}

public sealed class SubscriptionData
{
    public string Subscription { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Expiry { get; set; } = string.Empty;
    public long TimeLeft { get; set; }
}

public sealed class AppData
{
    public int TotalUsers { get; set; }
    public int OnlineUsers { get; set; }
    public int TotalKeys { get; set; }
    public string Version { get; set; } = string.Empty;
}

public sealed class AppStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int BannedUsers { get; set; }
    public int TotalLicenses { get; set; }
    public int UsedLicenses { get; set; }
    public int UnusedLicenses { get; set; }
    public string Version { get; set; } = string.Empty;
}

public sealed class ChatMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}

public sealed class TwoFactorSetupData
{
    public string Secret { get; set; } = string.Empty;
    public string QrDataUrl { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
}

public sealed class TwoFactorConfirmData
{
    public List<string> BackupCodes { get; set; } = new();
}

#endregion

public class Api : IDisposable, IAsyncDisposable
{
    #region Constants & Public Keys

    public const string DefaultApiUrl = "https://nightauth.org/api/v1";
    public const string ServerSigningPublicKey = "cdb2285db481666ce40003aadb874c8751dbeb9a0672a37f740dfe881ecdc742";
    public const string ServerSealedBoxPublicKey = "b773fc4f6cf1a0938e78e714c3d84cb386584bea24728923008471a6cd203213";
    public static readonly byte[] HkdfInfo = Encoding.UTF8.GetBytes("nightauth-v1-e2e");
    public static readonly byte[] SealInfo = Encoding.UTF8.GetBytes("nightauth-v1-init-seal");

    #endregion

    #region Properties & State

    public string Name { get; }
    public string ApiKey { get; }
    public string? Token { get; }
    public string Version { get; }
    private string _apiUrl = DefaultApiUrl;
    public string ApiUrl
    {
        get => _apiUrl;
        set
        {
            if (_isInitialized) throw new InvalidOperationException("Cannot change origin after initialization.");
            NightAuthClient.ValidateUri(new Uri(value), allowLoopback: true);
            _apiUrl = value.TrimEnd('/');
        }
    }

    public Response Response { get; private set; } = new();
    public UserData User { get; private set; } = new();
    public AppData App { get; private set; } = new();

    public string LatestVersion { get; private set; } = string.Empty;

    public string name => Name;
    public string apikey => ApiKey;
    public string version => Version;
    public string? token => Token;
    public Response response => Response;
    public UserData user_data => User;
    public AppData app_data => App;

public NightAuthClient Client => _client ??= new NightAuthClient(ToOptions());

    #region Unified Async Methods
    public Task<NightAuthResult> InitializeAsync(CancellationToken ct = default) => Client.InitializeAsync(ct);
    public Task<NightAuthResult> LoginAsync(string user, string pass, string? totp = null, CancellationToken ct = default) => Client.LoginAsync(user, pass, totp, ct);
    public Task<NightAuthResult> RegisterAsync(string user, string pass, string key, string email = "", CancellationToken ct = default) => Client.RegisterAsync(user, pass, key, email, ct);
    public Task<NightAuthResult> LicenseAsync(string key, CancellationToken ct = default) => Client.LicenseAsync(key, ct);
    public Task<NightAuthResult> LicenseAsync(string key, string? totp, CancellationToken ct = default) => Client.LicenseAsync(key, totp, ct);
    public Task<NightAuthResult> UpgradeAsync(string? user, string key, CancellationToken ct = default) => Client.UpgradeAsync(user, key, ct);
    public Task<NightAuthResult> CheckAsync(CancellationToken ct = default) => Client.CheckAsync(ct);
    public Task<NightAuthResult> HeartbeatAsync(CancellationToken ct = default) => Client.HeartbeatAsync(ct);
    public Task<NightAuthResult> LogoutAsync(CancellationToken ct = default) => Client.LogoutAsync(ct);
    public Task<NightAuthResult> ChangeUsernameAsync(string user, string pass, CancellationToken ct = default) => Client.ChangeUsernameAsync(user, pass, ct);
    public Task<NightAuthResult> ResetHwidAsync(string user, string pass, string? totp = null, CancellationToken ct = default) => Client.ResetHwidAsync(user, pass, totp, ct);
    public Task<NightAuthResult> ForgotPasswordAsync(string email, CancellationToken ct = default) => Client.ForgotPasswordAsync(email, ct);
    public Task<NightAuthResult> ResetPasswordAsync(string token, string pass, CancellationToken ct = default) => Client.ResetPasswordAsync(token, pass, ct);
    public Task<NightAuthResult> Setup2FAAsync(CancellationToken ct = default) => Client.Setup2FAAsync(ct);
    public Task<NightAuthResult> Confirm2FAAsync(string code, CancellationToken ct = default) => Client.Confirm2FAAsync(code, ct);
    public Task<NightAuthResult> Disable2FAAsync(string pass, string code, CancellationToken ct = default) => Client.Disable2FAAsync(pass, code, ct);
    public Task<NightAuthResult> GetGlobalVarAsync(string key, CancellationToken ct = default) => Client.GetGlobalVarAsync(key, ct);
    public Task<NightAuthResult> GetUserVarAsync(string key, CancellationToken ct = default) => Client.GetUserVarAsync(key, ct);
    public Task<NightAuthResult> GetUserVariablesAsync(CancellationToken ct = default) => Client.GetUserVariablesAsync(ct);
    public Task<NightAuthResult> SetUserVarAsync(string key, string val, CancellationToken ct = default) => Client.SetUserVarAsync(key, val, ct);
    public Task<NightAuthResult> GetChatMessagesAsync(string channel = "general", CancellationToken ct = default) => Client.GetChatMessagesAsync(channel, ct);
    public Task<NightAuthResult> GetChatMessagesAsync(string channel, long? before, bool? markRead, CancellationToken ct = default) => Client.GetChatMessagesAsync(channel, before, markRead, ct);
    public Task<NightAuthResult> SendChatMessageAsync(string msg, string channel = "general", CancellationToken ct = default) => Client.SendChatMessageAsync(msg, channel, ct);
    public Task<NightAuthResult> LogAsync(string msg, CancellationToken ct = default) => Client.LogAsync(msg, ct);
    public Task<NightAuthResult> BanAsync(string? reason = null, CancellationToken ct = default) => Client.BanAsync(reason, ct);
    public Task<NightAuthResult> CheckBlacklistAsync(CancellationToken ct = default) => Client.CheckBlacklistAsync(ct);
    public Task<NightAuthResult> VerifyFileHashAsync(string hash, CancellationToken ct = default) => Client.VerifyFileHashAsync(hash, ct);
    public Task<NightAuthResult> GetFileAsync(string fileId, CancellationToken ct = default) => Client.GetFileAsync(fileId, ct);
    public Task<NightAuthResult> TriggerWebhookAsync(string hookId, JsonObject body, CancellationToken ct = default) => Client.TriggerWebhookAsync(hookId, body, ct);
    public Task<NightAuthResult> GetStatsAsync(CancellationToken ct = default) => Client.GetStatsAsync(ct);
    public Task<NightAuthResult> GetOnlineUsersAsync(CancellationToken ct = default) => Client.GetOnlineUsersAsync(ct);
    public IAsyncEnumerable<JsonObject> ReadEventsAsync(string? lastEventId = null, CancellationToken ct = default) => Client.ReadEventsAsync(lastEventId, ct);
    public async ValueTask DisposeAsync()
    {
        Dispose();
        if (_client != null) await _client.DisposeAsync().ConfigureAwait(false);
    }
    #endregion
    private NightAuthClient? _client;

    public NightAuthOptions ToOptions() => new(
        new Uri(ApiUrl.EndsWith("/api/v1") ? ApiUrl[..^7] : ApiUrl),
        Name,
        ApiKey,
        ServerSigningPublicKey,
        ServerSealedBoxPublicKey)
    {
        Version = Version,
        Token = Token,
        AllowInsecureLoopback = ApiUrl.StartsWith("http://localhost") || ApiUrl.StartsWith("http://127.0.0.1")
    };

    public string PendingDownloadUrl { get; private set; } = string.Empty;

    public event Action<string>? OnSessionInvalidated;
    public event Action<string>? OnAppBanned;
    public event Action<string>? OnAppStatusChanged;
    public event Action<string>? OnVersionUpdated;
    public event Action<ChatMessage, string>? OnChatMessageReceived;

    private string? _sessionKey;
    private byte[]? _sharedKey;
    private bool _isInitialized;
    private readonly HttpClient _httpClient;
    private readonly Action _securityCheck;
    private CancellationTokenSource? _eventsCts;
    private Thread? _eventsThread;
    private bool _disposed;

    #endregion

    #region Constructor & Initialization

    public Api(string name, string apiKey, string version = "1.0", string? token = null, string? apiUrl = null,
               HttpMessageHandler? httpHandler = null, Action? securityCheck = null)
    {

        Name = name;
        ApiKey = apiKey;
        Version = version ?? "1.0";
        Token = token;
        if (!string.IsNullOrWhiteSpace(apiUrl)) ApiUrl = apiUrl.TrimEnd('/');

        _securityCheck = securityCheck ?? SecurityProtections.RunSecurityChecks;

        HttpMessageHandler resolvedHandler = httpHandler ?? new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true,
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                {
                    if (errors == SslPolicyErrors.None) return true;
                    return false;
                }
            }
        };

        _httpClient = new HttpClient(resolvedHandler) { Timeout = TimeSpan.FromSeconds(20), MaxResponseContentBufferSize = 16 * 1024 * 1024 };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NightAuth-CSharp-SDK/1.0");

        _securityCheck();
    }

    public void Init()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        StopRealtimeEventListener();
        _isInitialized = false;
        User = new UserData();
        if (_sharedKey != null) CryptographicOperations.ZeroMemory(_sharedKey);
        _sharedKey = null;
        _securityCheck();

        var random = new SecureRandom();
        var keyGen = new X25519KeyPairGenerator();
        keyGen.Init(new X25519KeyGenerationParameters(random));

        var clientPair = keyGen.GenerateKeyPair();
        var clientPriv = (X25519PrivateKeyParameters)clientPair.Private;
        var clientPub = (X25519PublicKeyParameters)clientPair.Public;

        byte[] clientPubBytes = clientPub.GetEncoded();
        string k1Hex = Convert.ToHexString(clientPubBytes).ToLowerInvariant();

        _sessionKey = Guid.NewGuid().ToString("N");

        var payload = new JsonObject
        {
            ["apikey"] = ApiKey,
            ["name"] = Name,
            ["version"] = Version,
            ["id"] = _sessionKey,
            ["k1"] = k1Hex
        };

        string sealedPayloadB64 = CryptoBoxSeal(payload.ToJsonString(), ServerSealedBoxPublicKey);
        var requestBody = new JsonObject { ["p"] = sealedPayloadB64 };

        using var httpResponse = PostRaw("/init", requestBody.ToJsonString());
        if (!httpResponse.IsSuccessStatusCode)
        {
            SetError($"Initialization failed: HTTP {httpResponse.StatusCode}");
            return;
        }

        string respContent = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var json = JsonNode.Parse(respContent)?.AsObject();
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string initErr = json?["error"]?.GetValue<string>() ?? "init_failed";
            SetError(
                json?["message"]?.GetValue<string>() ?? initErr ?? "Failed to initialize handshake",
                initErr);
            if (initErr == "version_mismatch") CaptureUpdateInfo(json);
            return;
        }

        string k2Hex = json["k2"]?.GetValue<string>() ?? string.Empty;
        string vkHex = json["vk"]?.GetValue<string>() ?? ServerSigningPublicKey;
        long ts = json["ts"]?.GetValue<long>() ?? 0;
        string sigB64 = json["sig"]?.GetValue<string>() ?? string.Empty;

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (ts < now - 90_000 || ts > now + 90_000 || !VerifyServerSignature(k2Hex, ServerSigningPublicKey, ts, sigB64))
        {
            SetError("Security integrity breach: Invalid server signature.");
            return;
        }

        byte[] serverPubBytes = Convert.FromHexString(k2Hex);
        var serverPub = new X25519PublicKeyParameters(serverPubBytes, 0);

        byte[] secret = new byte[32];
        clientPriv.GenerateSecret(serverPub, secret, 0);

        _sharedKey = HkdfExtractAndExpand(secret, HkdfInfo, 32);
        CryptographicOperations.ZeroMemory(secret);
        _isInitialized = true;

        StartRealtimeEventListener();
        SetSuccess("Initialized successfully.");
    }

    #endregion

    #region Authentication Operations

    public bool Login(string username, string password, string? totpCode = null)
    {
        CheckInit();
        _securityCheck();

        var body = CreateBasePayload();
        body["version"] = Version;
        body["username"] = username;
        body["password"] = password;
        body["hwid"] = SecurityProtections.GetHardwareId();
        if (!string.IsNullOrEmpty(totpCode)) body["totpCode"] = totpCode;

        var json = SendEncrypted("/login", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Login failed";
            string err = json?["error"]?.GetValue<string>() ?? "login_failed";
            SetError(msg, err);
            if (err == "version_mismatch") CaptureUpdateInfo(json);
            return false;
        }

        PopulateUserData(json);
        StartRealtimeEventListener();
        SetSuccess("Logged in successfully.");
        return true;
    }

    public bool Register(string username, string password, string key, string email = "")
    {
        CheckInit();
        _securityCheck();

        var body = CreateBasePayload();
        body["version"] = Version;
        body["username"] = username;
        body["password"] = password;
        body["key"] = key;
        body["email"] = email;
        body["hwid"] = SecurityProtections.GetHardwareId();

        var json = SendEncrypted("/register", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Registration failed";
            string err = json?["error"]?.GetValue<string>() ?? "registration_failed";
            SetError(msg, err);
            if (err == "version_mismatch") CaptureUpdateInfo(json);
            return false;
        }

        PopulateUserData(json);
        StartRealtimeEventListener();
        SetSuccess("Registered successfully.");
        return true;
    }

    public bool License(string key)
    {
        CheckInit();
        _securityCheck();

        var body = CreateBasePayload();
        body["version"] = Version;
        body["key"] = key;
        body["hwid"] = SecurityProtections.GetHardwareId();

        var json = SendEncrypted("/license", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "License activation failed";
            string err = json?["error"]?.GetValue<string>() ?? "license_failed";
            SetError(msg, err);
            if (err == "version_mismatch") CaptureUpdateInfo(json);
            return false;
        }

        PopulateUserData(json);
        StartRealtimeEventListener();
        SetSuccess("License verified successfully.");
        return true;
    }

    public bool Upgrade(string username, string key)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["username"] = username;
        body["key"] = key;

        var json = SendEncrypted("/upgrade", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Upgrade failed";
            string err = json?["error"]?.GetValue<string>() ?? "upgrade_failed";
            SetError(msg, err);
            return false;
        }

        SetSuccess("Subscription upgraded successfully.");
        return true;
    }

    public bool Heartbeat()
    {
        CheckInit();
        var body = CreateBasePayload();
        body["version"] = Version;

        var json = SendEncrypted("/heartbeat", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Heartbeat rejected";
            string err = json?["error"]?.GetValue<string>() ?? "heartbeat_failed";
            SetError(msg, err);
            if (err == "version_mismatch") CaptureUpdateInfo(json);
            if (err == "session_invalid" || err == "session_not_validated" || err == "unauthenticated" || err == "version_mismatch")
            {
                StopRealtimeEventListener();
                OnSessionInvalidated?.Invoke(
                    err == "version_mismatch"
                        ? $"Your build is outdated (server requires v{LatestVersion}). Session terminated."
                        : "Session terminated or invalid (Heartbeat failure)");
            }
            return false;
        }

        SetSuccess("Session alive.");
        return true;
    }

    public bool Check()
    {
        CheckInit();
        var body = CreateBasePayload();
        body["version"] = Version;

        var json = SendEncrypted("/check", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Session invalid";
            string err = json?["error"]?.GetValue<string>() ?? "check_failed";
            SetError(msg, err);
            if (err == "version_mismatch") CaptureUpdateInfo(json);
            if (err == "session_invalid" || err == "no_active_subscription" || err == "subscription_paused" || err == "unauthenticated" || err == "version_mismatch")
            {
                StopRealtimeEventListener();
                OnSessionInvalidated?.Invoke($"Session check rejected: {err}");
            }
            return false;
        }

        User.Subscriptions.Clear();
        if (json!["subscriptions"] is JsonArray subArray)
        {
            foreach (var item in subArray)
            {
                if (item == null) continue;
                long tl = item["timeleft"]?.GetValue<long>() ?? item["timeLeft"]?.GetValue<long>() ?? 0;
                User.Subscriptions.Add(new SubscriptionData
                {
                    Subscription = (item["subscription"] ?? "").ToString(),
                    Key = (item["key"] ?? "").ToString(),
                    Expiry = (item["expiry"] ?? item["expiresAt"] ?? "").ToString(),
                    TimeLeft = tl
                });
            }
        }
        if (json!["twoFactorEnabled"] != null)
        {
            User.TwoFactorEnabled = json!["twoFactorEnabled"]!.GetValue<bool>();
        }

        SetSuccess("Session valid.");
        return true;
    }

    public bool Logout()
    {
        CheckInit();
        StopRealtimeEventListener();
        var body = CreateBasePayload();

        var json = SendEncrypted("/logout", body);
        if (json?["success"]?.GetValue<bool>() != true)
        {
            SetError("Logout failed", json?["error"]?.GetValue<string>());
            return false;
        }
        User = new UserData();
        _isInitialized = false;
        if (_sharedKey != null) CryptographicOperations.ZeroMemory(_sharedKey);
        _sharedKey = null;
        _sessionKey = null;
        SetSuccess("Logged out successfully.");
        return true;
    }

    #endregion

    #region User Profile & Security Operations

    public bool ChangeUsername(string newUsername, string password)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["newUsername"] = newUsername;
        body["password"] = password;

        var json = SendEncrypted("/change-username", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Failed to change username";
            string err = json?["error"]?.GetValue<string>() ?? "change_username_failed";
            SetError(msg, err);
            return false;
        }

        User.Username = newUsername;

        StopRealtimeEventListener();
        SetSuccess("Username changed successfully.");
        return true;
    }

    public bool ResetHwid(string username, string password, string? totpCode = null)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["username"] = username;
        body["password"] = password;
        if (!string.IsNullOrEmpty(totpCode)) body["totpCode"] = totpCode;

        var json = SendEncrypted("/hwid-reset", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "HWID reset failed";
            string err = json?["error"]?.GetValue<string>() ?? "hwid_reset_failed";
            SetError(msg, err);
            return false;
        }

        StopRealtimeEventListener();
        SetSuccess(json["message"]?.GetValue<string>() ?? "HWID reset successfully.");
        return true;
    }

    public bool Ban(string? reason = null)
    {
        CheckInit();
        var body = CreateBasePayload();
        if (!string.IsNullOrEmpty(reason)) body["reason"] = reason;

        var json = SendEncrypted("/ban", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Failed to self-report ban";
            string err = json?["error"]?.GetValue<string>() ?? "ban_failed";
            SetError(msg, err);
            return false;
        }

        SetSuccess("Account banned.");
        return true;
    }

    public bool CheckBlacklist(string? hwid = null)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["hwid"] = hwid ?? SecurityProtections.GetHardwareId();

        var json = SendEncrypted("/check-blacklist", body);
        return json?["blacklisted"]?.GetValue<bool>() == true;
    }

    public bool VerifyFileHash(string hash)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["hash"] = hash;

        var json = SendEncrypted("/verify-hash", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Hash check failed";
            string err = json?["error"]?.GetValue<string>() ?? "verify_hash_failed";
            SetError(msg, err);
            return false;
        }

        return json["verified"]?.GetValue<bool>() == true;
    }

    public bool ForgotPassword(string email)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["email"] = email;

        var json = SendEncrypted("/forgot-password", body);
        return json?["success"]?.GetValue<bool>() == true;
    }

    public bool ResetPassword(string token, string newPassword)
    {
        var body = new JsonObject
        {
            ["token"] = token,
            ["newPassword"] = newPassword
        };

        var res = PostRaw("/reset-password", body.ToJsonString());
        string text = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var json = JsonNode.Parse(text)?.AsObject();
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Failed to reset password";
            string err = json?["error"]?.GetValue<string>() ?? "reset_password_failed";
            SetError(msg, err);
            return false;
        }

        SetSuccess("Password has been reset.");
        return true;
    }

    #endregion

    #region Two-Factor Authentication (2FA)

    public TwoFactorSetupData? Setup2FA()
    {
        CheckInit();
        var body = CreateBasePayload();

        var json = SendEncrypted("/2fa/setup", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "2FA setup failed";
            string err = json?["error"]?.GetValue<string>() ?? "2fa_setup_failed";
            SetError(msg, err);
            return null;
        }

        return new TwoFactorSetupData
        {
            Secret = json["secret"]?.GetValue<string>() ?? "",

            QrDataUrl = json["qr"]?.GetValue<string>() ?? "",
            Uri = json["uri"]?.GetValue<string>() ?? ""
        };
    }

    public TwoFactorConfirmData? Confirm2FA(string code)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["code"] = code;

        var json = SendEncrypted("/2fa/confirm", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "2FA confirmation failed";
            string err = json?["error"]?.GetValue<string>() ?? "2fa_confirm_failed";
            SetError(msg, err);
            return null;
        }

        var result = new TwoFactorConfirmData();
        if (json["backupCodes"] is JsonArray arr)
        {
            foreach (var b in arr)
            {
                if (b != null) result.BackupCodes.Add(b.ToString());
            }
        }
        return result;
    }

    public bool Disable2FA(string password, string code)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["password"] = password;
        body["code"] = code;

        var json = SendEncrypted("/2fa/disable", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Failed to disable 2FA";
            string err = json?["error"]?.GetValue<string>() ?? "2fa_disable_failed";
            SetError(msg, err);
            return false;
        }

        SetSuccess("2FA disabled.");
        return true;
    }

    #endregion

    #region Cloud Variables & File Storage

    public string GetGlobalVar(string key)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["varName"] = key;

        var json = SendEncrypted("/global-var", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            string msg = json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Variable not found";
            string err = json?["error"]?.GetValue<string>() ?? "var_not_found";
            SetError(msg, err);
            return string.Empty;
        }

        return json["value"]?.GetValue<string>() ?? string.Empty;
    }

    public string GetUserVar(string key)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["key"] = key;

        var json = SendEncrypted("/user-vars/get", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            SetError(
                json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "User variable not found",
                json?["error"]?.GetValue<string>());
            return string.Empty;
        }

        if (json["value"] != null) return json["value"]!.GetValue<string>();
        if (json["variables"] is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item?["key"]?.ToString() == key)
                {
                    return item["value"]?.ToString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

    public bool SetUserVar(string key, string value)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["key"] = key;
        body["value"] = value;

        var json = SendEncrypted("/user-vars/set", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            SetError(
                json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Failed to set variable",
                json?["error"]?.GetValue<string>());
            return false;
        }

        SetSuccess("User variable saved.");
        return true;
    }

    public byte[]? DownloadFile(string fileId)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["scope"] = "file";
        body["resourceId"] = fileId;
        var ticket = SendEncrypted("/access-ticket", body);
        if (ticket?["success"]?.GetValue<bool>() != true)
        {
            SetError("File access denied", ticket?["error"]?.GetValue<string>());
            return null;
        }
        var path = $"{ApiUrl}{FileDownloads.DownloadPath(ticket["ticket"]!.GetValue<string>())}";
        using var response = _httpClient.GetAsync(path).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            SetError("File download failed or requires an external download handler", "file_download_failed");
            return null;
        }
        var json = JsonNode.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult())?.AsObject();
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            SetError(
                json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "File download failed",
                json?["error"]?.GetValue<string>());
            return null;
        }

        string fileB64 = json["data"]?.GetValue<string>() ?? string.Empty;
        try
        {
            return FileDownloads.Decompress(Convert.FromBase64String(fileB64));
        }
        catch (FileTooLargeException)
        {
            SetError("File content exceeds the 64 MiB limit", "download_too_large");
            return null;
        }
        catch (Exception e) when (e is FormatException or InvalidDataException or IOException)
        {
            SetError("File content could not be decompressed", "download_decode_failed");
            return null;
        }
    }

    #endregion

    #region Real-Time Chat & Telemetry

    public List<ChatMessage> GetChatMessages(string channel = "general")
    {
        CheckInit();
        var body = CreateBasePayload();
        body["room"] = channel;

        var json = SendEncrypted("/chat/get", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            SetError(
                json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Failed to fetch messages",
                json?["error"]?.GetValue<string>());
            return new List<ChatMessage>();
        }

        var list = new List<ChatMessage>();
        if (json["messages"] is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item == null) continue;
                list.Add(new ChatMessage
                {
                    Id = item["id"]?.ToString() ?? "",
                    Author = item["author"]?.ToString() ?? "",
                    Message = item["content"]?.ToString() ?? item["message"]?.ToString() ?? "",
                    Timestamp = item["timestamp"]?.ToString() ?? item["createdAt"]?.ToString() ?? ""
                });
            }
        }
        return list;
    }

    public bool SendChatMessage(string message, string channel = "general")
    {
        CheckInit();
        var body = CreateBasePayload();
        body["room"] = channel;
        body["content"] = message;

        var json = SendEncrypted("/chat/send", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            SetError(
                json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Failed to send chat message",
                json?["error"]?.GetValue<string>());
            return false;
        }

        SetSuccess("Message sent.");
        return true;
    }

    public bool Log(string message)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["message"] = message;

        var json = SendEncrypted("/log", body);
        return json?["success"]?.GetValue<bool>() == true;
    }

    public AppStats? GetStats()
    {
        CheckInit();
        var body = CreateBasePayload();

        var json = SendEncrypted("/stats", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            SetError(
                json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Failed to retrieve stats",
                json?["error"]?.GetValue<string>());
            return null;
        }

        var users = json["users"]?.AsObject();
        var licenses = json["licenses"]?.AsObject();

        return new AppStats
        {
            Version = json["version"]?.GetValue<string>() ?? Version,
            TotalUsers = users?["total"]?.GetValue<int>() ?? 0,
            ActiveUsers = users?["active"]?.GetValue<int>() ?? 0,
            BannedUsers = users?["banned"]?.GetValue<int>() ?? 0,
            TotalLicenses = licenses?["total"]?.GetValue<int>() ?? 0,
            UsedLicenses = licenses?["used"]?.GetValue<int>() ?? 0,
            UnusedLicenses = licenses?["notUsed"]?.GetValue<int>() ?? 0
        };
    }

    public List<string> GetOnlineUsers()
    {
        CheckInit();
        var body = CreateBasePayload();

        var json = SendEncrypted("/online-users", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            SetError(
                json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Failed to fetch online users",
                json?["error"]?.GetValue<string>());
            return new List<string>();
        }

        var list = new List<string>();
        if (json["users"] is JsonArray arr)
        {
            foreach (var u in arr)
            {
                if (u != null) list.Add(u.ToString());
            }
        }
        return list;
    }

    public string? TriggerWebhook(string webId, JsonObject? customPayload = null)
    {
        CheckInit();
        var body = CreateBasePayload();
        body["webId"] = webId;
        if (customPayload != null) body["payload"] = customPayload;

        var json = SendEncrypted("/webhook", body);
        if (json == null || json["success"]?.GetValue<bool>() != true)
        {
            SetError(
                json?["message"]?.GetValue<string>() ?? json?["error"]?.GetValue<string>() ?? "Webhook trigger failed",
                json?["error"]?.GetValue<string>());
            return null;
        }

        return json["data"]?.ToString() ?? "";
    }

    #endregion

    #region Real-Time Server-Sent Events (SSE) Engine

    public void StartRealtimeEventListener()
    {
        StopRealtimeEventListener();

        _eventsCts = new CancellationTokenSource();
        var ct = _eventsCts.Token;

        _eventsThread = new Thread(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var ticketPayload = CreateBasePayload();
                    ticketPayload["scope"] = "events";

                    var ticketJson = SendEncrypted("/access-ticket", ticketPayload);
                    if (ticketJson == null || ticketJson["success"]?.GetValue<bool>() != true)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    string ticket = ticketJson["ticket"]?.GetValue<string>() ?? "";
                    if (string.IsNullOrEmpty(ticket))
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    string sseUrl = $"{ApiUrl}/events?ticket={Uri.EscapeDataString(ticket)}";
                    var sseHandler = new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
                        EnableMultipleHttp2Connections = true,
                        SslOptions = new SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                            RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                            {
                                if (errors == SslPolicyErrors.None) return true;
                                return false;
                            }
                        }
                    };

                    using var sseClient = new HttpClient(sseHandler) { Timeout = Timeout.InfiniteTimeSpan };
                    using var req = new HttpRequestMessage(HttpMethod.Get, sseUrl);
                    req.Headers.Add("User-Agent", "NightAuth-CSharp-SDK/1.0");

                    using var resp = sseClient.Send(req, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (!resp.IsSuccessStatusCode)
                    {
                        Thread.Sleep(3000);
                        continue;
                    }

                    using var stream = resp.Content.ReadAsStream(ct);
                    using var reader = new StreamReader(stream, Encoding.UTF8);

                    while (!reader.EndOfStream && !ct.IsCancellationRequested)
                    {
                        string? line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
                        {
                            string data = line.Substring(6).Trim();
                            HandleRealtimeEvent(data);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    if (ct.IsCancellationRequested) break;
                    Thread.Sleep(3000);
                }
            }
        })
        {
            IsBackground = true,
            Name = "NightAuth_RealtimeEventsThread"
        };

        _eventsThread.Start();
    }

    public void StopRealtimeEventListener()
    {
        try
        {
            _eventsCts?.Cancel();
            _eventsCts?.Dispose();
            _eventsCts = null;
            _eventsThread = null;
        }
        catch { }
    }

    private void HandleRealtimeEvent(string jsonString)
    {
        try
        {
            var node = JsonNode.Parse(jsonString)?.AsObject();
            if (node == null) return;

            string type = node["type"]?.GetValue<string>() ?? "";

            switch (type)
            {
                case "session-invalid":
                    StopRealtimeEventListener();
                    User = new UserData();
                    OnSessionInvalidated?.Invoke("Your session has been terminated by administrator or system.");
                    break;

                case "app-banned":
                    StopRealtimeEventListener();
                    User = new UserData();
                    string reason = node["reason"]?.GetValue<string>() ?? "Application has been banned.";
                    OnAppBanned?.Invoke(reason);
                    break;

                case "app-paused":
                case "app-disabled":
                    StopRealtimeEventListener();
                    User = new UserData();
                    OnAppStatusChanged?.Invoke($"Application status changed to: {type}");
                    break;

                case "version":
                    string newVer = node["version"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(newVer))
                    {
                        App.Version = newVer;
                        LatestVersion = newVer;
                        PendingDownloadUrl = node["downloadUrl"]?.GetValue<string>() ?? PendingDownloadUrl;
                        OnVersionUpdated?.Invoke(newVer);

                        if (!string.IsNullOrEmpty(Version) && newVer != Version)
                        {
                            StopRealtimeEventListener();
                            User = new UserData();
                            OnSessionInvalidated?.Invoke($"Your build is outdated (server now requires v{newVer}). Session terminated.");
                        }
                    }
                    break;

                case "chat-message":
                    string room = node["room"]?.GetValue<string>() ?? "general";
                    var msg = new ChatMessage
                    {
                        Id = node["id"]?.ToString() ?? "",
                        Author = node["author"]?.GetValue<string>() ?? "unknown",
                        Message = node["content"]?.GetValue<string>() ?? "",
                        Timestamp = node["createdAt"]?.GetValue<string>() ?? DateTime.UtcNow.ToString("o")
                    };
                    OnChatMessageReceived?.Invoke(msg, room);
                    break;
            }
        }
        catch { }
    }

    #endregion

    #region Internal Transportation & Cryptography

    private JsonObject CreateBasePayload()
    {
        var obj = new JsonObject
        {
            ["apikey"] = ApiKey,
            ["name"] = Name
        };
        if (!string.IsNullOrEmpty(Token)) obj["token"] = Token;
        return obj;
    }

    private void CheckInit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_isInitialized || _sharedKey == null || string.IsNullOrEmpty(_sessionKey))
            throw new InvalidOperationException("NightAuth must be initialized before calling API methods. Call app.Init() first.");
    }

    private JsonObject? SendEncrypted(string path, JsonObject payload)
    {
        string rid = Guid.NewGuid().ToString("N");
        long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        string targetPath = new Uri($"{ApiUrl}{path}").AbsolutePath;
        string aad = $"POST:{targetPath}:{_sessionKey}:{rid}:{ts}";
        string jsonPlain = payload.ToJsonString();
        var (ctB64, nonceB64) = AesGcmEncrypt(_sharedKey!, jsonPlain, aad);

        var envelope = new JsonObject
        {
            ["id"] = _sessionKey,
            ["ct"] = ctB64,
            ["n"] = nonceB64,
            ["rid"] = rid,
            ["ts"] = ts
        };

        using var httpResp = PostRaw(path, envelope.ToJsonString());
        string respRaw = httpResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        var encResp = JsonNode.Parse(respRaw)?.AsObject();
        if (encResp != null && encResp["ct"] != null && encResp["n"] != null)
        {
            string resCt = encResp["ct"]!.GetValue<string>();
            string resN = encResp["n"]!.GetValue<string>();
            string resRid = encResp["rid"]?.GetValue<string>() ?? "";
            long resTs = encResp["ts"]?.GetValue<long>() ?? 0;
            if (resRid != rid || resTs != ts)
                throw new CryptographicException("Response does not belong to this request.");

            string resAad = $"v1:res:{_sessionKey}:{resRid}:{resTs}";
            string decryptedPlain = AesGcmDecrypt(_sharedKey!, resCt, resN, resAad);

            return JsonNode.Parse(decryptedPlain)?.AsObject();
        }

        if (!httpResp.IsSuccessStatusCode)
        {
            return new JsonObject { ["success"] = false, ["error"] = $"HTTP {httpResp.StatusCode}" };
        }

        throw new CryptographicException("Expected an authenticated response.");
    }

    private HttpResponseMessage PostRaw(string path, string jsonBody)
    {
        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        return _httpClient.PostAsync($"{ApiUrl}{path}", content).GetAwaiter().GetResult();
    }

    internal static (string ct, string n) AesGcmEncrypt(byte[] key, string plaintext, string aad) => V1Crypto.AesGcmEncrypt(key, plaintext, aad);
    internal static string AesGcmDecrypt(byte[] key, string ct, string nonce, string aad) => V1Crypto.AesGcmDecrypt(key, ct, nonce, aad);
    internal static bool VerifyServerSignature(string k2, string vk, long ts, string signature) => V1Crypto.VerifyServerSignature(k2, vk, ts, signature);
    internal static string CryptoBoxSeal(string plaintext, string key) => V1Crypto.CryptoBoxSeal(plaintext, key);
    internal static byte[] HkdfExtractAndExpand(byte[] key, byte[] info, int length) => V1Crypto.HkdfExtractAndExpand(key, info, length);

    private void PopulateUserData(JsonObject json)
    {
        var u = (json["user"] ?? json["info"])?.AsObject();
        if (u == null) return;

        User.Username = (u["username"] ?? "").ToString();
        User.Email = (u["email"] ?? "").ToString();
        User.Hwid = (u["hwid"] ?? "").ToString();
        User.Ip = (u["ip"] ?? "").ToString();
        User.Status = (u["status"] ?? "active").ToString();
        User.CreatedAt = (u["createdAt"] ?? u["createdate"] ?? "").ToString();
        User.LastLoginAt = (u["lastLoginAt"] ?? u["lastlogin"] ?? "").ToString();
        if (u["twoFactorEnabled"] != null)
            User.TwoFactorEnabled = u["twoFactorEnabled"]!.GetValue<bool>();

        User.Subscriptions.Clear();
        if (u["subscriptions"] is JsonArray subArray)
        {
            foreach (var item in subArray)
            {
                if (item == null) continue;
                long tl = 0;
                if (item["timeleft"] != null) tl = item["timeleft"]!.GetValue<long>();
                else if (item["timeLeft"] != null) tl = item["timeLeft"]!.GetValue<long>();

                User.Subscriptions.Add(new SubscriptionData
                {
                    Subscription = (item["subscription"] ?? "").ToString(),
                    Key = (item["key"] ?? "").ToString(),
                    Expiry = (item["expiry"] ?? item["expiresAt"] ?? "").ToString(),
                    TimeLeft = tl
                });
            }
        }

        if (json["app"] is JsonObject appObj)
        {
            App.TotalUsers = appObj["totalUsers"]?.GetValue<int>() ?? 0;
            App.OnlineUsers = appObj["onlineUsers"]?.GetValue<int>() ?? 0;
            App.TotalKeys = appObj["totalKeys"]?.GetValue<int>() ?? 0;
            App.Version = appObj["version"]?.GetValue<string>() ?? Version;
        }
    }

    private static string FormatHumanErrorMessage(string msg, string? err)
    {
        string candidate = !string.IsNullOrWhiteSpace(msg) ? msg : (err ?? "Operation failed");
        return candidate switch
        {
            "rate_limited" => "Too many requests. Please wait a moment and try again.",
            "invalid_credentials" => "The credentials provided do not match our records.",
            "invalid_apikey" => "The API key provided is invalid or unrecognized.",
            "app_not_found" => "Application was not found. Please check your credentials.",
            "app_disabled" => "This application is currently disabled by administrator policy.",
            "app_paused" => "Application is currently paused for maintenance.",
            "invalid_token" => "Authentication token is invalid or expired.",
            "feature_disabled" => "This feature is currently disabled for this application.",
            "session_invalid" or "session_not_validated" => "Session is no longer valid. Please sign in again.",
            "invalid_hwid" => "Device signature is invalid or unrecognized.",
            "hwid_mismatch" => "Device signature mismatch. Please reset your HWID.",
            "vpn_blocked" => "Anonymous proxies and VPN connections are not permitted.",
            "blacklisted" => "Your hardware identifier or IP address has been restricted.",
            "username_taken" => "This username is already registered. Please choose another.",
            "email_taken" => "This email address is already registered.",
            "invalid_license" => "The license key provided is invalid or unrecognized.",
            "license_used" => "This license key has already been activated.",
            "no_active_subscription" or "expired" => "No active subscription found for this account.",
            "subscription_paused" or "paused" => "Your subscription is temporarily suspended.",
            "version_mismatch" => "This build is outdated and can no longer connect. Please update.",
            _ => candidate
        };
    }

    private void CaptureUpdateInfo(JsonObject? json)
    {
        string? required = json?["requiredVersion"]?.GetValue<string>();
        string? url = json?["downloadUrl"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(required)) LatestVersion = required;
        if (!string.IsNullOrEmpty(url)) PendingDownloadUrl = url;
    }

    #region Auto-Update

    [Obsolete("Unsigned executable replacement was removed. Use SignedUpdateInstaller.")]
    public bool ApplyUpdate(string? downloadUrl = null)
    {
        SetError("Unsigned auto-update is disabled. Verify a signed release manifest with SignedUpdateInstaller.", "unsigned_update_disabled");
        return false;
    }

    #endregion

    private void SetError(string msg, string? err = null)
    {
        string friendlyMsg = FormatHumanErrorMessage(msg, err);
        Response = new Response { Success = false, Message = friendlyMsg, Error = err ?? (!string.IsNullOrEmpty(msg) ? msg : "error") };
    }
    private void SetSuccess(string msg) => Response = new Response { Success = true, Message = msg, Error = null };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopRealtimeEventListener();
        _httpClient.Dispose();
        if (_sharedKey != null) CryptographicOperations.ZeroMemory(_sharedKey);
        _sharedKey = null;
        _sessionKey = null;
        _isInitialized = false;
    }

    #endregion
}

#pragma warning disable CS8981
public class api : Api
{
    public api(string name, string apikey, string version = "1.0", string? token = null, string? apiUrl = null,
               HttpMessageHandler? httpHandler = null, Action? securityCheck = null)
        : base(name, apikey, version, token, apiUrl, httpHandler, securityCheck)
    {
    }
}
#pragma warning restore CS8981

public class NightAuthApp : api
{
    public NightAuthApp(string name, string apikey, string version = "1.0", string? token = null, string? apiUrl = null,
                        HttpMessageHandler? httpHandler = null, Action? securityCheck = null)
        : base(name, apikey, version, token, apiUrl, httpHandler, securityCheck)
    {
    }
}

public class KeyAuthApp : api
{
    public KeyAuthApp(string name, string apikey, string version = "1.0", string? token = null, string? apiUrl = null,
                      HttpMessageHandler? httpHandler = null, Action? securityCheck = null)
        : base(name, apikey, version, token, apiUrl, httpHandler, securityCheck)
    {
    }
}

#region Security Protections (Anti-Debug, Anti-Sniffer, Anti-Tamper)

public static class SecurityProtections
{
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern bool IsDebuggerPresent();

    private static readonly string[] BlacklistedProcesses =
    {
        "fiddler", "wireshark", "httpdebugger", "httpdebuggerui", "x64dbg", "x32dbg",
        "ida", "ida64", "charles", "dnspy", "de4dot", "scylla", "processhacker",
        "cheatengine", "ollydbg"
    };

    private static string? _cachedHwid;
    private static readonly object _hwidLock = new();

    public static void RunSecurityChecks()
    {
        CheckSystemProxy();
    }

    public static void CheckDebugger()
    {
        if (Debugger.IsAttached || IsDebuggerPresent())
        {
            SelfTerminate("Debugger detected.");
        }

        bool isRemote = false;
        CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isRemote);
        if (isRemote)
        {
            SelfTerminate("Remote debugger detected.");
        }
    }

    public static void CheckBlacklistedProcesses()
    {
        if (Environment.GetEnvironmentVariable("NIGHTAUTH_IGNORE_SECURITY") == "1") return;
        try
        {
            var processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                string name = p.ProcessName.ToLowerInvariant();
                foreach (var bad in BlacklistedProcesses)
                {
                    if (name.Contains(bad))
                    {
                        SelfTerminate($"Blacklisted analysis tool detected: {p.ProcessName}");
                    }
                }
            }
        }
        catch { }
    }

    public static void CheckSystemProxy()
    {
        try
        {
            var proxy = System.Net.WebRequest.DefaultWebProxy;
            if (proxy != null)
            {
                Uri probeUri = new("https://www.google.com");
                Uri? proxyUri = proxy.GetProxy(probeUri);
                if (proxyUri != null && !proxyUri.Equals(probeUri))
                {
                    SelfTerminate("Active HTTP proxy detected.");
                }
            }
        }
        catch { }
    }

    public static string GetHardwareId()
    {
        if (_cachedHwid != null) return _cachedHwid;

        lock (_hwidLock)
        {
            if (_cachedHwid != null) return _cachedHwid;

            try
            {
                string hostname = Environment.GetEnvironmentVariable("COMPUTERNAME")
                    ?? Environment.GetEnvironmentVariable("HOSTNAME")
                    ?? Environment.MachineName;
                if (string.IsNullOrWhiteSpace(hostname)) hostname = "unknown_host";
                hostname = hostname.Trim().ToUpperInvariant();

                string username = Environment.GetEnvironmentVariable("USERNAME")
                    ?? Environment.GetEnvironmentVariable("USER")
                    ?? Environment.UserName;
                if (string.IsNullOrWhiteSpace(username)) username = "unknown_user";
                username = username.Trim().ToLowerInvariant();

                string raw = $"{hostname}-{username}-nightauth-hwid";
                byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
                _cachedHwid = Convert.ToHexString(hash).ToLowerInvariant();
                return _cachedHwid;
            }
            catch
            {
                _cachedHwid = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Environment.MachineName))).ToLowerInvariant();
                return _cachedHwid;
            }
        }
    }

    private static void SelfTerminate(string reason)
    {
        try
        {
            string cleanReason = reason.Replace("\"", "'").Replace("&", " ").Replace("|", " ");
            string cmdArgs = $"/c title NightAuth Security Alert & color 4f & echo ========================================================= & echo [NIGHTAUTH SECURITY PROTECTION TRIGGERED] & echo ========================================================= & echo. & echo Reason: {cleanReason} & echo. & echo Process has been terminated immediately to prevent memory dumps. & echo This window will close automatically in 5 seconds... & timeout /t 5 >nul";

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmdArgs,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };
            Process.Start(startInfo);
        }
        catch { }

        try
        {
            Process.GetCurrentProcess().Kill();
        }
        catch
        {
            Environment.Exit(0x505);
        }
    }
}

#endregion

#endregion

#region v1 Client (NightAuthClient, NightAuthOptions, NightAuthResult)

public sealed record NightAuthOptions(
    Uri BaseUri, string Name, string ApiKey,
    string SigningPublicKey, string SealedBoxPublicKey)
{
    public const string DefaultSigningPublicKey = "cdb2285db481666ce40003aadb874c8751dbeb9a0672a37f740dfe881ecdc742";
    public const string DefaultSealedBoxPublicKey = "b773fc4f6cf1a0938e78e714c3d84cb386584bea24728923008471a6cd203213";
    public string Version { get; init; } = "1.0";
    public string? Token { get; init; }
    public bool AllowInsecureLoopback { get; init; }
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxResponseBytes { get; init; } = 16 * 1024 * 1024;
    public Func<string> HardwareIdProvider { get; init; } = SecurityProtections.GetHardwareId;
}

public sealed class NightAuthResult
{
    private readonly JsonObject _payload;
    public bool Success { get; }
    public string? Error { get; }
    public string? Message { get; }

    public HttpStatusCode StatusCode { get; }

    public TimeSpan? RetryAfter { get; }
    public string DisplayMessage => Message ?? Error ?? (Success ? "Completed." : "Operation denied.");

    public UserData ToUserData()
    {
        var data = _payload["user"] ?? _payload["info"];
        var user = new UserData();
        if (!Success || data is null) return user;
        user.Username = data["username"]?.ToString() ?? "";
        user.Email = data["email"]?.ToString() ?? "";
        user.Ip = data["ip"]?.ToString() ?? "";
        user.CreatedAt = (data["createdAt"] ?? data["createdate"])?.ToString() ?? "";
        user.LastLoginAt = (data["lastLoginAt"] ?? data["lastlogin"])?.ToString() ?? "";
        user.TwoFactorEnabled = data["twoFactorEnabled"]?.GetValue<bool>() ?? false;
        if (data["subscriptions"] is JsonArray subscriptions)
        {
            foreach (var item in subscriptions.OfType<JsonObject>())
            {
                user.Subscriptions.Add(new SubscriptionData
                {
                    Subscription = item["subscription"]?.ToString() ?? "",
                    Key = item["key"]?.ToString() ?? "",
                    Expiry = (item["expiry"] ?? item["expiresAt"])?.ToString() ?? "",
                    TimeLeft = (item["timeleft"] ?? item["timeLeft"])?.GetValue<long>() ?? 0
                });
            }
        }
        return user;
    }
    public JsonObject Payload => (JsonObject)_payload.DeepClone();

    internal bool ClientFailure { get; }

    internal NightAuthResult(JsonObject payload, HttpStatusCode status) : this(payload, status, null) { }

    internal NightAuthResult(JsonObject payload, HttpStatusCode status, TimeSpan? retryAfter, bool clientFailure = false)
    {
        _payload = (JsonObject)payload.DeepClone();
        Success = !clientFailure && (int)status is >= 200 and < 300 && payload["success"] is JsonValue ok
            && ok.TryGetValue<bool>(out var success) && success;
        Error = NightAuthClient.Str(payload["error"]);
        Message = NightAuthClient.Str(payload["message"]);
        StatusCode = status;
        RetryAfter = retryAfter ?? PayloadRetryAfter(payload["retry_after"]);
        ClientFailure = clientFailure;
    }

    private static TimeSpan? PayloadRetryAfter(JsonNode? node)
    {
        if (node is not JsonValue value) return null;
        double seconds;
        if (value.TryGetValue<double>(out var d)) seconds = d;
        else if (value.TryGetValue<long>(out var l)) seconds = l;
        else if (value.TryGetValue<int>(out var i)) seconds = i;
        else return null;
        if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return null;
        return TimeSpan.FromSeconds(Math.Clamp(seconds, 0, 366d * 24 * 3600));
    }
}

internal enum SessionEffect { None, InvalidateOnRejection, ClearOnSuccess }

public sealed class NightAuthStreamException : InvalidOperationException
{
    public NightAuthStreamException(HttpStatusCode statusCode, string? error, string message) : base(message)
    {
        StatusCode = statusCode;
        Error = error;
    }
    public NightAuthStreamException(HttpStatusCode statusCode, string? error, string message, Exception inner) : base(message, inner)
    {
        StatusCode = statusCode;
        Error = error;
    }
    public HttpStatusCode StatusCode { get; }
    public string? Error { get; }
}

internal sealed class ResponseFailureException(NightAuthResult result) : Exception(result.Message)
{
    public NightAuthResult Result { get; } = result;
}

public sealed class NightAuthClient : IAsyncDisposable
{
    private const string WebhookPath = "/webhook";
    private readonly NightAuthOptions _options;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private byte[]? _key;
    private string? _session;
    private int _disposed;

    private readonly record struct RawResponse(JsonObject Body, HttpStatusCode Status, TimeSpan? RetryAfter);

    public NightAuthClient(NightAuthOptions options, HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateUri(options.BaseUri, options.AllowInsecureLoopback);
        if (string.IsNullOrWhiteSpace(options.Name) || string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("Name and API key are required.", nameof(options));
        if (Convert.FromHexString(options.SigningPublicKey).Length != 32 ||
            Convert.FromHexString(options.SealedBoxPublicKey).Length != 32)
            throw new ArgumentException("Trust keys must contain 32 bytes.", nameof(options));
        if (options.MaxResponseBytes < 1024 || options.RequestTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options));
        _options = options;

        _http = handler is null
            ? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2) })
            : new HttpClient(handler, disposeHandler: false);
        _http.Timeout = Timeout.InfiniteTimeSpan;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NightAuth-CSharp-SDK/2.0");
    }

    internal static void ValidateUri(Uri uri, bool allowLoopback)
    {
        if (!uri.IsAbsoluteUri || uri.UserInfo.Length != 0 || uri.Query.Length != 0 ||
            uri.Fragment.Length != 0 || (uri.AbsolutePath != "/" && uri.AbsolutePath != "/api/v1" && uri.AbsolutePath != "/api/v1/") ||
            (uri.Scheme != "https" && !(allowLoopback && uri.Scheme == "http" && uri.IsLoopback)))
            throw new ArgumentException("Use an HTTPS origin; HTTP requires explicit loopback development mode.");
    }

    private CancellationTokenSource Operation(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        source.CancelAfter(_options.RequestTimeout);
        return source;
    }

    private async Task<NightAuthResult> RunAsync(CancellationToken caller, bool gated, Func<CancellationToken, Task<NightAuthResult>> action)
    {
        using var operation = Operation(caller);
        try
        {
            if (!gated) return await action(operation.Token).ConfigureAwait(false);
            await _gate.WaitAsync(operation.Token).ConfigureAwait(false);
            try { return await action(operation.Token).ConfigureAwait(false); }
            finally { _gate.Release(); }
        }
        catch (ResponseFailureException e) { return e.Result; }
        catch (Exception e) when (IsTransportFailure(e))
        {
            if (caller.IsCancellationRequested || _lifetime.IsCancellationRequested)
            {
                if (e is OperationCanceledException) throw;
                throw new OperationCanceledException(e.Message, e, caller.IsCancellationRequested ? caller : _lifetime.Token);
            }
            return operation.IsCancellationRequested
                ? Failure("timeout", "The request did not complete within the configured timeout.", 0)
                : Failure("network_error", e.Message, 0);
        }
    }

    private static bool IsTransportFailure(Exception e)
        => e is OperationCanceledException or HttpRequestException or IOException or SocketException;

    private static NightAuthResult Failure(string error, string message, HttpStatusCode status, TimeSpan? retryAfter = null)
        => new(new JsonObject { ["success"] = false, ["error"] = error, ["message"] = message }, status, retryAfter, clientFailure: true);

    public Task<NightAuthResult> InitializeAsync(CancellationToken cancellationToken = default)
        => RunAsync(cancellationToken, gated: true, InitializeCoreAsync);

    private async Task<NightAuthResult> InitializeCoreAsync(CancellationToken ct)
    {
        ClearSession();
        var privateKey = new X25519PrivateKeyParameters(new SecureRandom());
        var session = Guid.NewGuid().ToString("N");
        var body = new JsonObject {
            ["name"] = _options.Name, ["apikey"] = _options.ApiKey, ["version"] = _options.Version,
            ["id"] = session, ["k1"] = Convert.ToHexString(privateKey.GeneratePublicKey().GetEncoded()).ToLowerInvariant()
        };
        var sealedBody = new JsonObject { ["p"] = V1Crypto.CryptoBoxSeal(body.ToJsonString(), _options.SealedBoxPublicKey) };
        var raw = await PostAsync("/init", Json(sealedBody), ct).ConfigureAwait(false);
        var json = raw.Body;
        var result = new NightAuthResult(json, raw.Status, raw.RetryAfter);
        if (!result.Success) return result;
        var k2 = Str(json["k2"]) ?? "";
        var vk = Str(json["vk"]) ?? "";
        var ts = Long(json["ts"]) ?? 0;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (k2.Length != 64 || !k2.All(Uri.IsHexDigit) ||
            !string.Equals(vk, _options.SigningPublicKey, StringComparison.OrdinalIgnoreCase) ||
            ts < now - 90_000 || ts > now + 90_000 ||
            !V1Crypto.VerifyServerSignature(k2, _options.SigningPublicKey, ts, Str(json["sig"]) ?? ""))
            throw new CryptographicException("Untrusted or stale initialization response.");
        var secret = new byte[32];
        try
        {
            privateKey.GenerateSecret(new X25519PublicKeyParameters(Convert.FromHexString(k2)), secret, 0);
            _key = V1Crypto.HkdfExtractAndExpand(secret, Encoding.UTF8.GetBytes("nightauth-v1-e2e"), 32);
            _session = session;
        }
        finally { CryptographicOperations.ZeroMemory(secret); }
        return result;
    }

    private Task<NightAuthResult> SendAsync(string path, JsonObject body, CancellationToken cancellationToken,
        SessionEffect effect = SessionEffect.None, Func<JsonObject, HttpContent>? transport = null)
        => RunAsync(cancellationToken, gated: true, async token =>
        {
            if (_key is null || _session is null)
            {
                var init = await InitializeCoreAsync(token).ConfigureAwait(false);
                if (!init.Success) return init;
            }
            var result = await SendOnceAsync(path, body, transport, token).ConfigureAwait(false);
            if (!result.Success && IsHandshakeExpired(result))
            {
                var reinit = await InitializeCoreAsync(token).ConfigureAwait(false);
                if (reinit.Success) result = await SendOnceAsync(path, body, transport, token).ConfigureAwait(false);
            }
            if (effect == SessionEffect.ClearOnSuccess && result.Success) ClearSession();
            if (effect == SessionEffect.InvalidateOnRejection && !result.Success) InvalidateRejectedAccess(result);
            return result;
        });

    private async Task<NightAuthResult> SendOnceAsync(string path, JsonObject body, Func<JsonObject, HttpContent>? transport, CancellationToken ct)
    {
        var key = _key!;
        var session = _session!;
        body["name"] = _options.Name;
        body["apikey"] = _options.ApiKey;
        if (_options.Token is not null) body["token"] = _options.Token;
        foreach (var name in body.Where(pair => pair.Value is null).Select(pair => pair.Key).ToArray()) body.Remove(name);
        var rid = Guid.NewGuid().ToString("N");
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var targetUri = BuildTargetUri(path);
        var (ciphertext, nonce) = V1Crypto.AesGcmEncrypt(key, body.ToJsonString(), $"POST:{targetUri.AbsolutePath}:{session}:{rid}:{ts}");
        var envelope = new JsonObject { ["id"] = session, ["ct"] = ciphertext, ["n"] = nonce, ["rid"] = rid, ["ts"] = ts };
        var raw = await PostAsync(path, transport is null ? Json(envelope) : transport(envelope), ct).ConfigureAwait(false);
        var (response, status) = (raw.Body, raw.Status);
        if (response["ct"] is null || response["rid"] is null)
        {

            if ((int)status is >= 200 and < 300) throw new CryptographicException("Response is not authenticated to this request.");
            var refusal = new NightAuthResult(response, status, raw.RetryAfter);
            if (status == HttpStatusCode.PreconditionRequired || refusal.Error == "handshake_required") ClearSession();
            return refusal;
        }
        JsonObject decoded;
        try { decoded = DecodeResponse(response, key, session, rid, ts); }
        catch (InvalidDataException e)
        {
            throw new ResponseFailureException(Failure("invalid_response", e.Message, status, raw.RetryAfter));
        }
        var result = new NightAuthResult(decoded, status, raw.RetryAfter);
        if (path == "/logout" && result.Success) ClearSession();
        return result;
    }

    private static bool IsHandshakeExpired(NightAuthResult result)
    {
        if (result.ClientFailure) return false;
        var message = (result.Message ?? "").ToLowerInvariant();
        var error = result.Error ?? "";
        return message.Contains("handshake") || message.Contains("initialize app")
            || error is "handshake_required" or "session_invalid" or "unauthenticated";
    }

    private void InvalidateRejectedAccess(NightAuthResult result)
    {
        if (result.Error is "session_invalid" or "session_not_validated" or "unauthenticated" or "version_mismatch"
            or "app_disabled" or "app_paused" or "blacklisted")
            ClearSession();
    }

    internal static JsonObject DecodeResponse(JsonObject envelope, byte[] key, string session, string rid, long ts)
    {
        var ct = Str(envelope["ct"]);
        var n = Str(envelope["n"]);
        if (Str(envelope["rid"]) != rid || Long(envelope["ts"]) != ts || ct is null || n is null)
            throw new CryptographicException("Response is not authenticated to this request.");
        string plain;
        try { plain = V1Crypto.AesGcmDecrypt(key, ct, n, $"v1:res:{session}:{rid}:{ts}"); }
        catch (FormatException e) { throw new CryptographicException("Malformed AES-GCM envelope.", e); }
        return ParseObject(Encoding.UTF8.GetBytes(plain)) ?? throw new InvalidDataException("Decrypted response is not a JSON object.");
    }

    private Uri BuildTargetUri(string path)
    {
        var baseStr = _options.BaseUri.ToString().TrimEnd('/');
        if (!baseStr.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            baseStr += "/api/v1";
        }
        return new Uri(baseStr + path);
    }

    private static StringContent Json(JsonObject body) => new(body.ToJsonString(), Encoding.UTF8, "application/json");

    private async Task<RawResponse> PostAsync(string path, HttpContent content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildTargetUri(path)) { Content = content };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        var retryAfter = ParseRetryAfter(response.Headers.RetryAfter);
        var body = await ReadObjectAsync(response, _options.MaxResponseBytes, retryAfter, ct).ConfigureAwait(false);
        return new RawResponse(body, response.StatusCode, retryAfter);
    }

    private static async Task<JsonObject> ReadObjectAsync(HttpResponseMessage response, int limit, TimeSpan? retryAfter, CancellationToken ct)
    {
        byte[] bytes;
        try { bytes = await ReadBoundedAsync(response.Content, limit, ct).ConfigureAwait(false); }
        catch (InvalidDataException e)
        {
            throw new ResponseFailureException(Failure("response_too_large", e.Message, response.StatusCode, retryAfter));
        }
        return ParseObject(bytes) ?? throw new ResponseFailureException(
            Failure("invalid_response", "Response body is not a JSON object.", response.StatusCode, retryAfter));
    }

    private static JsonObject? ParseObject(byte[] bytes)
    {
        try
        {
            if (JsonNode.Parse(bytes) is not JsonObject json) return null;
            _ = json.Count;
            return json;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or ArgumentException) { return null; }
    }

    private static TimeSpan? ParseRetryAfter(RetryConditionHeaderValue? header)
    {
        if (header?.Delta is { } delta) return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        if (header?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
        }
        return null;
    }

    internal static string? Str(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var s) ? s : null;
    internal static long? Long(JsonNode? node) => node is JsonValue value && value.TryGetValue<long>(out var l) ? l : null;

    internal static async Task<byte[]> ReadBoundedAsync(HttpContent content, int limit, CancellationToken ct)
    {
        if (content.Headers.ContentLength > limit) throw new InvalidDataException("Response exceeds size limit.");
        using var stream = await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(chunk, ct).ConfigureAwait(false)) != 0)
        {
            if (buffer.Length + count > limit) throw new InvalidDataException("Response exceeds size limit.");
            buffer.Write(chunk, 0, count);
        }
        return buffer.ToArray();
    }

    private JsonObject Credentials(string username, string password, string? totpCode) => new() {
        ["username"] = username, ["password"] = password, ["totpCode"] = totpCode,
        ["hwid"] = _options.HardwareIdProvider(), ["version"] = _options.Version
    };
    public Task<NightAuthResult> LoginAsync(string username, string password, string? totpCode = null, CancellationToken ct = default)
        => SendAsync("/login", Credentials(username, password, totpCode), ct);
    public Task<NightAuthResult> RegisterAsync(string username, string password, string key, string email = "", CancellationToken ct = default)
    {
        var body = Credentials(username, password, null); body["key"] = key;

        if (!string.IsNullOrWhiteSpace(email)) body["email"] = email;
        return SendAsync("/register", body, ct);
    }
    public Task<NightAuthResult> LicenseAsync(string key, CancellationToken ct = default)
        => LicenseAsync(key, null, ct);

    public Task<NightAuthResult> LicenseAsync(string key, string? totpCode, CancellationToken ct = default)
        => SendAsync("/license", new() { ["key"] = key, ["hwid"] = _options.HardwareIdProvider(), ["version"] = _options.Version, ["totpCode"] = totpCode }, ct);

    public Task<NightAuthResult> UpgradeAsync(string? username, string key, CancellationToken ct = default)
    {
        var body = new JsonObject { ["key"] = key };
        if (!string.IsNullOrWhiteSpace(username)) body["username"] = username;
        return SendAsync("/upgrade", body, ct);
    }

    public Task<NightAuthResult> CheckAsync(CancellationToken ct = default)
        => SendAsync("/check", new() { ["version"] = _options.Version }, ct, SessionEffect.InvalidateOnRejection);
    public Task<NightAuthResult> HeartbeatAsync(CancellationToken ct = default)
        => SendAsync("/heartbeat", new() { ["version"] = _options.Version }, ct, SessionEffect.InvalidateOnRejection);
    public async Task<NightAuthResult> LogoutAsync(CancellationToken ct = default)
    {
        var result = await SendAsync("/logout", new(), ct).ConfigureAwait(false);

        if (result.Success || result.ClientFailure || result.Error == "handshake_required") return result;
        return new NightAuthResult(new JsonObject
        {
            ["success"] = false, ["error"] = "logout_unconfirmed", ["serverError"] = result.Error, ["message"] = result.Message,
        }, result.StatusCode, result.RetryAfter);
    }
    public Task<NightAuthResult> ChangeUsernameAsync(string username, string password, CancellationToken ct = default)
        => SendAsync("/change-username", new() { ["newUsername"] = username, ["password"] = password }, ct);

    public Task<NightAuthResult> ResetHwidAsync(string username, string password, string? totpCode = null, CancellationToken ct = default)
        => SendAsync("/hwid-reset", Credentials(username, password, totpCode), ct, SessionEffect.ClearOnSuccess);
    public Task<NightAuthResult> ForgotPasswordAsync(string email, CancellationToken ct = default)
        => SendAsync("/forgot-password", new() { ["email"] = email }, ct);
    public Task<NightAuthResult> ResetPasswordAsync(string token, string password, CancellationToken ct = default)
        => RunAsync(ct, gated: false, async operation =>
        {
            var raw = await PostAsync("/reset-password", Json(new() { ["token"] = token, ["newPassword"] = password }), operation).ConfigureAwait(false);
            return new NightAuthResult(raw.Body, raw.Status, raw.RetryAfter);
        });
    public Task<NightAuthResult> Setup2FAAsync(CancellationToken ct = default) => SendAsync("/2fa/setup", new(), ct);
    public Task<NightAuthResult> Confirm2FAAsync(string code, CancellationToken ct = default)
        => SendAsync("/2fa/confirm", new() { ["code"] = code }, ct);
    public Task<NightAuthResult> Disable2FAAsync(string password, string code, CancellationToken ct = default)
        => SendAsync("/2fa/disable", new() { ["password"] = password, ["code"] = code }, ct);
    public Task<NightAuthResult> GetGlobalVarAsync(string key, CancellationToken ct = default)
        => SendAsync("/global-var", new() { ["varName"] = key }, ct);
    public async Task<NightAuthResult> GetUserVarAsync(string key, CancellationToken ct = default)
    {
        var result = await GetUserVariablesAsync(ct).ConfigureAwait(false);
        if (!result.Success) return result;
        var variable = (result.Payload["variables"] as JsonArray)?.FirstOrDefault(item => Str(item?["key"]) == key);
        return variable is null
            ? new NightAuthResult(new() { ["success"] = false, ["error"] = "variable_not_found" }, HttpStatusCode.NotFound)
            : new NightAuthResult(new() { ["success"] = true, ["variable"] = variable.DeepClone() }, result.StatusCode);
    }
    public Task<NightAuthResult> GetUserVariablesAsync(CancellationToken ct = default)
        => SendAsync("/user-vars/get", new(), ct);
    public Task<NightAuthResult> SetUserVarAsync(string key, string value, CancellationToken ct = default)
        => SendAsync("/user-vars/set", new() { ["key"] = key, ["value"] = value }, ct);
    public Task<NightAuthResult> GetChatMessagesAsync(string channel = "general", CancellationToken ct = default)
        => GetChatMessagesAsync(channel, null, null, ct);

    public Task<NightAuthResult> GetChatMessagesAsync(string channel, long? before, bool? markRead, CancellationToken ct = default)
        => SendAsync("/chat/get", new() { ["room"] = channel, ["before"] = before, ["markRead"] = markRead }, ct);
    public Task<NightAuthResult> SendChatMessageAsync(string message, string channel = "general", CancellationToken ct = default)
        => SendAsync("/chat/send", new() { ["content"] = message, ["room"] = channel }, ct);
    public Task<NightAuthResult> LogAsync(string message, CancellationToken ct = default)
        => SendAsync("/log", new() { ["message"] = message }, ct);
    public Task<NightAuthResult> GetStatsAsync(CancellationToken ct = default) => SendAsync("/stats", new(), ct);
    public Task<NightAuthResult> GetOnlineUsersAsync(CancellationToken ct = default) => SendAsync("/online-users", new(), ct);
    public Task<NightAuthResult> BanAsync(string? reason = null, CancellationToken ct = default)
        => SendAsync("/ban", new() { ["reason"] = reason }, ct);
    public Task<NightAuthResult> CheckBlacklistAsync(CancellationToken ct = default)
        => SendAsync("/check-blacklist", new() { ["hwid"] = _options.HardwareIdProvider() }, ct);
    public Task<NightAuthResult> VerifyFileHashAsync(string hash, CancellationToken ct = default)
        => SendAsync("/verify-hash", new() { ["hash"] = hash }, ct);

    public Task<NightAuthResult> TriggerWebhookAsync(string id, JsonObject? payload = null, CancellationToken ct = default)
        => TriggerWebhookAsync(id, payload, null, null, ct);

    public Task<NightAuthResult> TriggerWebhookAsync(string id, JsonObject? payload, string? content, string? parameters, CancellationToken ct = default)
        => SendAsync(WebhookPath, WebhookBody(id, payload, content, parameters), ct);

    public Task<NightAuthResult> TriggerWebhookAsync(string id, ReadOnlyMemory<byte> file, string fileName, JsonObject? payload = null,
        string? content = null, string? parameters = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return SendAsync(WebhookPath, WebhookBody(id, payload, content, parameters), ct,
            transport: envelope => MultipartEnvelope(envelope, file, fileName));
    }

    private static JsonObject WebhookBody(string id, JsonObject? payload, string? content, string? parameters) => new()
    {
        ["webId"] = id, ["payload"] = payload?.DeepClone(), ["content"] = content, ["params"] = parameters,
    };

    internal static HttpContent MultipartEnvelope(JsonObject envelope, ReadOnlyMemory<byte> file, string fileName)
    {
        var form = new MultipartContent("form-data", "NightAuthBoundary" + Guid.NewGuid().ToString("N"))
        {
            HeaderEncodingSelector = (_, _) => Encoding.UTF8,
        };
        var envelopePart = new ByteArrayContent(Encoding.UTF8.GetBytes(envelope.ToJsonString()));
        envelopePart.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"envelope\"");
        form.Add(envelopePart);
        var safeName = fileName.Replace("\"", "%22").Replace("\r", "%0D").Replace("\n", "%0A");
        var filePart = new ReadOnlyMemoryContent(file);
        filePart.Headers.TryAddWithoutValidation("Content-Disposition", $"form-data; name=\"file\"; filename=\"{safeName}\"");
        filePart.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(filePart);
        return form;
    }

    public async Task<NightAuthResult> GetFileAsync(string id, CancellationToken ct = default)
    {
        var ticket = await SendAsync("/access-ticket", new() { ["scope"] = "file", ["resourceId"] = id }, ct).ConfigureAwait(false);
        if (!ticket.Success) return ticket;
        var ticketValue = Str(ticket.Payload["ticket"]);
        if (ticketValue is null)
            return Failure("invalid_response", "Access ticket response has no ticket.", ticket.StatusCode, ticket.RetryAfter);
        return await RunAsync(ct, gated: false, async operation =>
        {
            var path = FileDownloads.DownloadPath(ticketValue);
            using var response = await _http.GetAsync(BuildTargetUri(path), HttpCompletionOption.ResponseHeadersRead, operation).ConfigureAwait(false);
            var retryAfter = ParseRetryAfter(response.Headers.RetryAfter);

            if ((int)response.StatusCode is >= 300 and < 400)
                return new NightAuthResult(new() { ["success"] = false, ["error"] = "external_download", ["url"] = response.Headers.Location?.ToString() }, response.StatusCode);
            var json = await ReadObjectAsync(response, FileDownloads.MaxResponseBytes, retryAfter, operation).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK || json["success"] is not JsonValue ok || !ok.TryGetValue<bool>(out var success)
                || !success || Str(json["data"]) is not { } data)
                return new NightAuthResult(json, response.StatusCode, retryAfter);
            try
            {
                var content = FileDownloads.Decompress(Convert.FromBase64String(data));
                json["data"] = Convert.ToBase64String(content);
                return new NightAuthResult(json, response.StatusCode, retryAfter);
            }
            catch (FileTooLargeException)
            {
                return new NightAuthResult(new() { ["success"] = false, ["error"] = "download_too_large" }, response.StatusCode);
            }
            catch (Exception e) when (e is FormatException or InvalidDataException or IOException)
            {
                return new NightAuthResult(new() { ["success"] = false, ["error"] = "download_decode_failed" }, response.StatusCode);
            }
        }).ConfigureAwait(false);
    }

    [Obsolete("Unsigned executable replacement was removed. Use SignedUpdateInstaller.")]
    public Task<string> ApplyUpdateAsync(string downloadUrl, CancellationToken ct = default)
        => Task.FromException<string>(new NotSupportedException(
            "Unsigned auto-update is disabled. Verify a signed release manifest with SignedUpdateInstaller."));

    public IAsyncEnumerable<JsonObject> ReadEventsAsync(CancellationToken ct = default) => ReadEventsAsync(null, ct);

    public async IAsyncEnumerable<JsonObject> ReadEventsAsync(string? lastEventId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var ticket = await SendAsync("/access-ticket", new() { ["scope"] = "events" }, ct).ConfigureAwait(false);
        if (!ticket.Success)
            throw new NightAuthStreamException(ticket.StatusCode, ticket.Error,
                (ticket.ClientFailure ? ticket.Message : ticket.Error) ?? "Event ticket denied.");
        var ticketValue = Str(ticket.Payload["ticket"])
            ?? throw new NightAuthStreamException(ticket.StatusCode, "invalid_response", "Access ticket response has no ticket.");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetime.Token);
        var path = $"/events?ticket={Uri.EscapeDataString(ticketValue)}";
        using var response = await OpenStreamAsync(path, lastEventId, linked.Token, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadRefusalCodeAsync(response.Content, linked.Token).ConfigureAwait(false);
            throw new NightAuthStreamException(response.StatusCode, error, error ?? $"Event stream refused with HTTP {(int)response.StatusCode}.");
        }
        using var stream = await StreamTransport(() => response.Content.ReadAsStreamAsync(linked.Token), ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var state = new EventReader(reader);
        while (await StreamTransport(() => state.NextAsync(linked.Token), ct).ConfigureAwait(false) is { } evt)
            yield return evt;
    }

    private async Task<HttpResponseMessage> OpenStreamAsync(string path, string? lastEventId, CancellationToken token, CancellationToken caller)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildTargetUri(path));
        if (lastEventId is not null) request.Headers.TryAddWithoutValidation("Last-Event-ID", lastEventId);
        return await StreamTransport(() => _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token), caller).ConfigureAwait(false);
    }

    private async Task<T> StreamTransport<T>(Func<Task<T>> action, CancellationToken caller)
    {
        try { return await action().ConfigureAwait(false); }
        catch (Exception e) when (IsTransportFailure(e) && !caller.IsCancellationRequested && !_lifetime.IsCancellationRequested)
        {
            throw new NightAuthStreamException(0, "network_error", e.Message, e);
        }
    }

    private sealed class EventReader(StreamReader reader)
    {
        private const int MaxEventChars = 65536;
        private readonly StringBuilder _line = new();
        private readonly StringBuilder _data = new();
        private readonly char[] _character = new char[1];
        private string? _id;

        public async Task<JsonObject?> NextAsync(CancellationToken ct)
        {
            while (await reader.ReadAsync(_character, ct).ConfigureAwait(false) != 0)
            {
                if (_character[0] != '\n') _line.Append(_character[0]);
                else
                {
                    var value = _line.ToString().TrimEnd('\r'); _line.Clear();
                    if (value.Length == 0)
                    {
                        var id = _id; _id = null;
                        if (_data.Length == 0) continue;
                        var text = _data.ToString(); _data.Clear();
                        JsonObject json;
                        try { json = JsonNode.Parse(text) as JsonObject ?? throw new InvalidDataException("Event data is not a JSON object."); }
                        catch (JsonException e) { throw new InvalidDataException("Event data is not valid JSON.", e); }
                        if (id is not null) json["_eventId"] = id;
                        return json;
                    }
                    if (value[0] == ':') continue;
                    if (value.StartsWith("data:", StringComparison.Ordinal))
                    {
                        if (_data.Length > 0) _data.Append('\n');
                        _data.Append(value[5..].TrimStart(' '));
                    }
                    else if (value.StartsWith("id:", StringComparison.Ordinal))
                    {
                        var id = value[3..];
                        if (id.StartsWith(' ')) id = id[1..];
                        if (!id.Contains('\0')) _id = id;
                    }
                }
                if (_line.Length + _data.Length > MaxEventChars) throw new InvalidDataException("Event exceeds size limit.");
            }
            return null;
        }
    }

    private static async Task<string?> ReadRefusalCodeAsync(HttpContent content, CancellationToken ct)
    {
        try
        {
            var bytes = await ReadBoundedAsync(content, 64 * 1024, ct).ConfigureAwait(false);
            return ParseObject(bytes) is { } json ? Str(json["error"]) : null;
        }
        catch (Exception e) when (e is InvalidDataException or HttpRequestException or IOException) { return null; }
    }

    private void ClearSession()
    {
        if (_key is not null) CryptographicOperations.ZeroMemory(_key);
        _key = null; _session = null;
    }
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        await _lifetime.CancelAsync().ConfigureAwait(false);
        await _gate.WaitAsync().ConfigureAwait(false);
        try { ClearSession(); _http.Dispose(); }
        finally { _gate.Release(); }
    }
}

#endregion

#region v1 Protocol Cryptography

internal static class V1Crypto
{
    private static readonly byte[] SealInfo = Encoding.UTF8.GetBytes("nightauth-v1-init-seal");
        internal static (string ct, string n) AesGcmEncrypt(byte[] key, string plaintext, string aad)
        {
            byte[] nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[16];
            byte[] aadBytes = Encoding.UTF8.GetBytes(aad);

            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag, aadBytes);

            byte[] combined = new byte[cipherBytes.Length + tag.Length];
            Buffer.BlockCopy(cipherBytes, 0, combined, 0, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, combined, cipherBytes.Length, tag.Length);

            return (Convert.ToBase64String(combined), Convert.ToBase64String(nonce));
        }

        internal static string AesGcmDecrypt(byte[] key, string ctB64, string nB64, string aad)
        {
            byte[] nonce = Convert.FromBase64String(nB64);
            byte[] combined = Convert.FromBase64String(ctB64);
            if (nonce.Length != 12 || combined.Length < 16)
                throw new CryptographicException("Malformed AES-GCM envelope.");

            int tagLength = 16;
            int cipherLength = combined.Length - tagLength;

            byte[] cipherBytes = new byte[cipherLength];
            byte[] tag = new byte[tagLength];
            Buffer.BlockCopy(combined, 0, cipherBytes, 0, cipherLength);
            Buffer.BlockCopy(combined, cipherLength, tag, 0, tagLength);

            byte[] plainBytes = new byte[cipherLength];
            byte[] aadBytes = Encoding.UTF8.GetBytes(aad);

            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes, aadBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }

        internal static bool VerifyServerSignature(string k2Hex, string vkHex, long ts, string sigB64)
        {
            try
            {
                byte[] vkBytes = Convert.FromHexString(vkHex);
                byte[] sigBytes = Convert.FromBase64String(sigB64);
                byte[] data = Encoding.UTF8.GetBytes($"{k2Hex}.{vkHex}.{ts}");

                var pubKey = new Ed25519PublicKeyParameters(vkBytes, 0);
                var verifier = new Ed25519Signer();
                verifier.Init(false, pubKey);
                verifier.BlockUpdate(data, 0, data.Length);
                return verifier.VerifySignature(sigBytes);
            }
            catch
            {
                return false;
            }
        }

        internal static string CryptoBoxSeal(string plaintext, string recipientPubKeyHex)
        {
            byte[] recipientPubBytes = Convert.FromHexString(recipientPubKeyHex);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);

            var random = new SecureRandom();
            var keyGen = new X25519KeyPairGenerator();
            keyGen.Init(new X25519KeyGenerationParameters(random));
            var ephemPair = keyGen.GenerateKeyPair();
            var ephemPriv = (X25519PrivateKeyParameters)ephemPair.Private;
            var ephemPub = (X25519PublicKeyParameters)ephemPair.Public;

            byte[] ephemPubBytes = ephemPub.GetEncoded();

            var recipientPub = new X25519PublicKeyParameters(recipientPubBytes, 0);
            byte[] rawSecret = new byte[32];
            ephemPriv.GenerateSecret(recipientPub, rawSecret, 0);

            byte[] encKey = HkdfExtractAndExpand(rawSecret, SealInfo, 32);

            byte[] nonce = new byte[12];
            random.NextBytes(nonce);

            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[16];
            using (var aes = new AesGcm(encKey, 16))
            {
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag, ephemPubBytes);
            }

            byte[] result = new byte[32 + 12 + 16 + cipherBytes.Length];
            Buffer.BlockCopy(ephemPubBytes, 0, result, 0, 32);
            Buffer.BlockCopy(nonce, 0, result, 32, 12);
            Buffer.BlockCopy(tag, 0, result, 44, 16);
            Buffer.BlockCopy(cipherBytes, 0, result, 60, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        internal static byte[] HkdfExtractAndExpand(byte[] ikm, byte[] info, int length)
        {
            var hkdf = new HkdfBytesGenerator(new Sha256Digest());
            hkdf.Init(new HkdfParameters(ikm, null, info));
            byte[] okm = new byte[length];
            hkdf.GenerateBytes(okm, 0, length);
            return okm;
        }

}

#endregion

#region File Downloads

internal sealed class FileTooLargeException : IOException
{
    public FileTooLargeException() : base("File content exceeds the size limit.") { }
}

internal static class FileDownloads
{

    internal const int MaxFileBytes = 64 * 1024 * 1024;

    internal const int MaxResponseBytes = 96 * 1024 * 1024;

    internal static string DownloadPath(string ticket) =>
        "/files/download?ticket=" + Uri.EscapeDataString(ticket);

    internal static byte[] Decompress(byte[] gzip, int maxBytes = MaxFileBytes)
    {
        using var input = new MemoryStream(gzip, writable: false);
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = gz.Read(chunk, 0, chunk.Length)) != 0)
        {
            if (output.Length + count > maxBytes) throw new FileTooLargeException();
            output.Write(chunk, 0, count);
        }
        return output.ToArray();
    }
}

#endregion

#region Signed Updates (Verification)

public sealed class VerifiedUpdate
{
    public string Application { get; }
    public string Version { get; }
    public long Sequence { get; }
    public Uri Url { get; }
    public long Size { get; }
    public DateTimeOffset ExpiresAt { get; }
    internal byte[] Digest { get; }
    internal VerifiedUpdate(string application, string version, long sequence, Uri url, long size, DateTimeOffset expiresAt, byte[] digest)
        => (Application, Version, Sequence, Url, Size, ExpiresAt, Digest) = (application, version, sequence, url, size, expiresAt, digest);
}

public static class SignedUpdate
{
    public const long MaxArtifactBytes = 256 * 1024 * 1024;

    public static VerifiedUpdate Verify(string envelopeJson, string pinnedPublicKeyHex, string expectedApplication, long minimumSequence)
    {
        if (envelopeJson.Length > 65536 || minimumSequence < 0 || string.IsNullOrWhiteSpace(expectedApplication))
            throw new InvalidDataException("Invalid manifest parameters.");
        try
        {
            var envelope = JsonNode.Parse(envelopeJson)!.AsObject();
            var payload = Convert.FromBase64String(envelope["payload"]!.GetValue<string>());
            var signature = Convert.FromBase64String(envelope["signature"]!.GetValue<string>());
            var key = Convert.FromHexString(pinnedPublicKeyHex);
            if (payload.Length > 32768 || signature.Length != 64 || key.Length != 32)
                throw new InvalidDataException("Invalid manifest size or key.");
            var verifier = new Ed25519Signer();
            verifier.Init(false, new Ed25519PublicKeyParameters(key, 0));
            verifier.BlockUpdate(payload, 0, payload.Length);
            if (!verifier.VerifySignature(signature)) throw new CryptographicException("Release signature rejected.");
            var data = JsonNode.Parse(payload)!.AsObject();
            var app = data["application"]!.GetValue<string>();
            var version = data["version"]!.GetValue<string>();
            var sequence = data["sequence"]!.GetValue<long>();
            var size = data["size"]!.GetValue<long>();
            var url = new Uri(data["url"]!.GetValue<string>(), UriKind.Absolute);
            var expires = DateTimeOffset.FromUnixTimeSeconds(data["expiresAt"]!.GetValue<long>());
            var digest = Convert.FromHexString(data["sha256"]!.GetValue<string>());
            if (data["schema"]!.GetValue<int>() != 1 || app != expectedApplication || string.IsNullOrWhiteSpace(version) || version.Length > 100
                || sequence <= minimumSequence || sequence > 9007199254740991 || size < 1 || size > MaxArtifactBytes
                || digest.Length != 32 || expires <= DateTimeOffset.UtcNow
                || url.Scheme != "https" || url.UserInfo.Length != 0 || url.Fragment.Length != 0)
                throw new InvalidDataException("Release identity, freshness, URL or size rejected.");
            return new VerifiedUpdate(app, version, sequence, url, size, expires, digest);
        }
        catch (Exception error) when (error is FormatException or ArgumentException or NullReferenceException or InvalidOperationException or System.Text.Json.JsonException)
        { throw new InvalidDataException("Malformed release manifest.", error); }
    }

    public static Task DownloadAsync(VerifiedUpdate update, string destination, CancellationToken ct = default)
        => DownloadAsync(update, destination, new HttpClientHandler { AllowAutoRedirect = false }, ct);

    internal static async Task DownloadAsync(VerifiedUpdate update, string destination, HttpMessageHandler handler, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromMinutes(5));
        var token = deadline.Token;
        var target = Path.GetFullPath(destination);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            if (update.ExpiresAt <= DateTimeOffset.UtcNow) throw new InvalidDataException("Release expired.");
            using var response = await http.GetAsync(update.Url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK) throw new HttpRequestException("Artifact must return HTTP 200 without redirects.");
            if (response.Content.Headers.ContentLength is long length && length != update.Size)
                throw new InvalidDataException("Artifact length differs from signed manifest.");
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous))
            {
                await using var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                var buffer = new byte[65536];
                long total = 0;
                int count;
                while ((count = await input.ReadAsync(buffer, token).ConfigureAwait(false)) != 0)
                {
                    total += count;
                    if (total > update.Size) throw new InvalidDataException("Artifact exceeds signed size.");
                    hash.AppendData(buffer, 0, count);
                    await output.WriteAsync(buffer.AsMemory(0, count), token).ConfigureAwait(false);
                }
                if (total != update.Size || !CryptographicOperations.FixedTimeEquals(hash.GetHashAndReset(), update.Digest))
                    throw new CryptographicException("Artifact hash or size rejected.");
                await output.FlushAsync(token).ConfigureAwait(false);
                output.Flush(flushToDisk: true);
            }
            token.ThrowIfCancellationRequested();
            if (update.ExpiresAt <= DateTimeOffset.UtcNow) throw new InvalidDataException("Release expired during download.");
            File.Move(temporary, target, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}

#endregion

#region Signed Updates (Installation)

public static class SignedUpdateInstaller
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private const int MaxManifestBytes = 64 * 1024;

    public static async Task<string> DownloadAsync(
        Uri manifestUri, string pinnedReleaseKeyHex, string application,
        string destination, CancellationToken cancellationToken = default)
    {
        if (!manifestUri.IsAbsoluteUri || manifestUri.Scheme != Uri.UriSchemeHttps ||
            manifestUri.UserInfo.Length != 0 || manifestUri.Fragment.Length != 0)
            throw new ArgumentException("Release manifest must use an HTTPS URL without user info or fragment.", nameof(manifestUri));

        var statePath = GetSequencePath(application);
        return await DownloadCoreAsync(manifestUri, pinnedReleaseKeyHex, application, destination,
            statePath, manifestHandler: null, artifactHandler: null, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    internal static Task<string> DownloadForTestingAsync(Uri manifestUri, string pinnedReleaseKeyHex,
        string application, string destination, string statePath, HttpMessageHandler manifestHandler,
        HttpMessageHandler artifactHandler, CancellationToken cancellationToken = default)
        => DownloadCoreAsync(manifestUri, pinnedReleaseKeyHex, application, destination, statePath,
            manifestHandler, artifactHandler, cancellationToken);

    private static async Task<string> DownloadCoreAsync(Uri manifestUri, string pinnedReleaseKeyHex,
        string application, string destination, string statePath, HttpMessageHandler? manifestHandler,
        HttpMessageHandler? artifactHandler, CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var processLock = await AcquireProcessLockAsync(statePath + ".lock", cancellationToken).ConfigureAwait(false);
            var currentSequence = await ReadSequenceAsync(statePath, cancellationToken).ConfigureAwait(false);
            using var http = new HttpClient(manifestHandler ?? new SocketsHttpHandler { AllowAutoRedirect = false }, disposeHandler: manifestHandler is null)
            { Timeout = TimeSpan.FromSeconds(30) };
            using var response = await http.GetAsync(manifestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("Release manifest must return HTTP 200 without redirects.");
            if (response.Content.Headers.ContentLength is long length && length > MaxManifestBytes)
                throw new InvalidDataException("Release manifest exceeds the maximum allowed size.");
            var manifestBytes = await NightAuthClient.ReadBoundedAsync(response.Content, MaxManifestBytes, cancellationToken).ConfigureAwait(false);
            var verified = SignedUpdate.Verify(Encoding.UTF8.GetString(manifestBytes), pinnedReleaseKeyHex, application, currentSequence);
            var fullDestination = Path.GetFullPath(destination);
            if (artifactHandler is null)
                await SignedUpdate.DownloadAsync(verified, fullDestination, cancellationToken).ConfigureAwait(false);
            else
                await SignedUpdate.DownloadAsync(verified, fullDestination, artifactHandler, cancellationToken).ConfigureAwait(false);

            await WriteSequenceAtomicallyAsync(statePath, verified.Sequence, cancellationToken).ConfigureAwait(false);
            return fullDestination;
        }
        finally { Gate.Release(); }
    }

    private static async Task<FileStream> AcquireProcessLockAsync(string path, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) { await Task.Delay(TimeSpan.FromMilliseconds(100), ct).ConfigureAwait(false); }
        }
    }

    private static string GetSequencePath(string application)
    {
        if (string.IsNullOrWhiteSpace(application) || application.Length > 100 ||
            application.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Invalid application identifier.", nameof(application));
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("No per-user application data directory is available.");
        var directory = Path.Combine(root, "NightAuth", "release-sequences");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(application))) + ".seq");
    }

    private static async Task<long> ReadSequenceAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return 0;
        var text = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        if (!long.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var sequence) || sequence < 0)
            throw new InvalidDataException("Stored release sequence is corrupt; refusing update.");
        return sequence;
    }

    private static async Task WriteSequenceAtomicallyAsync(string path, long sequence, CancellationToken ct)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            await using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                await writer.WriteAsync(sequence.ToString(System.Globalization.CultureInfo.InvariantCulture).AsMemory(), ct).ConfigureAwait(false);
                await writer.FlushAsync(ct).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

#endregion
