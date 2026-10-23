# NightAuth C# .NET SDK — Danos una estrella en GitHub ⭐

SDK oficial de **NightAuth** para C# / .NET y suite de referencia completa (Interfaz de Escritorio moderna en **WPF** y Consola Interactiva en Terminal). Protege tu software contra piratería, ingeniería inversa y manipulación con licenciamiento cloud, bloqueo de HWID, chat en vivo y streaming en memoria en minutos.

Portal Oficial: [https://nightauth.org](https://nightauth.org)

---

**Languages / Idiomas / Langues / Sprachen:**  
[English](../README.md) • [Español](es.md) • [Français](fr.md) • [Deutsch](de.md) • [Italiano](it.md) • [Português](pt.md) • [Русский](ru.md) • [Türkçe](tr.md) • [Polski](pl.md) • [中文](zh.md)

---

> [!NOTE]
> ### 📺 Tutoriales en Video y Configuración del Sistema
> Los video-tutoriales oficiales y las guías paso a paso de cómo configurar aplicaciones, planes, suscripciones, variables en la nube, webhooks y licencias en el panel de control de NightAuth **serán publicados próximamente** en nuestro portal y canal oficial.

---

## 📸 Capturas de Pantalla

### Aplicación de Escritorio (WPF Enterprise Dark Theme)

| Pantalla de Inicio / Autenticación | Panel de Control & Gestión de Suscripciones |
| :---: | :---: |
| ![NightAuth Desktop Login](../image/desktop.png) | ![NightAuth Desktop Dashboard](../image/desktop-dashboard.png) |

### Aplicación de Consola (CLI Interactiva)

| Inicio de Sesión en Terminal | Panel Interactivo de Consola |
| :---: | :---: |
| ![NightAuth Console Login](../image/console.png) | ![NightAuth Console Dashboard](../image/console-dashboard.png) |

---

## 🛡️ Prácticas de Seguridad Recomendadas

* **Compilación Nativa AOT:** El bytecode IL de C# puede descompilarse con herramientas como dnSpy o ILSpy. Para versiones de producción, compila usando **Native AOT** (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`).
* **Virtualización de Código Empresarial:** Protege los binarios compilados utilizando virtualizadores y ofuscadores como **VMProtect**, **Themida**, **Eazfuscator.NET** o **Obfuscar**.
* **Integridad de Memoria y Anti-Debug:** NightAuth incluye un helper `SecurityProtections` con comprobaciones contra proxies del sistema, debuggers y procesos en lista negra (`HTTPDebugger`, `CheatEngine`, `x64dbg`, `Wireshark`, `dnSpy`). Por defecto solo se ejecuta la comprobación de proxy; invoca `CheckDebugger()` y `CheckBlacklistedProcesses()` manualmente al inicio para protección completa (ver [SecurityProtections](#22-securityprotections)).
* **Ejecución Segura en Memoria:** Al descargar archivos protegidos con `NightAuthApp.GetFileAsync()`, **no guardes los bytes en el disco duro**. Carga y ejecuta los ejecutables directamente en RAM (mediante `Assembly.Load()` o carga reflectiva) y limpia los búferes (`CryptographicOperations.ZeroMemory()`) inmediatamente después de su uso.

---

## 📦 Requisitos e Instalación

Requiere **.NET 10.0 SDK** (o superior).

```bash
dotnet build NightAuth.sln
```

---

## ⚙️ Configuración e Inicialización

Configura las credenciales de tu aplicación obtenidas en el panel de NightAuth:

```csharp
public static api NightAuthApp = new api(
    name: "",
    apikey: "",
    version: "",
    token: null
);
```

### Inicializar Handshake Criptográfico (`InitializeAsync`)

Debes invocar `InitializeAsync()` antes de cualquier otra llamada. Establece el intercambio de claves efímeras Diffie-Hellman (X25519) y la sesión cifrada:

```csharp
var init = await NightAuthApp.InitializeAsync();

if (!init.Success)
{
    Console.WriteLine($"Error de inicialización: {init.DisplayMessage}");
    Environment.Exit(1);
}
```

---

## 🚀 Guía de Uso del SDK

### 1. Comprobar Validación de Sesión y Blacklist

```csharp
// Validar estado de la sesión activa
var check = await NightAuthApp.CheckAsync();
Console.WriteLine($"Estado de sesión: {check.DisplayMessage}");

// Comprobar si la máquina o IP está en la lista negra
var blacklist = await NightAuthApp.CheckBlacklistAsync();
if (blacklist.Success && blacklist.Payload["blacklisted"]?.GetValue<bool>() == true)
{
    Console.WriteLine("Acceso denegado: Máquina o IP en lista negra.");
    Environment.Exit(0);
}
```

### 2. Inicio de Sesión (Login con Usuario, Contraseña y 2FA)

```csharp
var result = await NightAuthApp.LoginAsync(username, password, totpCode);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Bienvenido de vuelta, {user.Username}!");
}
else
{
    Console.WriteLine($"Error de inicio de sesión: {result.DisplayMessage}");
}
```

### 3. Registro con Clave de Licencia

```csharp
var result = await NightAuthApp.RegisterAsync(username, password, licenseKey, email);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Registro exitoso: {user.Username}");
}
else
{
    Console.WriteLine($"Error de registro: {result.DisplayMessage}");
}
```

### 4. Acceso con Solo Clave de Licencia (Key Only)

```csharp
var result = await NightAuthApp.LicenseAsync(licenseKey);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine("Licencia verificada. Acceso concedido.");
}
else
{
    Console.WriteLine($"Acceso denegado: {result.DisplayMessage}");
}
```

### 5. Actualizar Suscripción / Canjear Clave (Upgrade)

```csharp
var result = await NightAuthApp.UpgradeAsync(username, newLicenseKey);

if (result.Success)
{
    Console.WriteLine("Suscripción actualizada exitosamente.");
}
else
{
    Console.WriteLine($"Error al actualizar: {result.DisplayMessage}");
}
```

### 6. Restablecer HWID (Hardware ID Lock)

```csharp
var result = await NightAuthApp.ResetHwidAsync(username, password, totpCode);

if (result.Success)
{
    Console.WriteLine("HWID restablecido correctamente. Ya puedes iniciar sesión en este equipo.");
}
else
{
    Console.WriteLine($"Fallo al restablecer HWID: {result.DisplayMessage}");
}
```

### 7. Recuperación de Contraseña (Forgot Password & Reset)

```csharp
// Solicitar correo de recuperación
var forgot = await NightAuthApp.ForgotPasswordAsync(email);
if (forgot.Success)
{
    Console.WriteLine("Correo de restablecimiento enviado.");
}

// Confirmar token y actualizar contraseña
var reset = await NightAuthApp.ResetPasswordAsync(token, newPassword);
if (reset.Success)
{
    Console.WriteLine("Contraseña actualizada exitosamente.");
}
```

### 8. Datos del Usuario Autenticado (`result.ToUserData()`)

```csharp
UserData user = result.ToUserData();

Console.WriteLine($"Usuario:     {user.Username}");
Console.WriteLine($"IP:          {user.Ip}");
Console.WriteLine($"HWID:        {user.Hwid}");
Console.WriteLine($"Registro:    {user.CreatedAt}");
Console.WriteLine($"Último login: {user.LastLoginAt}");

foreach (var sub in user.Subscriptions)
{
    Console.WriteLine($"Plan: {sub.Subscription} | Expira: {sub.Expiry} | Clave: {sub.Key}");
}
```

### 9. Variables en la Nube (Globales)

```csharp
var res = await NightAuthApp.GetGlobalVarAsync("server_motd");
string motd = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Variable en la nube: {motd}");
```

### 10. Variables por Usuario (User Variables)

```csharp
// Guardar variable de usuario
await NightAuthApp.SetUserVarAsync("discord_tag", "user#0001");

// Obtener variable de usuario
var res = await NightAuthApp.GetUserVarAsync("discord_tag");
string tag = res.Success ? res.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Discord guardado: {tag}");
```

### 11. Logs de Aplicación y Auditoría

```csharp
await NightAuthApp.LogAsync("Intento sospechoso de modificación de memoria detectado.");
```

### 12. Baneo de Usuarios y Lista Negra

```csharp
await NightAuthApp.BanAsync("Manipulación de seguridad detectada por el Anti-Cheat.");
```

### 13. Webhooks Seguros del Servidor

```csharp
var body = new JsonObject { ["content"] = "Notificación de cliente" };
var resp = await NightAuthApp.TriggerWebhookAsync("7kR0UedlVI", body);
Console.WriteLine($"Respuesta de Webhook: HTTP {(int)resp.StatusCode}");
```

### 14. Descarga Segura de Archivos en Memoria

```csharp
var res = await NightAuthApp.GetFileAsync("385624");

if (res.Success)
{
    byte[] fileBytes = Convert.FromBase64String(res.Payload["data"]?.ToString() ?? "");
    Console.WriteLine($"Descargados {fileBytes.Length} bytes de forma segura.");
    // Cargar o ejecutar directamente en RAM (p. ej. Assembly.Load)
}
```

### 15. Canales de Chat en Vivo

```csharp
// Enviar mensaje
await NightAuthApp.SendChatMessageAsync("¡Hola comunidad!", channel: "general");

// Obtener mensajes
var res = await NightAuthApp.GetChatMessagesAsync("general");
if (res.Success && res.Payload["messages"] is JsonArray messages)
{
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg?["author"]}]: {msg?["message"]}");
    }
}
```

### 16. Controladores de Eventos en Tiempo Real (SSE)

```csharp
using var cts = new CancellationTokenSource();

await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
{
    Console.WriteLine($"[EVENTO] {evt}");
}
```

### 17. Autenticación de Dos Factores (2FA)

```csharp
// Configurar TOTP 2FA
var setup = await NightAuthApp.Setup2FAAsync();
string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";

// Confirmar con código de 6 dígitos
var conf = await NightAuthApp.Confirm2FAAsync("123456");

// Desactivar 2FA
var disable = await NightAuthApp.Disable2FAAsync(password, "123456");
```

### 18. Ciclo de Vida de Sesión y Logout

```csharp
// Heartbeat para mantener sesión activa
await NightAuthApp.HeartbeatAsync();

// Cerrar y revocar sesión
await NightAuthApp.LogoutAsync();
```

### 19. API Síncrona

Cada llamada de autenticación y gestión también tiene su equivalente síncrono en `api` (p. ej. `NightAuthApp`): `Init`, `Login`, `Register`, `License`, `Upgrade`, `Heartbeat`, `Check`, `Logout`, `ChangeUsername`, `ResetHwid`, `Ban`, `CheckBlacklist`, `VerifyFileHash`, `ForgotPassword`, `ResetPassword`, `Setup2FA`, `Confirm2FA`, `Disable2FA`, `GetGlobalVar`, `GetUserVar`, `SetUserVar`, `DownloadFile`, `GetChatMessages`, `SendChatMessage`, `Log`, `GetStats`, `GetOnlineUsers` y `TriggerWebhook`. Bloquean el hilo actual y reportan a través de `Response`/`User`/`App` en lugar de devolver un `NightAuthResult`:

```csharp
NightAuthApp.Init();

if (NightAuthApp.Login(username, password, totpCode))
{
    Console.WriteLine($"Bienvenido de nuevo, {NightAuthApp.User.Username}!");
}
else
{
    Console.WriteLine($"Fallo de login: {NightAuthApp.Response.Message}");
}
```

Estado útil expuesto por la API síncrona: `Response`, `User` (alias `user_data`), `App` (alias `app_data`), `LatestVersion`, `ApiUrl`, `PendingDownloadUrl` y `ToOptions()` para derivar un `NightAuthOptions` de la instancia actual.

### 20. Manejadores de Eventos en Tiempo Real

`api` expone eventos de alto nivel disparados por el motor en tiempo real, además de control explícito sobre el listener en segundo plano:

```csharp
NightAuthApp.OnSessionInvalidated += reason => { Console.WriteLine($"Sesión invalidada: {reason}"); Environment.Exit(0); };
NightAuthApp.OnAppBanned += reason => Console.WriteLine($"App baneada: {reason}");
NightAuthApp.OnAppStatusChanged += status => Console.WriteLine($"Estado de la app cambiado: {status}");
NightAuthApp.OnVersionUpdated += version => Console.WriteLine($"Nueva versión disponible: {version}");
NightAuthApp.OnChatMessageReceived += (msg, channel) => Console.WriteLine($"[{channel}] {msg.Author}: {msg.Message}");

NightAuthApp.StartRealtimeEventListener(); // listener SSE en segundo plano
// ... después
NightAuthApp.StopRealtimeEventListener();
```

Para control total también puedes consumir el stream SSE manualmente con `ReadEventsAsync` (ver sección 16).

### 21. `NightAuthClient` Independiente

El motor asíncrono moderno está disponible como un cliente independiente, desacoplado del wrapper legado `api`:

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
    // NightAuthResult expone Payload (JsonObject), RetryAfter y Error
    string tag = login.Payload["user"]?["username"]?.GetValue<string>() ?? "";
}
```

Helpers asíncronos adicionales disponibles tanto en `NightAuthClient` como en `api`: `ChangeUsernameAsync`, `GetUserVariablesAsync` (lista todas las variables de usuario), `VerifyFileHashAsync`, `GetStatsAsync`, `GetOnlineUsersAsync` y tres sobrecargas de `TriggerWebhookAsync` (payload JSON, JSON con `content`/`parameters` personalizados y subida multipart con archivo: `TriggerWebhookAsync(id, fileBytes, fileName, payload, content, parameters)`).

### 22. `SecurityProtections`

Helpers estáticos anti-manipulación. `RunSecurityChecks()` (invocado automáticamente por `Init`) solo realiza la comprobación de proxy del sistema; llama a las demás comprobaciones explícitamente:

```csharp
SecurityProtections.CheckSystemProxy();          // termina si hay un proxy HTTP activo
SecurityProtections.CheckDebugger();             // termina si hay un debugger (local o remoto)
SecurityProtections.CheckBlacklistedProcesses(); // busca herramientas de análisis (HTTPDebugger, Cheat Engine, x64dbg, Wireshark, dnSpy, ...)

string hwid = SecurityProtections.GetHardwareId(); // identificador de hardware estable usado para el HWID lock
```

> [!NOTE]
> Define la variable de entorno `NIGHTAUTH_IGNORE_SECURITY=1` para omitir el escaneo de procesos en lista negra durante desarrollo/pruebas.

### 23. Auto-actualizaciones Firmadas

La auto-actualización sin firma (`ApplyUpdate`) está obsoleta y desactivada. Descarga y verifica releases firmados en su lugar:

```csharp
// Helper todo en uno: descarga el manifiesto por HTTPS, verifica la firma Ed25519
// contra tu clave de release fijada, rechaza secuencias obsoletas, descarga el artefacto
// y devuelve la ruta del archivo instalado.
string path = await SignedUpdateInstaller.DownloadAsync(
    manifestUri: new Uri("https://releases.example.com/manifest.json"),
    pinnedReleaseKeyHex: "YOUR_PINNED_ED25519_PUBLIC_KEY_HEX",
    application: "my-app",
    destination: "updates/my-app.exe");

// O controla el proceso manualmente:
var verified = SignedUpdate.Verify(envelopeJson, pinnedKeyHex, expectedApplication, minimumSequence);
Console.WriteLine($"Verificado {verified.Application} v{verified.Version} (seq {verified.Sequence})");
await SignedUpdate.DownloadAsync(verified, "updates/my-app.exe");
```

---

## 🖥️ Ejecutar las Aplicaciones de Ejemplo

Inicia los clientes de referencia desde la carpeta `source`:

```bash
# Cliente de Consola en Terminal
dotnet run --project console/NightAuth.Console.csproj

# Cliente de Escritorio WPF (Windows)
dotnet run --project desktop/NightAuth.Desktop.csproj
```

---

## 📄 Licencia

Este proyecto está bajo la licencia **Elastic License 2.0**.

* No está permitido ofrecer el software a terceros como servicio gestionado o alojado.
* No está permitido modificar, deshabilitar o eludir el sistema de licencias.
* No está permitido alterar ni eliminar avisos de copyright o de licencia.

Consulta el archivo [LICENSE](../LICENSE) para el texto legal completo.
