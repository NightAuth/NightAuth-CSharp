# NightAuth C# .NET SDK — Lascia una stella su GitHub ⭐

SDK ufficiale **NightAuth** per C# / .NET & Suite client di riferimento (Interfaccia Desktop moderna in **WPF** e CLI interattiva nel terminale). Proteggi il tuo software da pirateria, reverse engineering e manomissioni con licenze cloud, blocco HWID, chat in tempo reale ed esecuzione sicura in memoria in pochi minuti.

Portale Ufficiale: [https://nightauth.org](https://nightauth.org)

---

**Languages / Idiomas / Langues / Sprachen:**  
[English](../README.md) • [Español](es.md) • [Français](fr.md) • [Deutsch](de.md) • [Italiano](it.md) • [Português](pt.md) • [Русский](ru.md) • [Türkçe](tr.md) • [Polski](pl.md) • [中文](zh.md)

---

> [!NOTE]
> ### 📺 Video Tutorial & Guide di Configurazione
> I video tutorial ufficiali e le guide dettagliate passo-passo per configurare applicazioni, piani, abbonamenti, variabili cloud, webhook e licenze nel dashboard di NightAuth **saranno pubblicati a breve** sul nostro portale e canale ufficiale.

---

## 📸 Schermate

### Applicazione Desktop (WPF Enterprise Dark Theme)

| Schermata di Accesso / Autenticazione | Dashboard & Gestione Abbonamento |
| :---: | :---: |
| ![NightAuth Desktop Login](../image/desktop.png) | ![NightAuth Desktop Dashboard](../image/desktop-dashboard.png) |

### Applicazione Console (CLI Interattiva)

| Accesso da Terminale | Dashboard Console Interattiva |
| :---: | :---: |
| ![NightAuth Console Login](../image/console.png) | ![NightAuth Console Dashboard](../image/console-dashboard.png) |

---

## 🛡️ Pratiche di Sicurezza Raccomandate

* **Compilazione Nativa AOT:** Il bytecode IL di C# può essere facilmente decompilato con strumenti come dnSpy o ILSpy. Per la distribuzione, compila con **Native AOT** (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`).
* **Virtualizzazione del Codice Enterprise:** Proteggi gli eseguibili utilizzando offuscatori e protector commerciali come **VMProtect**, **Themida**, **Eazfuscator.NET** o **Obfuscar**.
* **Integrità della Memoria & Anti-Debug:** NightAuth include un helper `SecurityProtections` con controlli contro proxy di sistema, debugger e processi in blacklist (`HTTPDebugger`, `CheatEngine`, `x64dbg`, `Wireshark`, `dnSpy`). Per impostazione predefinita viene eseguito solo il controllo del proxy; invoca `CheckDebugger()` e `CheckBlacklistedProcesses()` manualmente all'avvio per la protezione completa (vedi [SecurityProtections](#22-securityprotections)).
* **Esecuzione Sicura in Memoria:** Quando scarichi file protetti con `NightAuthApp.GetFileAsync()`, **non salvare i byte su disco**. Carica ed esegui i dati direttamente in memoria (es. `Assembly.Load()`) e azzera i buffer (`CryptographicOperations.ZeroMemory()`) subito dopo l'uso.

---

## 📦 Requisiti & Installazione

Richiede **.NET 10.0 SDK** (o successivo).

```bash
dotnet build NightAuth.sln
```

---

## ⚙️ Configurazione & Inizializzazione

Configura le credenziali della tua applicazione dalla dashboard di NightAuth:

```csharp
public static api NightAuthApp = new api(
    name: "",
    apikey: "",
    version: "",
    token: null
);
```

### Handshake Crittografico (`InitializeAsync`)

Chiama `InitializeAsync()` prima di eseguire qualsiasi altra funzione. Stabilisce lo scambio di chiavi Diffie-Hellman (X25519) e la sessione cifrata:

```csharp
var init = await NightAuthApp.InitializeAsync();

if (!init.Success)
{
    Console.WriteLine($"Inizializzazione non riuscita: {init.DisplayMessage}");
    Environment.Exit(1);
}
```

---

## 🚀 Guida all'Uso dell'SDK

### 1. Controllo Sessione & Blacklist

```csharp
// Valida lo stato della sessione attiva
var check = await NightAuthApp.CheckAsync();
Console.WriteLine($"Stato sessione: {check.DisplayMessage}");

// Verifica se la macchina o l'IP è in blacklist
var blacklist = await NightAuthApp.CheckBlacklistAsync();
if (blacklist.Success && blacklist.Payload["blacklisted"]?.GetValue<bool>() == true)
{
    Console.WriteLine("Accesso negato: Macchina o IP in blacklist.");
    Environment.Exit(0);
}
```

### 2. Login con Nome Utente & Password (e 2FA)

```csharp
var result = await NightAuthApp.LoginAsync(username, password, totpCode);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Bentornato, {user.Username}!");
}
else
{
    Console.WriteLine($"Accesso fallito: {result.DisplayMessage}");
}
```

### 3. Registrazione con Chiave di Licenza

```csharp
var result = await NightAuthApp.RegisterAsync(username, password, licenseKey, email);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Registrazione riuscita: {user.Username}");
}
else
{
    Console.WriteLine($"Registrazione fallita: {result.DisplayMessage}");
}
```

### 4. Autenticazione Solo Licenza (Key Only)

```csharp
var result = await NightAuthApp.LicenseAsync(licenseKey);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine("Licenza verificata con successo.");
}
else
{
    Console.WriteLine($"Accesso negato: {result.DisplayMessage}");
}
```

### 5. Riscatta Licenza / Aggiorna Abbonamento (Upgrade)

```csharp
var result = await NightAuthApp.UpgradeAsync(username, newLicenseKey);

if (result.Success)
{
    Console.WriteLine("Abbonamento aggiornato con successo.");
}
else
{
    Console.WriteLine($"Aggiornamento fallito: {result.DisplayMessage}");
}
```

### 6. Reset Hardware ID (HWID Lock)

```csharp
var result = await NightAuthApp.ResetHwidAsync(username, password, totpCode);

if (result.Success)
{
    Console.WriteLine("Blocco HWID reimpostato con successo.");
}
else
{
    Console.WriteLine($"Reset HWID fallito: {result.DisplayMessage}");
}
```

### 7. Recupero Password (Forgot Password & Reset)

```csharp
// Richiedi email di reimpostazione
var forgot = await NightAuthApp.ForgotPasswordAsync(email);
if (forgot.Success)
{
    Console.WriteLine("Email di reimpostazione password inviata.");
}

// Conferma token e aggiorna password
var reset = await NightAuthApp.ResetPasswordAsync(token, newPassword);
if (reset.Success)
{
    Console.WriteLine("Password aggiornata con successo.");
}
```

### 8. Dati del Profilo Utente (`result.ToUserData()`)

```csharp
UserData user = result.ToUserData();

Console.WriteLine($"Utente:       {user.Username}");
Console.WriteLine($"IP:           {user.Ip}");
Console.WriteLine($"HWID:         {user.Hwid}");
Console.WriteLine($"Registrazione:{user.CreatedAt}");
Console.WriteLine($"Ultimo login: {user.LastLoginAt}");

foreach (var sub in user.Subscriptions)
{
    Console.WriteLine($"Piano: {sub.Subscription} | Scadenza: {sub.Expiry} | Chiave: {sub.Key}");
}
```

### 9. Variabili Cloud (Globali)

```csharp
var res = await NightAuthApp.GetGlobalVarAsync("server_motd");
string motd = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Variabile Cloud: {motd}");
```

### 10. Variabili Utente (User Variables)

```csharp
// Salva variabile utente
await NightAuthApp.SetUserVarAsync("discord_tag", "user#0001");

// Recupera variabile utente
var res = await NightAuthApp.GetUserVarAsync("discord_tag");
string tag = res.Success ? res.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Discord salvato: {tag}");
```

### 11. Registri di Audit e Telemetria

```csharp
await NightAuthApp.LogAsync("Rilevata sospetta modifica della memoria.");
```

### 12. Banna Utente e Blocca Macchina

```csharp
await NightAuthApp.BanAsync("Manomissione rilevata dal sistema Anti-Cheat.");
```

### 13. Webhook Lato Server

```csharp
var body = new JsonObject { ["content"] = "Notifica" };
var resp = await NightAuthApp.TriggerWebhookAsync("7kR0UedlVI", body);
Console.WriteLine($"Risposta Webhook: HTTP {(int)resp.StatusCode}");
```

### 14. Download Sicuro in Memoria

```csharp
var res = await NightAuthApp.GetFileAsync("385624");

if (res.Success)
{
    byte[] fileBytes = Convert.FromBase64String(res.Payload["data"]?.ToString() ?? "");
    Console.WriteLine($"{fileBytes.Length} byte scaricati in modo sicuro.");
    // Esecuzione diretta in RAM (es. Assembly.Load)
}
```

### 15. Canali Chat in Tempo Reale

```csharp
// Invia messaggio
await NightAuthApp.SendChatMessageAsync("Ciao a tutti!", channel: "general");

// Ricevi messaggi
var res = await NightAuthApp.GetChatMessagesAsync("general");
if (res.Success && res.Payload["messages"] is JsonArray messages)
{
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg?["author"]}]: {msg?["message"]}");
    }
}
```

### 16. Gestori di Eventi in Tempo Reale (SSE)

```csharp
using var cts = new CancellationTokenSource();

await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
{
    Console.WriteLine($"[EVENTO] {evt}");
}
```

### 17. Autenticazione a Due Fattori (2FA)

```csharp
// Configura TOTP 2FA
var setup = await NightAuthApp.Setup2FAAsync();
string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";

// Conferma con codice a 6 cifre
var conf = await NightAuthApp.Confirm2FAAsync("123456");

// Disabilita 2FA
var disable = await NightAuthApp.Disable2FAAsync(password, "123456");
```

### 18. Ciclo di Vita della Sessione e Logout

```csharp
// Invia heartbeat ping
await NightAuthApp.HeartbeatAsync();

// Disconnetti e revoca sessione
await NightAuthApp.LogoutAsync();
```

### 19. API Sincrona

Ogni chiamata di autenticazione e gestione ha anche una controparte sincrona in `api` (es. `NightAuthApp`): `Init`, `Login`, `Register`, `License`, `Upgrade`, `Heartbeat`, `Check`, `Logout`, `ChangeUsername`, `ResetHwid`, `Ban`, `CheckBlacklist`, `VerifyFileHash`, `ForgotPassword`, `ResetPassword`, `Setup2FA`, `Confirm2FA`, `Disable2FA`, `GetGlobalVar`, `GetUserVar`, `SetUserVar`, `DownloadFile`, `GetChatMessages`, `SendChatMessage`, `Log`, `GetStats`, `GetOnlineUsers` e `TriggerWebhook`. Bloccano il thread corrente e riportano i risultati tramite `Response`/`User`/`App` invece di restituire un `NightAuthResult`:

```csharp
NightAuthApp.Init();

if (NightAuthApp.Login(username, password, totpCode))
{
    Console.WriteLine($"Bentornato, {NightAuthApp.User.Username}!");
}
else
{
    Console.WriteLine($"Login fallito: {NightAuthApp.Response.Message}");
}
```

Stato utile esposto dall'API sincrona: `Response`, `User` (alias `user_data`), `App` (alias `app_data`), `LatestVersion`, `ApiUrl`, `PendingDownloadUrl` e `ToOptions()` per derivare un `NightAuthOptions` dall'istanza corrente.

### 20. Gestori di Eventi in Tempo Reale

`api` espone eventi di alto livello generati dal motore realtime, oltre al controllo esplicito del listener in background:

```csharp
NightAuthApp.OnSessionInvalidated += reason => { Console.WriteLine($"Sessione invalidata: {reason}"); Environment.Exit(0); };
NightAuthApp.OnAppBanned += reason => Console.WriteLine($"App bannata: {reason}");
NightAuthApp.OnAppStatusChanged += status => Console.WriteLine($"Stato dell'app cambiato: {status}");
NightAuthApp.OnVersionUpdated += version => Console.WriteLine($"Nuova versione disponibile: {version}");
NightAuthApp.OnChatMessageReceived += (msg, channel) => Console.WriteLine($"[{channel}] {msg.Author}: {msg.Message}");

NightAuthApp.StartRealtimeEventListener(); // listener SSE in background
// ... dopo
NightAuthApp.StopRealtimeEventListener();
```

Per il controllo totale puoi anche consumare lo stream SSE manualmente con `ReadEventsAsync` (vedi sezione 16).

### 21. `NightAuthClient` Autonomo

Il motore asincrono moderno è disponibile come client indipendente, disaccoppiato dal wrapper legacy `api`:

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
    // NightAuthResult espone Payload (JsonObject), RetryAfter ed Error
    string tag = login.Payload["user"]?["username"]?.GetValue<string>() ?? "";
}
```

Helper asincroni aggiuntivi disponibili sia su `NightAuthClient` che su `api`: `ChangeUsernameAsync`, `GetUserVariablesAsync` (elenca tutte le variabili utente), `VerifyFileHashAsync`, `GetStatsAsync`, `GetOnlineUsersAsync` e tre overload di `TriggerWebhookAsync` (payload JSON, JSON con `content`/`parameters` personalizzati e upload multipart con file: `TriggerWebhookAsync(id, fileBytes, fileName, payload, content, parameters)`).

### 22. `SecurityProtections`

Helper anti-manomissione statici. `RunSecurityChecks()` (invocato automaticamente da `Init`) esegue solo il controllo del proxy di sistema; chiama gli altri controlli esplicitamente:

```csharp
SecurityProtections.CheckSystemProxy();          // termina se è attivo un proxy HTTP di sistema
SecurityProtections.CheckDebugger();             // termina se è agganciato un debugger (locale o remoto)
SecurityProtections.CheckBlacklistedProcesses(); // cerca strumenti di analisi (HTTPDebugger, Cheat Engine, x64dbg, Wireshark, dnSpy, ...)

