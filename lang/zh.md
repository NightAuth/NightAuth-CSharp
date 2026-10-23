# NightAuth C# .NET SDK — 在 GitHub 上为我们点亮星标 ⭐

官方 **NightAuth** C# / .NET SDK 及完整参考客户端套件（基于 **WPF** 的现代化桌面客户端与基于终端交互式的 CLI 控制台）。在数分钟内为您的 .NET 软件集成云端授权激活、硬件防盗绑定（HWID Lock）、反作弊检测、实时聊天与内存防转储保护。

官方门户网站：[https://nightauth.org](https://nightauth.org)

---

**Languages / Idiomas / Langues / Sprachen:**  
[English](../README.md) • [Español](es.md) • [Français](fr.md) • [Deutsch](de.md) • [Italiano](it.md) • [Português](pt.md) • [Русский](ru.md) • [Türkçe](tr.md) • [Polski](pl.md) • [中文](zh.md)

---

> [!NOTE]
> ### 📺 视频教程与系统部署指南
> 官方视频操作教程以及关于如何在 NightAuth 管理后台配置应用、订阅计划、云端变量、Webhook 与密钥分发的分步指南**即将发布**，敬请关注官方网站与频道。

---

## 📸 界面截图

### 桌面图形界面客户端 (WPF Enterprise Dark Theme)

| 登录 / 激活认证界面 | 用户面板与授权监控 |
| :---: | :---: |
| ![NightAuth Desktop Login](../image/desktop.png) | ![NightAuth Desktop Dashboard](../image/desktop-dashboard.png) |

### 控制台客户端 (交互式 Terminal CLI)

| 终端登录交互 | 控制台交互式仪表盘 |
| :---: | :---: |
| ![NightAuth Console Login](../image/console.png) | ![NightAuth Console Dashboard](../image/console-dashboard.png) |

---

## 🛡️ 推荐安全防护实践

* **原生 Native AOT 编译：** C# 的 IL 中间语言极易被 dnSpy 或 ILSpy 等反编译工具还原。对于生产环境发布，推荐使用 **Native AOT** 提前编译 (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`)。
* **企业级代码虚拟化保护：** 使用商业级保护器或混淆器（如 **VMProtect**、**Themida**、**Eazfuscator.NET** 或 **Obfuscar**）对生成的二进制程序进行保护。
* **内存完整性与反调试防护：** NightAuth 提供 `SecurityProtections` 工具类，包含针对系统代理、调试器以及黑名单进程（`HTTPDebugger`、`CheatEngine`、`x64dbg`、`Wireshark`、`dnSpy` 等）的检测。默认情况下仅执行代理检测；如需完整防护，请在启动时手动调用 `CheckDebugger()` 和 `CheckBlacklistedProcesses()`（见 [SecurityProtections](#22-securityprotections)）。
* **内存中安全执行与加载：** 使用 `NightAuthApp.GetFileAsync()` 下载受保护的核心二进制文件时，**切勿将未加密的字节写入物理磁盘**。请直接在内存中执行载入（如 `Assembly.Load()` 或反射加载技术），使用完毕后立即清除内存缓冲区 (`CryptographicOperations.ZeroMemory()`)。

---

## 📦 依赖环境与快速安装

需要 **.NET 10.0 SDK**（或更高版本）。

```bash
dotnet build NightAuth.sln
```

---

## ⚙️ 应用程序配置与客户端实例化

在 NightAuth 管理后台的 **Manage Apps** 选项卡中获取应用凭证：

```csharp
public static api NightAuthApp = new api(
    name: "",
    apikey: "",
    version: "",
    token: null
);
```

### 初始化加密握手 (`InitializeAsync`)

在调用其他任何 SDK 方法之前，必须先调用 `InitializeAsync()`。该方法将完成 Diffie-Hellman (X25519) 密钥交换并建立端到端加密会话：

```csharp
var init = await NightAuthApp.InitializeAsync();

if (!init.Success)
{
    Console.WriteLine($"初始化失败: {init.DisplayMessage}");
    Environment.Exit(1);
}
```

---

## 🚀 SDK 核心功能使用指南

### 1. 验证会话与黑名单检查

```csharp
// 校验当前活跃会话状态
var check = await NightAuthApp.CheckAsync();
Console.WriteLine($"会话状态: {check.DisplayMessage}");

// 检查当前设备或 IP 是否已被拉黑
var blacklist = await NightAuthApp.CheckBlacklistAsync();
if (blacklist.Success && blacklist.Payload["blacklisted"]?.GetValue<bool>() == true)
{
    Console.WriteLine("拒绝访问: 当前设备或 IP 地址已被加入黑名单。");
    Environment.Exit(0);
}
```

### 2. 账号密码登录 (支持 2FA 双重认证)

```csharp
var result = await NightAuthApp.LoginAsync(username, password, totpCode);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"欢迎回来, {user.Username}!");
}
else
{
    Console.WriteLine($"登录失败: {result.DisplayMessage}");
}
```

### 3. 使用授权密钥注册新用户

```csharp
var result = await NightAuthApp.RegisterAsync(username, password, licenseKey, email);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"注册成功: {user.Username}");
}
else
{
    Console.WriteLine($"注册失败: {result.DisplayMessage}");
}
```

### 4. 仅凭卡密激活登录 (Key Only)

```csharp
var result = await NightAuthApp.LicenseAsync(licenseKey);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine("卡密验证成功，已授予访问权限。");
}
else
{
    Console.WriteLine($"访问被拒绝: {result.DisplayMessage}");
}
```

### 5. 升级账号 / 激活充值新卡密 (Upgrade)

```csharp
var result = await NightAuthApp.UpgradeAsync(username, newLicenseKey);

if (result.Success)
{
    Console.WriteLine("订阅已成功升级！");
}
else
{
    Console.WriteLine($"升级失败: {result.DisplayMessage}");
}
```

### 6. 重置机器码绑定 (HWID Reset)

```csharp
var result = await NightAuthApp.ResetHwidAsync(username, password, totpCode);

if (result.Success)
{
    Console.WriteLine("HWID 绑定已成功重置，现在可在新设备登录。");
}
else
{
    Console.WriteLine($"重置 HWID 失败: {result.DisplayMessage}");
}
```

### 7. 找回密码与重置 (Forgot Password & Reset)

```csharp
// 发送重置密码邮件
var forgot = await NightAuthApp.ForgotPasswordAsync(email);
if (forgot.Success)
{
    Console.WriteLine("密码重置邮件已发送，请查收邮箱。");
}

// 凭借恢复 Token 设置新密码
var reset = await NightAuthApp.ResetPasswordAsync(token, newPassword);
if (reset.Success)
{
    Console.WriteLine("密码已成功重置。");
}
```

### 8. 用户档案与授权信息 (`result.ToUserData()`)

```csharp
UserData user = result.ToUserData();

Console.WriteLine($"用户名:   {user.Username}");
Console.WriteLine($"IP 地址:  {user.Ip}");
Console.WriteLine($"硬件 HWID: {user.Hwid}");
Console.WriteLine($"注册时间: {user.CreatedAt}");
Console.WriteLine($"上次登录: {user.LastLoginAt}");

foreach (var sub in user.Subscriptions)
{
    Console.WriteLine($"方案: {sub.Subscription} | 到期: {sub.Expiry} | 密钥: {sub.Key}");
}
```

### 9. 云端全局变量 (Cloud Variables)

```csharp
var res = await NightAuthApp.GetGlobalVarAsync("server_motd");
string motd = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"云端变量: {motd}");
```

### 10. 用户专属隔离变量 (User Variables)

```csharp
// 写入用户变量
await NightAuthApp.SetUserVarAsync("discord_tag", "user#0001");

// 读取用户变量
var res = await NightAuthApp.GetUserVarAsync("discord_tag");
string tag = res.Success ? res.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"读取到的 Discord: {tag}");
```

### 11. 远程安全审计日志

```csharp
await NightAuthApp.LogAsync("检测到可疑内存篡改行为。");
```

### 12. 封禁用户并拉黑当前设备

```csharp
await NightAuthApp.BanAsync("检测到反作弊异常注入行为。");
```

### 13. 服务端安全 Webhook 转发

```csharp
var body = new JsonObject { ["content"] = "通知内容" };
var resp = await NightAuthApp.TriggerWebhookAsync("7kR0UedlVI", body);
Console.WriteLine($"Webhook 返回状态: HTTP {(int)resp.StatusCode}");
```

### 14. 内存中安全下载文件

```csharp
var res = await NightAuthApp.GetFileAsync("385624");

if (res.Success)
{
    byte[] fileBytes = Convert.FromBase64String(res.Payload["data"]?.ToString() ?? "");
    Console.WriteLine($"已安全下载 {fileBytes.Length} 字节。");
    // 直接在内存中反射执行（如 Assembly.Load）
}
```

### 15. 实时聊天频道

```csharp
// 发送消息
await NightAuthApp.SendChatMessageAsync("大家好！", channel: "general");

// 获取历史消息
var res = await NightAuthApp.GetChatMessagesAsync("general");
if (res.Success && res.Payload["messages"] is JsonArray messages)
{
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg?["author"]}]: {msg?["message"]}");
    }
}
```

### 16. 实时服务端推送事件处理 (SSE)

```csharp
using var cts = new CancellationTokenSource();

await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
{
    Console.WriteLine($"[事件推送] {evt}");
}
```

### 17. 双重认证管理 (2FA)

```csharp
// 开启 TOTP 2FA
var setup = await NightAuthApp.Setup2FAAsync();
string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";

// 提交 6 位验证码确认绑定
var conf = await NightAuthApp.Confirm2FAAsync("123456");

// 关闭 2FA
var disable = await NightAuthApp.Disable2FAAsync(password, "123456");
```

### 18. 会话保活与安全登出

```csharp
// 发送心跳包保活会话
await NightAuthApp.HeartbeatAsync();

// 安全登出并撤销服务端会话
await NightAuthApp.LogoutAsync();
```

### 19. 同步 API

每个认证与管理调用在 `api` 中都有对应的同步版本（例如 `NightAuthApp`）：`Init`、`Login`、`Register`、`License`、`Upgrade`、`Heartbeat`、`Check`、`Logout`、`ChangeUsername`、`ResetHwid`、`Ban`、`CheckBlacklist`、`VerifyFileHash`、`ForgotPassword`、`ResetPassword`、`Setup2FA`、`Confirm2FA`、`Disable2FA`、`GetGlobalVar`、`GetUserVar`、`SetUserVar`、`DownloadFile`、`GetChatMessages`、`SendChatMessage`、`Log`、`GetStats`、`GetOnlineUsers` 以及 `TriggerWebhook`。它们会阻塞当前线程，并通过 `Response`/`User`/`App` 返回结果，而不是返回 `NightAuthResult`：

```csharp
NightAuthApp.Init();

if (NightAuthApp.Login(username, password, totpCode))
{
    Console.WriteLine($"欢迎回来，{NightAuthApp.User.Username}!");
}
else
{
    Console.WriteLine($"登录失败：{NightAuthApp.Response.Message}");
}
```

同步 API 暴露的实用状态：`Response`、`User`（别名 `user_data`）、`App`（别名 `app_data`）、`LatestVersion`、`ApiUrl`、`PendingDownloadUrl` 以及用于从当前实例派生 `NightAuthOptions` 的 `ToOptions()`。

### 20. 实时事件处理器

`api` 暴露了由实时引擎触发的高层事件，并可显式控制后台监听器：

```csharp
NightAuthApp.OnSessionInvalidated += reason => { Console.WriteLine($"会话已失效：{reason}"); Environment.Exit(0); };
NightAuthApp.OnAppBanned += reason => Console.WriteLine($"应用已被封禁：{reason}");
NightAuthApp.OnAppStatusChanged += status => Console.WriteLine($"应用状态已变更：{status}");
NightAuthApp.OnVersionUpdated += version => Console.WriteLine($"有新版本可用：{version}");
NightAuthApp.OnChatMessageReceived += (msg, channel) => Console.WriteLine($"[{channel}] {msg.Author}: {msg.Message}");

NightAuthApp.StartRealtimeEventListener(); // 后台 SSE 监听器
// ... 之后
NightAuthApp.StopRealtimeEventListener();
```

如需完全控制，也可以使用 `ReadEventsAsync` 手动消费 SSE 流（见第 16 节）。

### 21. 独立 `NightAuthClient`

现代化的异步引擎可作为独立客户端使用，与旧版 `api` 封装解耦：

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
    // NightAuthResult 暴露 Payload (JsonObject)、RetryAfter 和 Error
    string tag = login.Payload["user"]?["username"]?.GetValue<string>() ?? "";
}
```

`NightAuthClient` 与 `api` 均可使用的额外异步辅助方法：`ChangeUsernameAsync`、`GetUserVariablesAsync`（列出全部用户变量）、`VerifyFileHashAsync`、`GetStatsAsync`、`GetOnlineUsersAsync`，以及三个 `TriggerWebhookAsync` 重载（JSON 负载、带自定义 `content`/`parameters` 的 JSON，以及带文件的多部分上传：`TriggerWebhookAsync(id, fileBytes, fileName, payload, content, parameters)`）。

### 22. `SecurityProtections`

静态反篡改辅助方法。`RunSecurityChecks()`（由 `Init` 自动调用）默认仅执行系统代理检测；请显式调用其余检测：

```csharp
SecurityProtections.CheckSystemProxy();          // 若存在活动的系统 HTTP 代理则终止进程
SecurityProtections.CheckDebugger();             // 若附加了调试器（本地或远程）则终止进程
SecurityProtections.CheckBlacklistedProcesses(); // 扫描分析工具（HTTPDebugger、Cheat Engine、x64dbg、Wireshark、dnSpy 等）

string hwid = SecurityProtections.GetHardwareId(); // 用于 HWID 锁定的稳定硬件标识符
```

> [!NOTE]
> 设置环境变量 `NIGHTAUTH_IGNORE_SECURITY=1` 可在开发/测试期间跳过黑名单进程扫描。

### 23. 签名自动更新

未签名的自动更新（`ApplyUpdate`）已废弃并被禁用。请改为下载并验证签名版本：

```csharp
// 一站式辅助方法：通过 HTTPS 获取清单，根据固定的发布密钥验证 Ed25519 签名，
// 拒绝过期的序列号，下载产物文件并返回安装后的文件路径。
string path = await SignedUpdateInstaller.DownloadAsync(
    manifestUri: new Uri("https://releases.example.com/manifest.json"),
    pinnedReleaseKeyHex: "YOUR_PINNED_ED25519_PUBLIC_KEY_HEX",
    application: "my-app",
    destination: "updates/my-app.exe");

// 或者手动控制整个流程：
var verified = SignedUpdate.Verify(envelopeJson, pinnedKeyHex, expectedApplication, minimumSequence);
Console.WriteLine($"已验证 {verified.Application} v{verified.Version} (seq {verified.Sequence})");
await SignedUpdate.DownloadAsync(verified, "updates/my-app.exe");
```

---

## 🖥️ 运行参考客户端

在 `source` 目录下直接运行：

```bash
# 启动交互式终端控制台客户端
dotnet run --project console/NightAuth.Console.csproj

# 启动现代化 WPF 桌面客户端 (仅限 Windows)
dotnet run --project desktop/NightAuth.Desktop.csproj
```

---

## 📄 授权协议

本项目基于 **Elastic License 2.0** 协议授权。

* 不得以托管或在线托管服务的形式向第三方提供本软件服务。
* 不得移动、修改、禁用或规避许可密钥验证相关功能。
* 不得修改、移除或遮挡任何许可与版权声明。

详情请参阅 [LICENSE](../LICENSE) 文件。
