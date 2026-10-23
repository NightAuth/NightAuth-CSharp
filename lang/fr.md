# NightAuth C# .NET SDK — Ajoutez une étoile sur GitHub ⭐

SDK officiel **NightAuth** pour C# / .NET & Suite logicielle client de référence (Interface graphique moderne en **WPF** et CLI interactive dans le terminal). Protégez vos logiciels contre le piratage, l'ingénierie inverse et la falsification grâce aux licences cloud, au verrouillage HWID, au chat en direct et au streaming en mémoire en quelques minutes.

Portail officiel : [https://nightauth.org](https://nightauth.org)

---

**Languages / Idiomas / Langues / Sprachen:**  
[English](../README.md) • [Español](es.md) • [Français](fr.md) • [Deutsch](de.md) • [Italiano](it.md) • [Português](pt.md) • [Русский](ru.md) • [Türkçe](tr.md) • [Polski](pl.md) • [中文](zh.md)

---

> [!NOTE]
> ### 📺 Tutoriels Vidéo & Guides de Déploiement
> Les tutoriels vidéo officiels et les guides pas-à-pas détaillant la configuration des applications, plans, abonnements, variables cloud, webhooks et licences sur le tableau de bord NightAuth **seront publiés prochainement** sur notre portail et chaîne officielle.

---

## 📸 Captures d'écran

### Application de Bureau (WPF Thème Sombre Entreprise)

| Écran d'Authentification / Connexion | Tableau de Bord & Abonnements |
| :---: | :---: |
| ![NightAuth Desktop Login](../image/desktop.png) | ![NightAuth Desktop Dashboard](../image/desktop-dashboard.png) |

### Application Console (Terminal Interactif)

| Connexion Console | Tableau de Bord Interactif |
| :---: | :---: |
| ![NightAuth Console Login](../image/console.png) | ![NightAuth Console Dashboard](../image/console-dashboard.png) |

---

## 🛡️ Pratiques de Sécurité Recommandées

* **Compilation Native AOT :** Le bytecode IL C# peut être décompilé avec des outils comme dnSpy ou ILSpy. Pour la production, publiez avec **Native AOT** (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`).
* **Virtualisation de Code d'Entreprise :** Protégez les binaires avec des outils de virtualisation et d'obfuscation comme **VMProtect**, **Themida**, **Eazfuscator.NET** ou **Obfuscar**.
* **Intégrité Mémoire & Anti-Débogage :** NightAuth inclut un helper `SecurityProtections` avec des vérifications contre les proxies système, les débogueurs et les processus en liste noire (`HTTPDebugger`, `CheatEngine`, `x64dbg`, `Wireshark`, `dnSpy`). Par défaut, seule la vérification du proxy est exécutée ; appelez `CheckDebugger()` et `CheckBlacklistedProcesses()` manuellement au démarrage pour une protection complète (voir [SecurityProtections](#22-securityprotections)).
* **Exécution Sécurisée en Mémoire :** Lors du téléchargement de fichiers protégés avec `NightAuthApp.GetFileAsync()`, **n'écrivez pas les octets sur le disque**. Chargez et exécutez directement en RAM (via `Assembly.Load()` ou chargement réflexif) et effacez les tampons (`CryptographicOperations.ZeroMemory()`) immédiatement après.

---

## 📦 Prérequis & Installation

Nécessite le **SDK .NET 10.0** (ou version ultérieure).

```bash
dotnet build NightAuth.sln
```

---

## ⚙️ Configuration & Initialisation

Définissez vos identifiants d'application depuis le tableau de bord NightAuth :

```csharp
public static api NightAuthApp = new api(
    name: "",
    apikey: "",
    version: "",
    token: null
);
```

### Initialiser le Handshake Cryptographique (`InitializeAsync`)

Vous devez appeler `InitializeAsync()` avant toute autre fonction. Il initialise l'échange de clés Diffie-Hellman (X25519) et la session chiffrée :

```csharp
var init = await NightAuthApp.InitializeAsync();

if (!init.Success)
{
    Console.WriteLine($"Échec de l'initialisation : {init.DisplayMessage}");
    Environment.Exit(1);
}
```

---

## 🚀 Guide d'Utilisation du SDK

### 1. Vérification de Session & Liste Noire

```csharp
// Vérifier la session active
var check = await NightAuthApp.CheckAsync();
Console.WriteLine($"État de la session : {check.DisplayMessage}");

// Vérifier si la machine ou l'IP est bannie
var blacklist = await NightAuthApp.CheckBlacklistAsync();
if (blacklist.Success && blacklist.Payload["blacklisted"]?.GetValue<bool>() == true)
{
    Console.WriteLine("Accès refusé : Machine ou adresse IP bannie.");
    Environment.Exit(0);
}
```

### 2. Connexion avec Identifiant & Mot de Passe (et 2FA)

```csharp
var result = await NightAuthApp.LoginAsync(username, password, totpCode);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Bienvenue, {user.Username} !");
}
else
{
    Console.WriteLine($"Échec de connexion : {result.DisplayMessage}");
}
```

### 3. Inscription avec Clé de Licence

```csharp
var result = await NightAuthApp.RegisterAsync(username, password, licenseKey, email);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Compte enregistré : {user.Username}");
}
else
{
    Console.WriteLine($"Échec d'inscription : {result.DisplayMessage}");
}
```

### 4. Authentification par Clé Seule (Key Only)

```csharp
var result = await NightAuthApp.LicenseAsync(licenseKey);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine("Licence validée. Accès autorisé.");
}
else
{
    Console.WriteLine($"Accès refusé : {result.DisplayMessage}");
}
```

### 5. Mise à Niveau d'Abonnement (Upgrade)

```csharp
var result = await NightAuthApp.UpgradeAsync(username, newLicenseKey);

if (result.Success)
{
    Console.WriteLine("Abonnement mis à niveau avec succès.");
}
else
{
    Console.WriteLine($"Échec de mise à niveau : {result.DisplayMessage}");
}
```

### 6. Réinitialisation de l'Identifiant Matériel (HWID Lock)

```csharp
var result = await NightAuthApp.ResetHwidAsync(username, password, totpCode);

if (result.Success)
{
    Console.WriteLine("Verrouillage HWID réinitialisé. Connexion possible sur cette machine.");
}
else
{
    Console.WriteLine($"Échec réinitialisation HWID : {result.DisplayMessage}");
}
```

### 7. Mot de Passe Oublié (Forgot Password & Reset)

```csharp
// Demander un lien de récupération par e-mail
var forgot = await NightAuthApp.ForgotPasswordAsync(email);
if (forgot.Success)
{
    Console.WriteLine("Instructions de réinitialisation envoyées par e-mail.");
}

// Réinitialiser le mot de passe avec le jeton
var reset = await NightAuthApp.ResetPasswordAsync(token, newPassword);
if (reset.Success)
{
    Console.WriteLine("Mot de passe mis à jour avec succès.");
}
```

### 8. Données du Profil Utilisateur (`result.ToUserData()`)

```csharp
UserData user = result.ToUserData();

Console.WriteLine($"Utilisateur : {user.Username}");
Console.WriteLine($"IP :          {user.Ip}");
Console.WriteLine($"HWID :        {user.Hwid}");
Console.WriteLine($"Création :    {user.CreatedAt}");
Console.WriteLine($"Dernier log : {user.LastLoginAt}");

foreach (var sub in user.Subscriptions)
{
    Console.WriteLine($"Plan : {sub.Subscription} | Expire : {sub.Expiry} | Clé : {sub.Key}");
}
```

### 9. Variables Cloud (Globales)

```csharp
var res = await NightAuthApp.GetGlobalVarAsync("server_motd");
string motd = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Variable Cloud : {motd}");
```

### 10. Variables Utilisateur (User Variables)

```csharp
// Définir une variable utilisateur
await NightAuthApp.SetUserVarAsync("discord_tag", "user#0001");

// Récupérer une variable utilisateur
var res = await NightAuthApp.GetUserVarAsync("discord_tag");
string tag = res.Success ? res.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Discord enregistré : {tag}");
```

### 11. Journaux d'Audit & Sécurité

```csharp
await NightAuthApp.LogAsync("Tentative suspecte de modification mémoire détectée.");
```

### 12. Bannissement d'Utilisateur & Machine

```csharp
await NightAuthApp.BanAsync("Altération de sécurité détectée par l'Anti-Cheat.");
```

### 13. Webhooks Côté Serveur Sécurisés

```csharp
var body = new JsonObject { ["content"] = "Notification client" };
var resp = await NightAuthApp.TriggerWebhookAsync("7kR0UedlVI", body);
Console.WriteLine($"Réponse Webhook : HTTP {(int)resp.StatusCode}");
```

### 14. Téléchargement Sécurisé en Mémoire

```csharp
var res = await NightAuthApp.GetFileAsync("385624");

if (res.Success)
{
    byte[] fileBytes = Convert.FromBase64String(res.Payload["data"]?.ToString() ?? "");
    Console.WriteLine($"{fileBytes.Length} octets téléchargés en mémoire.");
    // Exécuter directement en mémoire (ex: Assembly.Load)
}
```

### 15. Canaux de Chat en Direct

```csharp
// Envoyer un message
await NightAuthApp.SendChatMessageAsync("Bonjour la communauté !", channel: "general");

// Récupérer les messages
var res = await NightAuthApp.GetChatMessagesAsync("general");
if (res.Success && res.Payload["messages"] is JsonArray messages)
{
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg?["author"]}]: {msg?["message"]}");
    }
}
```

### 16. Gestionnaires d'Événements Temps Réel (SSE)

```csharp
using var cts = new CancellationTokenSource();

await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
{
    Console.WriteLine($"[ÉVÉNEMENT] {evt}");
}
```

### 17. Authentification à Deux Facteurs (2FA)

```csharp
// Configurer TOTP 2FA
var setup = await NightAuthApp.Setup2FAAsync();
string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";

// Confirmer le code à 6 chiffres
var conf = await NightAuthApp.Confirm2FAAsync("123456");

// Désactiver 2FA
var disable = await NightAuthApp.Disable2FAAsync(password, "123456");
```

### 18. Cycle de Vie de Session & Déconnexion

```csharp
// Heartbeat pour maintenir la session
await NightAuthApp.HeartbeatAsync();

// Fermer et révoquer la session
await NightAuthApp.LogoutAsync();
```

### 19. API Synchrone

Chaque appel d'authentification et de gestion possède également un équivalent synchrone dans `api` (par ex. `NightAuthApp`) : `Init`, `Login`, `Register`, `License`, `Upgrade`, `Heartbeat`, `Check`, `Logout`, `ChangeUsername`, `ResetHwid`, `Ban`, `CheckBlacklist`, `VerifyFileHash`, `ForgotPassword`, `ResetPassword`, `Setup2FA`, `Confirm2FA`, `Disable2FA`, `GetGlobalVar`, `GetUserVar`, `SetUserVar`, `DownloadFile`, `GetChatMessages`, `SendChatMessage`, `Log`, `GetStats`, `GetOnlineUsers` et `TriggerWebhook`. Elles bloquent le thread courant et rapportent les résultats via `Response`/`User`/`App` au lieu de retourner un `NightAuthResult` :

```csharp
NightAuthApp.Init();

if (NightAuthApp.Login(username, password, totpCode))
{
    Console.WriteLine($"Bon retour, {NightAuthApp.User.Username} !");
}
else
{
    Console.WriteLine($"Échec de connexion : {NightAuthApp.Response.Message}");
}
```

État utile exposé par l'API synchrone : `Response`, `User` (alias `user_data`), `App` (alias `app_data`), `LatestVersion`, `ApiUrl`, `PendingDownloadUrl` et `ToOptions()` pour dériver un `NightAuthOptions` de l'instance courante.

### 20. Gestionnaires d'Événements Temps Réel

`api` expose des événements de haut niveau déclenchés par le moteur temps réel, ainsi qu'un contrôle explicite de l'écouteur en arrière-plan :

```csharp
NightAuthApp.OnSessionInvalidated += reason => { Console.WriteLine($"Session invalidée : {reason}"); Environment.Exit(0); };
NightAuthApp.OnAppBanned += reason => Console.WriteLine($"Application bannie : {reason}");
NightAuthApp.OnAppStatusChanged += status => Console.WriteLine($"Statut de l'application modifié : {status}");
NightAuthApp.OnVersionUpdated += version => Console.WriteLine($"Nouvelle version disponible : {version}");
NightAuthApp.OnChatMessageReceived += (msg, channel) => Console.WriteLine($"[{channel}] {msg.Author}: {msg.Message}");

NightAuthApp.StartRealtimeEventListener(); // écouteur SSE en arrière-plan
// ... plus tard
NightAuthApp.StopRealtimeEventListener();
```

Pour un contrôle total, vous pouvez aussi consommer le flux SSE manuellement avec `ReadEventsAsync` (voir section 16).

### 21. `NightAuthClient` Autonome

Le moteur asynchrone moderne est disponible comme client indépendant, découplé du wrapper hérité `api` :

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
    // NightAuthResult expose Payload (JsonObject), RetryAfter et Error
    string tag = login.Payload["user"]?["username"]?.GetValue<string>() ?? "";
}
```

Assistants asynchrones supplémentaires, disponibles à la fois sur `NightAuthClient` et sur `api` : `ChangeUsernameAsync`, `GetUserVariablesAsync` (liste toutes les variables utilisateur), `VerifyFileHashAsync`, `GetStatsAsync`, `GetOnlineUsersAsync` et trois surcharges de `TriggerWebhookAsync` (payload JSON, JSON avec `content`/`parameters` personnalisés et envoi multipart avec fichier : `TriggerWebhookAsync(id, fileBytes, fileName, payload, content, parameters)`).

### 22. `SecurityProtections`

Assistants anti-manipulation statiques. `RunSecurityChecks()` (invoqué automatiquement par `Init`) n'effectue que la vérification du proxy système ; appelez les autres vérifications explicitement :

```csharp
SecurityProtections.CheckSystemProxy();          // termine si un proxy HTTP système est actif
SecurityProtections.CheckDebugger();             // termine si un débogueur (local ou distant) est attaché
SecurityProtections.CheckBlacklistedProcesses(); // recherche des outils d'analyse (HTTPDebugger, Cheat Engine, x64dbg, Wireshark, dnSpy, ...)

string hwid = SecurityProtections.GetHardwareId(); // identifiant matériel stable utilisé pour le verrou HWID
```

> [!NOTE]
> Définissez la variable d'environnement `NIGHTAUTH_IGNORE_SECURITY=1` pour ignorer l'analyse des processus en liste noire pendant le développement/les tests.

### 23. Mises à Jour Signées

La mise à jour automatique non signée (`ApplyUpdate`) est obsolète et désactivée. Téléchargez et vérifiez des releases signées à la place :

```csharp
// Assistant tout-en-un : télécharge le manifeste via HTTPS, vérifie la signature Ed25519
// contre votre clé de release épinglée, rejette les séquences obsolètes, télécharge
// l'artefact et retourne le chemin du fichier installé.
string path = await SignedUpdateInstaller.DownloadAsync(
    manifestUri: new Uri("https://releases.example.com/manifest.json"),
    pinnedReleaseKeyHex: "YOUR_PINNED_ED25519_PUBLIC_KEY_HEX",
    application: "my-app",
    destination: "updates/my-app.exe");

// Ou pilotez le pipeline manuellement :
var verified = SignedUpdate.Verify(envelopeJson, pinnedKeyHex, expectedApplication, minimumSequence);
Console.WriteLine($"Vérifié {verified.Application} v{verified.Version} (séq. {verified.Sequence})");
await SignedUpdate.DownloadAsync(verified, "updates/my-app.exe");
```

---

## 🖥️ Exécuter les Clients de Référence

Lancez les clients depuis le dossier `source` :

```bash
# Client Console Interactif
dotnet run --project console/NightAuth.Console.csproj

# Client de Bureau WPF (Windows)
dotnet run --project desktop/NightAuth.Desktop.csproj
```

---

## 📄 Licence

Ce projet est sous licence **Elastic License 2.0**.

* Vous ne pouvez pas fournir le logiciel à des tiers en tant que service hébergé ou géré.
* Vous ne pouvez pas altérer, désactiver ou contourner le système de licences.
* Vous ne pouvez pas modifier ou supprimer les mentions de droit d'auteur.

Consultez le fichier [LICENSE](../LICENSE) pour le texte légal complet.