string hwid = SecurityProtections.GetHardwareId(); // identificatore hardware stabile usato per il blocco HWID
```

> [!NOTE]
> Imposta la variabile di ambiente `NIGHTAUTH_IGNORE_SECURITY=1` per saltare la scansione dei processi in blacklist durante sviluppo/test.

### 23. Aggiornamenti Auto Firmati

L'auto-update non firmato (`ApplyUpdate`) è obsoleto e disabilitato. Scarica e verifica release firmate invece:

```csharp
// Helper tutto-in-uno: scarica il manifest via HTTPS, verifica la firma Ed25519
// rispetto alla tua chiave di release fissata, rifiuta le sequenze obsolete, scarica
// l'artefatto e restituisce il percorso del file installato.
string path = await SignedUpdateInstaller.DownloadAsync(
    manifestUri: new Uri("https://releases.example.com/manifest.json"),
    pinnedReleaseKeyHex: "YOUR_PINNED_ED25519_PUBLIC_KEY_HEX",
    application: "my-app",
    destination: "updates/my-app.exe");

// Oppure gestisci la pipeline manualmente:
var verified = SignedUpdate.Verify(envelopeJson, pinnedKeyHex, expectedApplication, minimumSequence);
Console.WriteLine($"Verificato {verified.Application} v{verified.Version} (seq {verified.Sequence})");
await SignedUpdate.DownloadAsync(verified, "updates/my-app.exe");
```

---

## 🖥️ Esecuzione dei Client di Riferimento

Avvia i client dalla directory `source`:

```bash
# Client Console da Terminale
dotnet run --project console/NightAuth.Console.csproj

# Client Desktop WPF (Windows)
dotnet run --project desktop/NightAuth.Desktop.csproj
```

---

## 📄 Licenza

Questo progetto è rilasciato sotto licenza **Elastic License 2.0**.

* Non è consentito fornire il software a terzi come servizio gestito o in hosting.
* Non è consentito alterare, disattivare o aggirare le funzioni di licenza.
* Non è consentito rimuovere o modificare gli avvisi di copyright.

Consulta il file [LICENSE](../LICENSE) per il testo completo.
