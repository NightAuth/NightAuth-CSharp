# NightAuth C# .NET SDK — GitHub'da yıldız verin ⭐

Resmi **NightAuth** C# / .NET SDK & Referans İstemci Paketi (**WPF** ile modern masaüstü arayüzü ve terminal içi etkileşimli CLI). Yazılımınızı dakikalar içinde bulut lisanslama, HWID kilitleme, canlı sohbet ve bellekte güvenli çalıştırma ile korsanlığa ve tersine mühendisliğe karşı koruyun.

Resmi Portal: [https://nightauth.org](https://nightauth.org)

---

**Languages / Idiomas / Langues / Sprachen:**  
[English](../README.md) • [Español](es.md) • [Français](fr.md) • [Deutsch](de.md) • [Italiano](it.md) • [Português](pt.md) • [Русский](ru.md) • [Türkçe](tr.md) • [Polski](pl.md) • [中文](zh.md)

---

> [!NOTE]
> ### 📺 Video Eğitimleri & Kurulum Kılavuzları
> NightAuth Yönetim Panelinde uygulama, plan, abonelik, bulut değişkenleri, webhook ve lisansların nasıl yapılandırılacağını anlatan resmi video rehberleri ve adım adım kılavuzlar **yakında yayınlanacaktır**.

---

## 📸 Ekran Görüntüleri

### Masaüstü Uygulaması (WPF Enterprise Dark Theme)

| Giriş ve Kimlik Doğrulama Ekranı | Kullanıcı Paneli ve Abonelik Yönetimi |
| :---: | :---: |
| ![NightAuth Desktop Login](../image/desktop.png) | ![NightAuth Desktop Dashboard](../image/desktop-dashboard.png) |

### Konsol Uygulaması (Etkileşimli Terminal CLI)

| Terminal Giriş Ekranı | Etkileşimli Konsol Paneli |
| :---: | :---: |
| ![NightAuth Console Login](../image/console.png) | ![NightAuth Console Dashboard](../image/console-dashboard.png) |

---

## 🛡️ Önerilen Güvenlik Uygulamaları

* **Native AOT Derleme:** C# IL bayt kodu dnSpy veya ILSpy gibi araçlarla kolayca kaynak koduna dönüştürülebilir. Canlı dağıtımlar için **Native AOT** (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`) kullanın.
* **Kurumsal Kod Sanallaştırma:** Derlenen ikili dosyaları **VMProtect**, **Themida**, **Eazfuscator.NET** veya **Obfuscar** gibi profesyonel araçlarla koruyun.
* **Bellek Bütünlüğü & Hata Ayıklayıcı Koruması:** NightAuth, sistem proxy'leri, hata ayıklayıcılar ve kara listedeki süreçlere karşı denetimler içeren bir `SecurityProtections` yardımcısı sunar (`HTTPDebugger`, `CheatEngine`, `x64dbg`, `Wireshark`, `dnSpy`). Varsayılan olarak yalnızca proxy kontrolü çalışır; tam koruma için `CheckDebugger()` ve `CheckBlacklistedProcesses()` çağrılarını başlangıçta manuel olarak yapın (bkz. [SecurityProtections](#22-securityprotections)).
* **Bellekte Güvenli Yürütme:** `NightAuthApp.GetFileAsync()` ile indirilen dosyaları **asla diske kaydetmeyin**. Yürütülebilir baytları doğrudan RAM'e yükleyin (örn. `Assembly.Load()`) ve bellek arabelleklerini (`CryptographicOperations.ZeroMemory()`) hemen temizleyin.

---

## 📦 Gereksinimler & Kurulum

**.NET 10.0 SDK** (veya üstü) gereklidir.

```bash
dotnet build NightAuth.sln
```

---

## ⚙️ Yapılandırma & Başlatma

NightAuth panelindeki uygulama bilgilerinizi tanımlayın:

```csharp
public static api NightAuthApp = new api(
    name: "",
    apikey: "",
    version: "",
    token: null
);
```

### Kriptografik El Sıkışmayı Başlatma (`InitializeAsync`)

Tüm işlemlerden önce `InitializeAsync()` çağrılmalıdır. Diffie-Hellman (X25519) anahtar değişimi ve şifreli oturumu kurar:

```csharp
var init = await NightAuthApp.InitializeAsync();

if (!init.Success)
{
    Console.WriteLine($"Başlatma başarısız: {init.DisplayMessage}");
    Environment.Exit(1);
}
```

---

## 🚀 SDK Kullanım Kılavuzu

### 1. Oturum Kontrolü & Kara Liste Durumu

```csharp
// Aktif oturum durumunu doğrula
var check = await NightAuthApp.CheckAsync();
Console.WriteLine($"Oturum Durumu: {check.DisplayMessage}");

// Cihazın veya IP adresinin kara listede olup olmadığını kontrol et
var blacklist = await NightAuthApp.CheckBlacklistAsync();
if (blacklist.Success && blacklist.Payload["blacklisted"]?.GetValue<bool>() == true)
{
    Console.WriteLine("Erişim Reddedildi: Cihaz veya IP engellendi.");
    Environment.Exit(0);
}
```

### 2. Kullanıcı Adı & Parola ile Giriş (ve 2FA)

```csharp
var result = await NightAuthApp.LoginAsync(username, password, totpCode);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Tekrar hoş geldiniz, {user.Username}!");
}
else
{
    Console.WriteLine($"Giriş başarısız: {result.DisplayMessage}");
}
```

### 3. Lisans Anahtarı ile Kayıt

```csharp
var result = await NightAuthApp.RegisterAsync(username, password, licenseKey, email);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Başarıyla kaydedildi: {user.Username}");
}
else
{
    Console.WriteLine($"Kayıt başarısız: {result.DisplayMessage}");
}
```

### 4. Sadece Lisans Anahtarı ile Giriş (Key Only)

```csharp
var result = await NightAuthApp.LicenseAsync(licenseKey);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine("Lisans doğrulandı. Erişim onaylandı.");
}
else
{
    Console.WriteLine($"Erişim engellendi: {result.DisplayMessage}");
}
```

### 5. Abonelik Yükseltme / Anahtar Kullanma (Upgrade)

```csharp
var result = await NightAuthApp.UpgradeAsync(username, newLicenseKey);

if (result.Success)
{
    Console.WriteLine("Abonelik başarıyla yükseltildi.");
}
else
{
    Console.WriteLine($"Yükseltme başarısız: {result.DisplayMessage}");
}
```

### 6. Donanım Kimliği Sıfırlama (HWID Reset)

```csharp
var result = await NightAuthApp.ResetHwidAsync(username, password, totpCode);

if (result.Success)
{
    Console.WriteLine("HWID kilidi başarıyla sıfırlandı.");
}
else
{
    Console.WriteLine($"HWID sıfırlama başarısız: {result.DisplayMessage}");
}
```

### 7. Şifremi Unuttum (Forgot Password & Reset)

```csharp
// Sıfırlama e-postası iste
var forgot = await NightAuthApp.ForgotPasswordAsync(email);
if (forgot.Success)
{
    Console.WriteLine("Şifre sıfırlama e-postası gönderildi.");
}

// Belirteç ile şifreyi güncelle
var reset = await NightAuthApp.ResetPasswordAsync(token, newPassword);
if (reset.Success)
{
    Console.WriteLine("Şifre başarıyla güncellendi.");
}
```

### 8. Kullanıcı Profil Bilgileri (`result.ToUserData()`)

```csharp
UserData user = result.ToUserData();

Console.WriteLine($"Kullanıcı:   {user.Username}");
Console.WriteLine($"IP:          {user.Ip}");
Console.WriteLine($"HWID:        {user.Hwid}");
Console.WriteLine($"Kayıt:       {user.CreatedAt}");
Console.WriteLine($"Son Giriş:   {user.LastLoginAt}");

foreach (var sub in user.Subscriptions)
{
    Console.WriteLine($"Plan: {sub.Subscription} | Bitiş: {sub.Expiry} | Anahtar: {sub.Key}");
}
```

### 9. Bulut Değişkenleri (Genel)

```csharp
var res = await NightAuthApp.GetGlobalVarAsync("server_motd");
string motd = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Bulut Değişkeni: {motd}");
```

### 10. Kullanıcı Değişkenleri (User Variables)

```csharp
// Değişken kaydet
await NightAuthApp.SetUserVarAsync("discord_tag", "user#0001");

// Değişken oku
var res = await NightAuthApp.GetUserVarAsync("discord_tag");
string tag = res.Success ? res.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Kayıtlı Discord: {tag}");
```

### 11. Uygulama Denetim Günlükleri

```csharp
await NightAuthApp.LogAsync("Şüpheli bellek değişikliği tespit edildi.");
```

### 12. Kullanıcı Engelleme & Cihazı Yasaklama

```csharp
await NightAuthApp.BanAsync("Anti-Cheat tarafından güvenlik ihlali algılandı.");
```

### 13. Sunucu Taraflı Webhook'lar

```csharp
var body = new JsonObject { ["content"] = "Bildirim" };
var resp = await NightAuthApp.TriggerWebhookAsync("7kR0UedlVI", body);
Console.WriteLine($"Webhook Yanıtı: HTTP {(int)resp.StatusCode}");
```

### 14. Bellekte Güvenli Dosya İndirme

```csharp
var res = await NightAuthApp.GetFileAsync("385624");

if (res.Success)
{
    byte[] fileBytes = Convert.FromBase64String(res.Payload["data"]?.ToString() ?? "");
    Console.WriteLine($"{fileBytes.Length} bayt güvenle indirildi.");
    // Doğrudan bellekte çalıştırın (örn. Assembly.Load)
}
```

### 15. Canlı Sohbet Kanalları

```csharp
// Mesaj gönder
await NightAuthApp.SendChatMessageAsync("Selam topluluk!", channel: "general");

// Mesajları getir
var res = await NightAuthApp.GetChatMessagesAsync("general");
if (res.Success && res.Payload["messages"] is JsonArray messages)
{
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg?["author"]}]: {msg?["message"]}");
    }
}
```

### 16. Gerçek Zamanlı Olay Dinleyicileri (SSE)

```csharp
using var cts = new CancellationTokenSource();

await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
{
    Console.WriteLine($"[OLAY] {evt}");
}
```

### 17. İki Faktörlü Kimlik Doğrulama (2FA)

```csharp
// TOTP 2FA kurulumu
var setup = await NightAuthApp.Setup2FAAsync();
string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";

// 6 haneli kod ile onayla
var conf = await NightAuthApp.Confirm2FAAsync("123456");

// 2FA devre dışı bırak
var disable = await NightAuthApp.Disable2FAAsync(password, "123456");
```

### 18. Oturum Yaşam Döngüsü & Çıkış

```csharp
// Oturumu canlı tutmak için heartbeat
await NightAuthApp.HeartbeatAsync();

// Oturumu sonlandır ve iptal et
await NightAuthApp.LogoutAsync();
```

### 19. Senkron API

Kimlik doğrulama ve yönetim çağrılarının her biri `api` üzerinde senkron bir karşılığa sahiptir (örn. `NightAuthApp`): `Init`, `Login`, `Register`, `License`, `Upgrade`, `Heartbeat`, `Check`, `Logout`, `ChangeUsername`, `ResetHwid`, `Ban`, `CheckBlacklist`, `VerifyFileHash`, `ForgotPassword`, `ResetPassword`, `Setup2FA`, `Confirm2FA`, `Disable2FA`, `GetGlobalVar`, `GetUserVar`, `SetUserVar`, `DownloadFile`, `GetChatMessages`, `SendChatMessage`, `Log`, `GetStats`, `GetOnlineUsers` ve `TriggerWebhook`. Mevcut thread'i engeller ve `NightAuthResult` döndürmek yerine sonucu `Response`/`User`/`App` üzerinden bildirir:

```csharp
NightAuthApp.Init();

if (NightAuthApp.Login(username, password, totpCode))
{
    Console.WriteLine($"Tekrar hoş geldin, {NightAuthApp.User.Username}!");
}
else
{
    Console.WriteLine($"Giriş başarısız: {NightAuthApp.Response.Message}");
}
```

Senkron API'nin sunduğu yararlı durum: `Response`, `User` (alias `user_data`), `App` (alias `app_data`), `LatestVersion`, `ApiUrl`, `PendingDownloadUrl` ve mevcut örnekten `NightAuthOptions` türetmek için `ToOptions()`.

### 20. Gerçek Zamanlı Olay İşleyicileri

`api`, gerçek zamanlı motorun tetiklediği üst düzey olayları ve arka plan dinleyicisi üzerinde açık kontrolü sunar:

```csharp
NightAuthApp.OnSessionInvalidated += reason => { Console.WriteLine($"Oturum geçersiz kılındı: {reason}"); Environment.Exit(0); };
NightAuthApp.OnAppBanned += reason => Console.WriteLine($"Uygulama yasaklandı: {reason}");
NightAuthApp.OnAppStatusChanged += status => Console.WriteLine($"Uygulama durumu değişti: {status}");
NightAuthApp.OnVersionUpdated += version => Console.WriteLine($"Yeni sürüm mevcut: {version}");
NightAuthApp.OnChatMessageReceived += (msg, channel) => Console.WriteLine($"[{channel}] {msg.Author}: {msg.Message}");

NightAuthApp.StartRealtimeEventListener(); // arka plan SSE dinleyicisi
// ... daha sonra
NightAuthApp.StopRealtimeEventListener();
```

Tam kontrol için SSE akışını `ReadEventsAsync` ile manuel olarak da tüketebilirsiniz (bkz. bölüm 16).

### 21. Bağımsız `NightAuthClient`

Modern async motoru, eski `api` sarmalayıcısından ayrılmış bağımsız bir istemci olarak kullanılabilir:

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
    // NightAuthResult; Payload (JsonObject), RetryAfter ve Error sunar
    string tag = login.Payload["user"]?["username"]?.GetValue<string>() ?? "";
}
```

Hem `NightAuthClient` hem de `api` üzerinde ek async yardımcılar: `ChangeUsernameAsync`, `GetUserVariablesAsync` (tüm kullanıcı değişkenlerini listeler), `VerifyFileHashAsync`, `GetStatsAsync`, `GetOnlineUsersAsync` ve üç `TriggerWebhookAsync` aşırı yüklemesi (JSON payload, özelleştirilmiş `content`/`parameters` ile JSON ve dosyalı multipart yükleme: `TriggerWebhookAsync(id, fileBytes, fileName, payload, content, parameters)`).

### 22. `SecurityProtections`

Statik kurcalama karşıtı yardımcılar. `RunSecurityChecks()` (`Init` tarafından otomatik çağrılır) yalnızca sistem proxy kontrolünü yapar; diğer kontrolleri açıkça çağırın:

```csharp
SecurityProtections.CheckSystemProxy();          // aktif sistem HTTP proxy'sinde sonlandırır
SecurityProtections.CheckDebugger();             // bağlı hata ayıklayıcıda (yerel veya uzak) sonlandırır
SecurityProtections.CheckBlacklistedProcesses(); // analiz araçlarını tarar (HTTPDebugger, Cheat Engine, x64dbg, Wireshark, dnSpy, ...)

