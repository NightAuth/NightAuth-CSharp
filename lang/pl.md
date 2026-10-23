# NightAuth C# .NET SDK — Zostaw gwiazdkę na GitHubie ⭐

Oficjalny pakiet SDK **NightAuth** dla C# / .NET i zestaw aplikacji referencyjnych (nowoczesny interfejs graficzny **WPF** oraz interaktywny terminal CLI). Zabezpiecz swoje oprogramowanie przed piractwem, inżynierią wsteczną i manipulacją dzięki licencjonowaniu w chmurze, blokadzie HWID, czatowi na żywo i strumieniowaniu w pamięci RAM w kilka minut.

Oficjalny portal: [https://nightauth.org](https://nightauth.org)

---

**Languages / Idiomas / Langues / Sprachen:**  
[English](../README.md) • [Español](es.md) • [Français](fr.md) • [Deutsch](de.md) • [Italiano](it.md) • [Português](pt.md) • [Русский](ru.md) • [Türkçe](tr.md) • [Polski](pl.md) • [中文](zh.md)

---

> [!NOTE]
> ### 📺 Poradniki wideo i instrukcje konfiguracji
> Oficjalne poradniki wideo i przewodniki krok po kroku opisujące konfigurację aplikacji, planów, subskrypcji, zmiennych chmurowych, webhooków i licencji w panelu NightAuth **zostaną wkrótce opublikowane**.

---

## 📸 Zrzuty ekranu

### Aplikacja desktopowa (WPF Enterprise Dark Theme)

| Ekran logowania i autoryzacji | Panel zarządzania i subskrypcji |
| :---: | :---: |
| ![NightAuth Desktop Login](../image/desktop.png) | ![NightAuth Desktop Dashboard](../image/desktop-dashboard.png) |

### Aplikacja konsolowa (Interaktywny terminal CLI)

| Logowanie w terminalu | Panel interaktywny w konsoli |
| :---: | :---: |
| ![NightAuth Console Login](../image/console.png) | ![NightAuth Console Dashboard](../image/console-dashboard.png) |

---

## 🛡️ Zalecane praktyki bezpieczeństwa

* **Natywna kompilacja AOT:** Kod bajtowy C# IL można łatwo zdekompilować za pomocą narzędzi takich jak dnSpy lub ILSpy. W wersjach produkcyjnych publikuj z użyciem **Native AOT** (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`).
* **Wirtualizacja kodu:** Zabezpieczaj pliki binarne za pomocą profesjonalnych protektorów i obfuskatorów, takich jak **VMProtect**, **Themida**, **Eazfuscator.NET** lub **Obfuscar**.
* **Integralność pamięci i ochrona przed debugowaniem:** NightAuth udostępnia helper `SecurityProtections` z weryfikacją proxy systemowego, debuggerów i procesów z czarnej listy (`HTTPDebugger`, `CheatEngine`, `x64dbg`, `Wireshark`, `dnSpy`). Domyślnie wykonywana jest tylko kontrola proxy; wywołaj `CheckDebugger()` i `CheckBlacklistedProcesses()` ręcznie przy starcie, aby uzyskać pełną ochronę (zobacz [SecurityProtections](#22-securityprotections)).
* **Bezpieczne wykonywanie w pamięci RAM:** Pobierając zabezpieczone pliki za pomocą `NightAuthApp.GetFileAsync()`, **nie zapisuj bajtów na dysku**. Ładuj i wykonuj pliki bezpośrednio w pamięci RAM (np. przez `Assembly.Load()`) oraz czyść bufory pamięci (`CryptographicOperations.ZeroMemory()`) natychmiast po użyciu.

---

## 📦 Wymagania i instalacja

Wymaga **.NET 10.0 SDK** (lub nowszego).

```bash
dotnet build NightAuth.sln
```

---

## ⚙️ Konfiguracja i inicjalizacja

Skonfiguruj dane uwierzytelniające aplikacji z panelu NightAuth:

```csharp
public static api NightAuthApp = new api(
    name: "",
    apikey: "",
    version: "",
    token: null
);
```

### Inicjalizacja uzgadniania kryptograficznego (`InitializeAsync`)

Wywołaj `InitializeAsync()` przed wszelkimi innymi operacjami. Tworzy wymianę kluczy Diffie-Hellmana (X25519) i szyfrowaną sesję:

```csharp
var init = await NightAuthApp.InitializeAsync();

if (!init.Success)
{
    Console.WriteLine($"Błąd inicjalizacji: {init.DisplayMessage}");
    Environment.Exit(1);
}
```

---

## 🚀 Przewodnik po SDK

### 1. Sprawdzenie sesji i czarna lista

```csharp
// Walidacja aktywnej sesji
var check = await NightAuthApp.CheckAsync();
Console.WriteLine($"Status sesji: {check.DisplayMessage}");

// Sprawdzenie, czy urządzenie lub IP znajduje się na czarnej liście
var blacklist = await NightAuthApp.CheckBlacklistAsync();
if (blacklist.Success && blacklist.Payload["blacklisted"]?.GetValue<bool>() == true)
{
    Console.WriteLine("Dostęp zablokowany: Urządzenie lub adres IP na czarnej liście.");
    Environment.Exit(0);
}
```

### 2. Logowanie loginem i hasłem (oraz 2FA)

```csharp
var result = await NightAuthApp.LoginAsync(username, password, totpCode);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Witaj ponownie, {user.Username}!");
}
else
{
    Console.WriteLine($"Błąd logowania: {result.DisplayMessage}");
}
```

### 3. Rejestracja z kluczem licencyjnym

```csharp
var result = await NightAuthApp.RegisterAsync(username, password, licenseKey, email);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Zarejestrowano pomyślnie: {user.Username}");
}
else
{
    Console.WriteLine($"Błąd rejestracji: {result.DisplayMessage}");
}
```

### 4. Dostęp tylko z kluczem (Key Only)

```csharp
var result = await NightAuthApp.LicenseAsync(licenseKey);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine("Licencja zweryfikowana. Dostęp przyznany.");
}
else
{
    Console.WriteLine($"Brak dostępu: {result.DisplayMessage}");
}
```

### 5. Aktualizacja subskrypcji / Użycie klucza (Upgrade)

```csharp
var result = await NightAuthApp.UpgradeAsync(username, newLicenseKey);

if (result.Success)
{
    Console.WriteLine("Subskrypcja została pomyślnie zaktualizowana.");
}
else
{
    Console.WriteLine($"Błąd aktualizacji: {result.DisplayMessage}");
}
```

### 6. Reset identyfikatora sprzętowego (HWID Lock)

```csharp
var result = await NightAuthApp.ResetHwidAsync(username, password, totpCode);

if (result.Success)
{
    Console.WriteLine("Blokada HWID zresetowana. Możesz zalogować się na tym komputerze.");
}
else
{
    Console.WriteLine($"Błąd resetu HWID: {result.DisplayMessage}");
}
```

### 7. Przypomnienie hasła (Forgot Password & Reset)

```csharp
// Żądanie linku resetującego na e-mail
var forgot = await NightAuthApp.ForgotPasswordAsync(email);
if (forgot.Success)
{
    Console.WriteLine("Wysłano instrukcję resetowania hasła na e-mail.");
}

// Potwierdzenie tokenu i nowe hasło
var reset = await NightAuthApp.ResetPasswordAsync(token, newPassword);
if (reset.Success)
{
    Console.WriteLine("Hasło zostało pomyślnie zaktualizowane.");
}
```

### 8. Dane profilu użytkownika (`result.ToUserData()`)

```csharp
UserData user = result.ToUserData();

Console.WriteLine($"Użytkownik:     {user.Username}");
Console.WriteLine($"Adres IP:       {user.Ip}");
Console.WriteLine($"HWID:           {user.Hwid}");
Console.WriteLine($"Rejestracja:    {user.CreatedAt}");
Console.WriteLine($"Ostatnie log.:  {user.LastLoginAt}");

foreach (var sub in user.Subscriptions)
{
    Console.WriteLine($"Plan: {sub.Subscription} | Wygasa: {sub.Expiry} | Klucz: {sub.Key}");
}
```

### 9. Zmienne w chmurze (Globalne)

```csharp
var res = await NightAuthApp.GetGlobalVarAsync("server_motd");
string motd = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Zmienna chmurowa: {motd}");
```

### 10. Zmienne użytkownika (User Variables)

```csharp
// Zapisz zmienną
await NightAuthApp.SetUserVarAsync("discord_tag", "user#0001");

// Odczytaj zmienną
var res = await NightAuthApp.GetUserVarAsync("discord_tag");
string tag = res.Success ? res.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Zapisany Discord: {tag}");
```

### 11. Dziennik audytu aplikacji

```csharp
await NightAuthApp.LogAsync("Wykryto podejrzaną próbę modyfikacji pamięci.");
```

### 12. Blokowanie użytkownika i urządzenia

```csharp
await NightAuthApp.BanAsync("Naruszenie integralności wykryte przez Anti-Cheat.");
```

### 13. Webhooki po stronie serwera

```csharp
var body = new JsonObject { ["content"] = "Powiadomienie" };
var resp = await NightAuthApp.TriggerWebhookAsync("7kR0UedlVI", body);
Console.WriteLine($"Odpowiedź Webhook: HTTP {(int)resp.StatusCode}");
```

### 14. Bezpieczne pobieranie plików w pamięci

```csharp
var res = await NightAuthApp.GetFileAsync("385624");

if (res.Success)
{
    byte[] fileBytes = Convert.FromBase64String(res.Payload["data"]?.ToString() ?? "");
    Console.WriteLine($"Bezpiecznie pobrano {fileBytes.Length} bajtów.");
    // Wykonaj bezpośrednio w pamięci RAM (np. Assembly.Load)
}
```

### 15. Kanały czatu na żywo

```csharp
// Wyślij wiadomość
await NightAuthApp.SendChatMessageAsync("Cześć wszystkim!", channel: "general");

// Pobierz wiadomości
var res = await NightAuthApp.GetChatMessagesAsync("general");
if (res.Success && res.Payload["messages"] is JsonArray messages)
{
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg?["author"]}]: {msg?["message"]}");
    }
}
```

### 16. Obsługa zdarzeń w czasie rzeczywistym (SSE)

```csharp
using var cts = new CancellationTokenSource();

await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
{
    Console.WriteLine($"[ZDARZENIE] {evt}");
}
```

### 17. Uwierzytelnianie dwuskładnikowe (2FA)

```csharp
// Konfiguracja TOTP 2FA
var setup = await NightAuthApp.Setup2FAAsync();
string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";

// Potwierdzenie kodem 6-cyfrowym
var conf = await NightAuthApp.Confirm2FAAsync("123456");

// Wyłączenie 2FA
var disable = await NightAuthApp.Disable2FAAsync(password, "123456");
```

### 18. Cykl życia sesji i wylogowanie

```csharp
// Heartbeat podtrzymujący sesję
await NightAuthApp.HeartbeatAsync();

// Zamknięcie i unieważnienie sesji
await NightAuthApp.LogoutAsync();
```

### 19. API Synchroniczna

Każde wywołanie uwierzytelniania i zarządzania ma też synchroniczny odpowiednik w `api` (np. `NightAuthApp`): `Init`, `Login`, `Register`, `License`, `Upgrade`, `Heartbeat`, `Check`, `Logout`, `ChangeUsername`, `ResetHwid`, `Ban`, `CheckBlacklist`, `VerifyFileHash`, `ForgotPassword`, `ResetPassword`, `Setup2FA`, `Confirm2FA`, `Disable2FA`, `GetGlobalVar`, `GetUserVar`, `SetUserVar`, `DownloadFile`, `GetChatMessages`, `SendChatMessage`, `Log`, `GetStats`, `GetOnlineUsers` i `TriggerWebhook`. Blokują bieżący wątek i zgłaszają wyniki przez `Response`/`User`/`App` zamiast zwracać `NightAuthResult`:

```csharp
NightAuthApp.Init();

if (NightAuthApp.Login(username, password, totpCode))
{
    Console.WriteLine($"Witaj ponownie, {NightAuthApp.User.Username}!");
}
else
{
    Console.WriteLine($"Logowanie nieudane: {NightAuthApp.Response.Message}");
}
```

Przydatny stan udostępniany przez API synchroniczne: `Response`, `User` (alias `user_data`), `App` (alias `app_data`), `LatestVersion`, `ApiUrl`, `PendingDownloadUrl` oraz `ToOptions()` do utworzenia `NightAuthOptions` z bieżącej instancji.

### 20. Obsługa Zdarzeń w Czasie Rzeczywistym

`api` udostępnia wysokopoziomowe zdarzenia wywoływane przez silnik czasu rzeczywistego oraz pełną kontrolę nad nasłuchiwaniem w tle:

```csharp
NightAuthApp.OnSessionInvalidated += reason => { Console.WriteLine($"Sesja unieważniona: {reason}"); Environment.Exit(0); };
NightAuthApp.OnAppBanned += reason => Console.WriteLine($"Aplikacja zbanowana: {reason}");
NightAuthApp.OnAppStatusChanged += status => Console.WriteLine($"Zmieniono status aplikacji: {status}");
NightAuthApp.OnVersionUpdated += version => Console.WriteLine($"Dostępna nowa wersja: {version}");
NightAuthApp.OnChatMessageReceived += (msg, channel) => Console.WriteLine($"[{channel}] {msg.Author}: {msg.Message}");

NightAuthApp.StartRealtimeEventListener(); // nasłuch SSE w tle
// ... później
NightAuthApp.StopRealtimeEventListener();
```

Aby mieć pełną kontrolę, możesz też obsłużyć strumień SSE ręcznie za pomocą `ReadEventsAsync` (zobacz sekcja 16).

### 21. Niezależny `NightAuthClient`

Nowoczesny silnik asynchroniczny jest dostępny jako niezależny klient, odseparowany od starszego wrappera `api`:

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
    // NightAuthResult udostępnia Payload (JsonObject), RetryAfter i Error
    string tag = login.Payload["user"]?["username"]?.GetValue<string>() ?? "";
}
```

Dodatkowe asynchroniczne metody pomocnicze, dostępne zarówno w `NightAuthClient`, jak i w `api`: `ChangeUsernameAsync`, `GetUserVariablesAsync` (wyświetla wszystkie zmienne użytkownika), `VerifyFileHashAsync`, `GetStatsAsync`, `GetOnlineUsersAsync` oraz trzy przeciążenia `TriggerWebhookAsync` (payload JSON, JSON z własnymi `content`/`parameters` oraz przesył multipart z plikiem: `TriggerWebhookAsync(id, fileBytes, fileName, payload, content, parameters)`).

### 22. `SecurityProtections`

Statyczne metody pomocnicze anty-manipulacyjne. `RunSecurityChecks()` (wywoływane automatycznie przez `Init`) wykonuje tylko kontrolę proxy systemowego; pozostałe kontrole wywołaj jawnie:

```csharp
SecurityProtections.CheckSystemProxy();          // kończy działanie przy aktywnym proxy HTTP
SecurityProtections.CheckDebugger();             // kończy działanie przy podpiętym debuggerze (lokalnym lub zdalnym)
SecurityProtections.CheckBlacklistedProcesses(); // skanuje procesy pod kątem narzędzi analitycznych (HTTPDebugger, Cheat Engine, x64dbg, Wireshark, dnSpy, ...)

string hwid = SecurityProtections.GetHardwareId(); // stabilny identyfikator sprzętowy używany w blokadzie HWID
```

> [!NOTE]
> Ustaw zmienną środowiskową `NIGHTAUTH_IGNORE_SECURITY=1`, aby pominąć skanowanie procesów podczas developmentu/testów.

### 23. Podpisane Aktualizacje Automatyczne

Niepodpisana aktualizacja automatyczna (`ApplyUpdate`) jest przestarzała i wyłączona. Zamiast tego pobieraj i weryfikuj podpisane wydania:

```csharp
// Metoda all-in-one: pobiera manifest przez HTTPS, weryfikuje podpis Ed25519
// względem przypiętego klucza wydania, odrzuca przestarzałe sekwencje, pobiera
// artefakt i zwraca ścieżkę zainstalowanego pliku.
string path = await SignedUpdateInstaller.DownloadAsync(
    manifestUri: new Uri("https://releases.example.com/manifest.json"),
    pinnedReleaseKeyHex: "YOUR_PINNED_ED25519_PUBLIC_KEY_HEX",
    application: "my-app",
    destination: "updates/my-app.exe");

// Albo steruj procesem ręcznie:
var verified = SignedUpdate.Verify(envelopeJson, pinnedKeyHex, expectedApplication, minimumSequence);
Console.WriteLine($"Zweryfikowano {verified.Application} v{verified.Version} (sekw. {verified.Sequence})");
await SignedUpdate.DownloadAsync(verified, "updates/my-app.exe");
```

---

## 🖥️ Uruchamianie aplikacji referencyjnych

Uruchom klientów z katalogu `source`:

```bash
# Interaktywny klient konsolowy
dotnet run --project console/NightAuth.Console.csproj

# Nowoczesny klient desktopowy WPF (Windows)
dotnet run --project desktop/NightAuth.Desktop.csproj
```

---

## 📄 Licencja

Projekt jest licencjonowany na warunkach **Elastic License 2.0**.

* Nie wolno udostępniać oprogramowania osobom trzecim jako usługi hostowanej lub zarządzanej.
* Nie wolno modyfikować, wyłączać ani omijać funkcji weryfikacji licencji.
* Nie wolno usuwać ani modyfikować informacji o prawach autorskich.

Pełny tekst znajduje się w pliku [LICENSE](../LICENSE).
