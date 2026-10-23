# NightAuth C# .NET SDK — Поставьте звезду на GitHub ⭐

Официальный SDK **NightAuth** для C# / .NET и эталонный клиентский пакет (современный графический интерфейс на **WPF** и интерактивный терминал CLI). Защитите ваше ПО от пиратства, реверс-инжиниринга и взлома с помощью облачного лицензирования, привязки к HWID, онлайн-чата и безопасной загрузки в оперативную память за считанные минуты.

Официальный портал: [https://nightauth.org](https://nightauth.org)

---

**Languages / Idiomas / Langues / Sprachen:**  
[English](../README.md) • [Español](es.md) • [Français](fr.md) • [Deutsch](de.md) • [Italiano](it.md) • [Português](pt.md) • [Русский](ru.md) • [Türkçe](tr.md) • [Polski](pl.md) • [中文](zh.md)

---

> [!NOTE]
> ### 📺 Видеоинструкции и руководства
> Официальные видеоуроки и пошаговые руководства по настройке приложений, тарифов, подписок, переменных, вебхуков и ключей в панели NightAuth **будут опубликованы в ближайшее время**.

---

## 📸 Скриншоты

### Графическое приложение (WPF Enterprise Dark Theme)

| Окно входа и авторизации | Панель управления и подписки |
| :---: | :---: |
| ![NightAuth Desktop Login](../image/desktop.png) | ![NightAuth Desktop Dashboard](../image/desktop-dashboard.png) |

### Консольное приложение (Интерактивный CLI)

| Вход через терминал | Панель управления в консоли |
| :---: | :---: |
| ![NightAuth Console Login](../image/console.png) | ![NightAuth Console Dashboard](../image/console-dashboard.png) |

---

## 🛡️ Рекомендации по безопасности

* **Нативная компиляция AOT:** Байт-код C# IL легко декомпилируется с помощью dnSpy или ILSpy. Для релизов используйте **Native AOT** (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`).
* **Виртуализация кода:** Защищайте сборки коммерческими протекторами: **VMProtect**, **Themida**, **Eazfuscator.NET** или **Obfuscar**.
* **Антиотладка и целостность памяти:** NightAuth содержит хелпер `SecurityProtections` с проверками системного прокси, отладчиков и процессов из чёрного списка (`HTTPDebugger`, `CheatEngine`, `x64dbg`, `Wireshark`, `dnSpy`). По умолчанию выполняется только проверка прокси; для полной защиты вызывайте `CheckDebugger()` и `CheckBlacklistedProcesses()` вручную при запуске (см. [SecurityProtections](#22-securityprotections)).
* **Безопасное выполнение в памяти:** При скачивании файлов через `NightAuthApp.GetFileAsync()` **не сохраняйте байты на диск**. Загружайте и выполняйте их прямо в RAM (например, через `Assembly.Load()`) и очищайте буферы (`CryptographicOperations.ZeroMemory()`) сразу после использования.

---

## 📦 Требования и установка

Требуется **.NET 10.0 SDK** (или новее).

```bash
dotnet build NightAuth.sln
```

---

## ⚙️ Конфигурация и инициализация

Укажите учетные данные вашего приложения из панели управления NightAuth:

```csharp
public static api NightAuthApp = new api(
    name: "",
    apikey: "",
    version: "",
    token: null
);
```

### Криптографическое рукопожатие (`InitializeAsync`)

Вызовите `InitializeAsync()` перед вызовом других методов. Устанавливает сеанс с обменом ключами Diffie-Hellman (X25519) и сквозным шифрованием:

```csharp
var init = await NightAuthApp.InitializeAsync();

if (!init.Success)
{
    Console.WriteLine($"Ошибка инициализации: {init.DisplayMessage}");
    Environment.Exit(1);
}
```

---

## 🚀 Руководство по использованию SDK

### 1. Проверка статуса сессии и черный список

```csharp
// Проверить активную сессию
var check = await NightAuthApp.CheckAsync();
Console.WriteLine($"Статус сессии: {check.DisplayMessage}");

// Проверить наличие устройства или IP в черном списке
var blacklist = await NightAuthApp.CheckBlacklistAsync();
if (blacklist.Success && blacklist.Payload["blacklisted"]?.GetValue<bool>() == true)
{
    Console.WriteLine("Доступ запрещен: устройство или IP заблокированы.");
    Environment.Exit(0);
}
```

### 2. Вход по логину и паролю (и 2FA)

```csharp
var result = await NightAuthApp.LoginAsync(username, password, totpCode);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Добро пожаловать, {user.Username}!");
}
else
{
    Console.WriteLine($"Ошибка входа: {result.DisplayMessage}");
}
```

### 3. Регистрация с лицензионным ключом

```csharp
var result = await NightAuthApp.RegisterAsync(username, password, licenseKey, email);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Успешная регистрация: {user.Username}");
}
else
{
    Console.WriteLine($"Ошибка регистрации: {result.DisplayMessage}");
}
```

### 4. Вход только по ключу лицензии (Key Only)

```csharp
var result = await NightAuthApp.LicenseAsync(licenseKey);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine("Лицензия подтверждена. Доступ разрешен.");
}
else
{
    Console.WriteLine($"Доступ запрещен: {result.DisplayMessage}");
}
```

### 5. Обновление подписки / Активация ключа (Upgrade)

```csharp
var result = await NightAuthApp.UpgradeAsync(username, newLicenseKey);

if (result.Success)
{
    Console.WriteLine("Подписка успешно продлена.");
}
else
{
    Console.WriteLine($"Ошибка продления: {result.DisplayMessage}");
}
```

### 6. Сброс привязки железа (HWID Lock)

```csharp
var result = await NightAuthApp.ResetHwidAsync(username, password, totpCode);

if (result.Success)
{
    Console.WriteLine("HWID успешно сброшен. Теперь можно войти на этом ПК.");
}
else
{
    Console.WriteLine($"Ошибка сброса HWID: {result.DisplayMessage}");
}
```

### 7. Восстановление пароля (Forgot Password & Reset)

```csharp
// Запрос письма для восстановления
var forgot = await NightAuthApp.ForgotPasswordAsync(email);
if (forgot.Success)
{
    Console.WriteLine("Инструкции по сбросу отправлены на почту.");
}

// Завершение сброса пароля с токеном
var reset = await NightAuthApp.ResetPasswordAsync(token, newPassword);
if (reset.Success)
{
    Console.WriteLine("Пароль успешно обновлен.");
}
```

### 8. Профиль пользователя (`result.ToUserData()`)

```csharp
UserData user = result.ToUserData();

Console.WriteLine($"Логин:      {user.Username}");
Console.WriteLine($"IP:         {user.Ip}");
Console.WriteLine($"HWID:       {user.Hwid}");
Console.WriteLine($"Создан:     {user.CreatedAt}");
Console.WriteLine($"Посл. вход: {user.LastLoginAt}");

foreach (var sub in user.Subscriptions)
{
    Console.WriteLine($"Тариф: {sub.Subscription} | До: {sub.Expiry} | Ключ: {sub.Key}");
}
```

### 9. Облачные переменные (Глобальные)

```csharp
var res = await NightAuthApp.GetGlobalVarAsync("server_motd");
string motd = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Облачная переменная: {motd}");
```

### 10. Пользовательские переменные

```csharp
// Установить переменную
await NightAuthApp.SetUserVarAsync("discord_tag", "user#0001");

// Получить переменную
var res = await NightAuthApp.GetUserVarAsync("discord_tag");
string tag = res.Success ? res.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Сохраненный Discord: {tag}");
```

### 11. Журнал аудита и логи

```csharp
await NightAuthApp.LogAsync("Обнаружена подозрительная модификация памяти.");
```

### 12. Бан пользователя и блокировка устройства

```csharp
await NightAuthApp.BanAsync("Попытка взлома обнаружена Anti-Cheat системой.");
```

### 13. Серверные вебхуки

```csharp
var body = new JsonObject { ["content"] = "Уведомление" };
var resp = await NightAuthApp.TriggerWebhookAsync("7kR0UedlVI", body);
Console.WriteLine($"Ответ вебхука: HTTP {(int)resp.StatusCode}");
```

### 14. Безопасная загрузка файлов в RAM

```csharp
var res = await NightAuthApp.GetFileAsync("385624");

if (res.Success)
{
    byte[] fileBytes = Convert.FromBase64String(res.Payload["data"]?.ToString() ?? "");
    Console.WriteLine($"Загружено {fileBytes.Length} байт в память.");
    // Выполнение напрямую в RAM (например, Assembly.Load)
}
```

### 15. Онлайн-чат

```csharp
// Отправить сообщение
await NightAuthApp.SendChatMessageAsync("Привет всем!", channel: "general");

// Получить сообщения
var res = await NightAuthApp.GetChatMessagesAsync("general");
if (res.Success && res.Payload["messages"] is JsonArray messages)
{
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg?["author"]}]: {msg?["message"]}");
    }
}
```

### 16. Обработчики событий реального времени (SSE)

```csharp
using var cts = new CancellationTokenSource();

await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
{
    Console.WriteLine($"[СОБЫТИЕ] {evt}");
}
```

### 17. Двухфакторная аутентификация (2FA)

```csharp
// Настройка TOTP 2FA
var setup = await NightAuthApp.Setup2FAAsync();
string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";

// Подтверждение 6-значным кодом
var conf = await NightAuthApp.Confirm2FAAsync("123456");

// Отключение 2FA
var disable = await NightAuthApp.Disable2FAAsync(password, "123456");
```

### 18. Жизненный цикл сессии и выход

```csharp
// Heartbeat пинг для поддержания сессии
await NightAuthApp.HeartbeatAsync();

// Завершить сессию и отозвать токен
await NightAuthApp.LogoutAsync();
```

### 19. Синхронный API

Каждый вызов аутентификации и управления также имеет синхронный аналог в `api` (напр. `NightAuthApp`): `Init`, `Login`, `Register`, `License`, `Upgrade`, `Heartbeat`, `Check`, `Logout`, `ChangeUsername`, `ResetHwid`, `Ban`, `CheckBlacklist`, `VerifyFileHash`, `ForgotPassword`, `ResetPassword`, `Setup2FA`, `Confirm2FA`, `Disable2FA`, `GetGlobalVar`, `GetUserVar`, `SetUserVar`, `DownloadFile`, `GetChatMessages`, `SendChatMessage`, `Log`, `GetStats`, `GetOnlineUsers` и `TriggerWebhook`. Они блокируют текущий поток и возвращают результат через `Response`/`User`/`App`, а не через `NightAuthResult`:

```csharp
NightAuthApp.Init();

if (NightAuthApp.Login(username, password, totpCode))
{
    Console.WriteLine($"С возвращением, {NightAuthApp.User.Username}!");
}
else
{
    Console.WriteLine($"Ошибка входа: {NightAuthApp.Response.Message}");
}
```

Полезное состояние синхронного API: `Response`, `User` (алиас `user_data`), `App` (алиас `app_data`), `LatestVersion`, `ApiUrl`, `PendingDownloadUrl` и `ToOptions()` для получения `NightAuthOptions` из текущего экземпляра.

### 20. Обработчики Событий в Реальном Времени

`api` предоставляет высокоуровневые события механизма реального времени, а также явный контроль над фоновым слушателем:

```csharp
NightAuthApp.OnSessionInvalidated += reason => { Console.WriteLine($"Сессия отменена: {reason}"); Environment.Exit(0); };
NightAuthApp.OnAppBanned += reason => Console.WriteLine($"Приложение заблокировано: {reason}");
NightAuthApp.OnAppStatusChanged += status => Console.WriteLine($"Статус приложения изменён: {status}");
NightAuthApp.OnVersionUpdated += version => Console.WriteLine($"Доступна новая версия: {version}");
NightAuthApp.OnChatMessageReceived += (msg, channel) => Console.WriteLine($"[{channel}] {msg.Author}: {msg.Message}");

NightAuthApp.StartRealtimeEventListener(); // фоновый SSE-слушатель
// ... позже
NightAuthApp.StopRealtimeEventListener();
```

Для полного контроля поток SSE можно также обрабатывать вручную через `ReadEventsAsync` (см. раздел 16).

### 21. Автономный `NightAuthClient`

Современный асинхронный движок доступен как независимый клиент, отделённый от устаревшей обёртки `api`:

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
    // NightAuthResult предоставляет Payload (JsonObject), RetryAfter и Error
    string tag = login.Payload["user"]?["username"]?.GetValue<string>() ?? "";
}
```

Дополнительные асинхронные помощники, доступные и в `NightAuthClient`, и в `api`: `ChangeUsernameAsync`, `GetUserVariablesAsync` (перечисляет все переменные пользователя), `VerifyFileHashAsync`, `GetStatsAsync`, `GetOnlineUsersAsync` и три перегрузки `TriggerWebhookAsync` (JSON-payload, JSON с собственными `content`/`parameters` и multipart-загрузка с файлом: `TriggerWebhookAsync(id, fileBytes, fileName, payload, content, parameters)`).

### 22. `SecurityProtections`

Статические помощники против вмешательства. `RunSecurityChecks()` (вызывается автоматически из `Init`) выполняет только проверку системного прокси; остальные проверки вызывайте явно:

```csharp
SecurityProtections.CheckSystemProxy();          // завершает работу при активном системном HTTP-прокси
SecurityProtections.CheckDebugger();             // завершает работу при подключённом отладчике (локальном или удалённом)
SecurityProtections.CheckBlacklistedProcesses(); // ищет инструменты анализа (HTTPDebugger, Cheat Engine, x64dbg, Wireshark, dnSpy, ...)

string hwid = SecurityProtections.GetHardwareId(); // стабильный аппаратный идентификатор для HWID-блокировки
```

> [!NOTE]
> Установите переменную окружения `NIGHTAUTH_IGNORE_SECURITY=1`, чтобы пропустить сканирование процессов при разработке/тестировании.

### 23. Подписанные Автообновления

Неподписанное автообновление (`ApplyUpdate`) устарело и отключено. Вместо этого скачивайте и проверяйте подписанные релизы:

```csharp
// Универсальный помощник: скачивает манифест по HTTPS, проверяет подпись Ed25519
// по закреплённому ключу релиза, отклоняет устаревшие последовательности, скачивает
// артефакт и возвращает путь установленного файла.
string path = await SignedUpdateInstaller.DownloadAsync(
    manifestUri: new Uri("https://releases.example.com/manifest.json"),
    pinnedReleaseKeyHex: "YOUR_PINNED_ED25519_PUBLIC_KEY_HEX",
    application: "my-app",
    destination: "updates/my-app.exe");

// Или управляйте процессом вручную:
var verified = SignedUpdate.Verify(envelopeJson, pinnedKeyHex, expectedApplication, minimumSequence);
Console.WriteLine($"Проверено {verified.Application} v{verified.Version} (seq {verified.Sequence})");
await SignedUpdate.DownloadAsync(verified, "updates/my-app.exe");
```

---

## 🖥️ Запуск примеров

Запуск из директории `source`:

```bash
# Консольный интерактивный терминал
dotnet run --project console/NightAuth.Console.csproj

# Графический интерфейс WPF (Windows)
dotnet run --project desktop/NightAuth.Desktop.csproj
```

---

## 📄 Лицензия

Проект распространяется под лицензией **Elastic License 2.0**.

* Запрещено предоставлять ПО третьим лицам в качестве хостинг-сервиса.
* Запрещено изменять или обходить систему лицензирования.
* Запрещено удалять уведомления об авторских правах.

Полный текст лицензии доступен в файле [LICENSE](../LICENSE).