string hwid = SecurityProtections.GetHardwareId(); // HWID kilitlemede kullanılan kararlı donanım kimliği
```

> [!NOTE]
> Geliştirme/test sırasında kara liste süreç taramasını atlamak için `NIGHTAUTH_IGNORE_SECURITY=1` ortam değişkenini ayarlayın.

### 23. İmzalı Otomatik Güncellemeler

İmzasız otomatik güncelleme (`ApplyUpdate`) kullanımdan kaldırılmıştır ve devre dışıdır. Bunun yerine imzalı sürümleri indirip doğrulayın:

```csharp
// Tek adımlık yardımcı: manifestoyu HTTPS üzerinden indirir, Ed25519 imzasını
// sabitlenmiş sürüm anahtarınıza karşı doğrular, eski dizileri reddeder, yapıyı
// indirir ve kurulan dosyanın yolunu döndürür.
string path = await SignedUpdateInstaller.DownloadAsync(
    manifestUri: new Uri("https://releases.example.com/manifest.json"),
    pinnedReleaseKeyHex: "YOUR_PINNED_ED25519_PUBLIC_KEY_HEX",
    application: "my-app",
    destination: "updates/my-app.exe");

// Veya süreci manuel yönetin:
var verified = SignedUpdate.Verify(envelopeJson, pinnedKeyHex, expectedApplication, minimumSequence);
Console.WriteLine($"Doğrulandı {verified.Application} v{verified.Version} (sıra {verified.Sequence})");
await SignedUpdate.DownloadAsync(verified, "updates/my-app.exe");
```

---

## 🖥️ Örnek İstemcileri Çalıştırma

`source` dizininden istemcileri başlatın:

```bash
# Etkileşimli Terminal Konsol İstemcisi
dotnet run --project console/NightAuth.Console.csproj

# Modern WPF Masaüstü İstemcisi (Windows)
dotnet run --project desktop/NightAuth.Desktop.csproj
```

---

## 📄 Lisans

Bu proje **Elastic License 2.0** kapsamında lisanslanmıştır.

* Yazılımı üçüncü taraflara barındırılan veya yönetilen bir hizmet olarak sağlayamazsınız.
* Lisans anahtarı işlevselliğini değiştiremez, devre dışı bırakamaz veya atlayamazsınız.
* Lisans veya telif hakkı bildirimlerini kaldıramazsınız.

Tam metin için [LICENSE](../LICENSE) dosyasına bakın.
