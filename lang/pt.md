# NightAuth C# .NET SDK — Deixe uma estrela no GitHub ⭐

SDK oficial do **NightAuth** para C# / .NET & Conjunto de clientes de referência (Interface moderna em **WPF** e Console interativo no terminal). Proteja seu software contra pirataria, engenharia reversa e adulteração com licenciamento em nuvem, bloqueio de HWID, chat em tempo real e streaming em memória em minutos.

Portal Oficial: [https://nightauth.org](https://nightauth.org)

---

**Languages / Idiomas / Langues / Sprachen:**  
[English](../README.md) • [Español](es.md) • [Français](fr.md) • [Deutsch](de.md) • [Italiano](it.md) • [Português](pt.md) • [Русский](ru.md) • [Türkçe](tr.md) • [Polski](pl.md) • [中文](zh.md)

---

> [!NOTE]
> ### 📺 Tutoriais em Vídeo & Guias de Configuração
> Tutoriais oficiais em vídeo e guias passo a passo explicando como configurar aplicativos, planos, assinaturas, variáveis em nuvem, webhooks e licenças no painel do NightAuth **serão publicados em breve**.

---

## 📸 Capturas de Tela

### Aplicativo Desktop (WPF Enterprise Dark Theme)

| Tela de Início / Autenticação | Painel de Controle & Assinaturas |
| :---: | :---: |
| ![NightAuth Desktop Login](../image/desktop.png) | ![NightAuth Desktop Dashboard](../image/desktop-dashboard.png) |

### Aplicativo de Console (CLI Interativa)

| Login pelo Terminal | Painel Interativo de Console |
| :---: | :---: |
| ![NightAuth Console Login](../image/console.png) | ![NightAuth Console Dashboard](../image/console-dashboard.png) |

---

## 🛡️ Práticas de Segurança Recomendadas

* **Compilação Nativa AOT:** O bytecode IL do C# pode ser descompilado facilmente com ferramentas como dnSpy ou ILSpy. Para produção, publique usando **Native AOT** (`dotnet publish -c Release -r win-x64 /p:PublishAot=true`).
* **Virtualização de Código Empresarial:** Proteja seus executáveis com protetores e ofuscadores como **VMProtect**, **Themida**, **Eazfuscator.NET** ou **Obfuscar**.
* **Integridade de Memória & Anti-Depuração:** O NightAuth inclui um helper `SecurityProtections` com verificações contra proxies do sistema, depuradores e processos em lista negra (`HTTPDebugger`, `CheatEngine`, `x64dbg`, `Wireshark`, `dnSpy`). Por padrão, apenas a verificação de proxy é executada; chame `CheckDebugger()` e `CheckBlacklistedProcesses()` manualmente na inicialização para proteção completa (veja [SecurityProtections](#22-securityprotections)).
* **Execução Segura em Memória:** Ao baixar arquivos protegidos via `NightAuthApp.GetFileAsync()`, **não grave os bytes no disco**. Carregue e execute diretamente na RAM (via `Assembly.Load()`) e limpe os buffers de memória (`CryptographicOperations.ZeroMemory()`) imediatamente.

---

## 📦 Requisitos & Instalação

Requer **.NET 10.0 SDK** (ou superior).

```bash
dotnet build NightAuth.sln
```

---

## ⚙️ Configuração & Inicialização

Configure as credenciais do seu aplicativo obtidas no painel do NightAuth:

```csharp
public static api NightAuthApp = new api(
    name: "",
    apikey: "",
    version: "",
    token: null
);
```

### Inicializar Handshake Criptográfico (`InitializeAsync`)

Chame `InitializeAsync()` antes de qualquer outra chamada para estabelecer a troca de chaves Diffie-Hellman (X25519) e a sessão criptografada:

```csharp
var init = await NightAuthApp.InitializeAsync();

if (!init.Success)
{
    Console.WriteLine($"Falha na inicialização: {init.DisplayMessage}");
    Environment.Exit(1);
}
```

---

## 🚀 Guia de Uso do SDK

### 1. Verificação de Sessão & Blacklist

```csharp
// Validar sessão ativa
var check = await NightAuthApp.CheckAsync();
Console.WriteLine($"Status da sessão: {check.DisplayMessage}");

// Verificar se máquina ou IP está na blacklist
var blacklist = await NightAuthApp.CheckBlacklistAsync();
if (blacklist.Success && blacklist.Payload["blacklisted"]?.GetValue<bool>() == true)
{
    Console.WriteLine("Acesso negado: Computador ou IP na lista negra.");
    Environment.Exit(0);
}
```

### 2. Login com Usuário & Senha (e 2FA)

```csharp
var result = await NightAuthApp.LoginAsync(username, password, totpCode);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Bem-vindo de volta, {user.Username}!");
}
else
{
    Console.WriteLine($"Falha no login: {result.DisplayMessage}");
}
```

### 3. Registro com Chave de Licença

```csharp
var result = await NightAuthApp.RegisterAsync(username, password, licenseKey, email);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine($"Registrado com sucesso: {user.Username}");
}
else
{
    Console.WriteLine($"Falha no registro: {result.DisplayMessage}");
}
```

### 4. Acesso Apenas com Licença (Key Only)

```csharp
var result = await NightAuthApp.LicenseAsync(licenseKey);

if (result.Success)
{
    UserData user = result.ToUserData();
    Console.WriteLine("Licença verificada com sucesso.");
}
else
{
    Console.WriteLine($"Acesso negado: {result.DisplayMessage}");
}
```

### 5. Atualizar Assinatura / Resgatar Chave (Upgrade)

```csharp
var result = await NightAuthApp.UpgradeAsync(username, newLicenseKey);

if (result.Success)
{
    Console.WriteLine("Assinatura atualizada com sucesso.");
}
else
{
    Console.WriteLine($"Falha na atualização: {result.DisplayMessage}");
}
```

### 6. Redefinir Hardware ID (HWID Lock)

```csharp
var result = await NightAuthApp.ResetHwidAsync(username, password, totpCode);

if (result.Success)
{
    Console.WriteLine("Bloqueio de HWID redefinido com sucesso.");
}
else
{
    Console.WriteLine($"Falha ao redefinir HWID: {result.DisplayMessage}");
}
```

### 7. Recuperação de Senha (Forgot Password & Reset)

```csharp
// Solicitar e-mail de recuperação
var forgot = await NightAuthApp.ForgotPasswordAsync(email);
if (forgot.Success)
{
    Console.WriteLine("E-mail de redefinição de senha enviado.");
}

// Confirmar token e redefinir senha
var reset = await NightAuthApp.ResetPasswordAsync(token, newPassword);
if (reset.Success)
{
    Console.WriteLine("Senha redefinida com sucesso.");
}
```

### 8. Dados do Perfil do Usuário (`result.ToUserData()`)

```csharp
UserData user = result.ToUserData();

Console.WriteLine($"Usuário:      {user.Username}");
Console.WriteLine($"IP:           {user.Ip}");
Console.WriteLine($"HWID:         {user.Hwid}");
Console.WriteLine($"Cadastro:     {user.CreatedAt}");
Console.WriteLine($"Último login: {user.LastLoginAt}");

foreach (var sub in user.Subscriptions)
{
    Console.WriteLine($"Plano: {sub.Subscription} | Expira: {sub.Expiry} | Chave: {sub.Key}");
}
```

### 9. Variáveis na Nuvem (Globais)

```csharp
var res = await NightAuthApp.GetGlobalVarAsync("server_motd");
string motd = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Variável Cloud: {motd}");
```

### 10. Variáveis do Usuário (User Variables)

```csharp
// Definir variável
await NightAuthApp.SetUserVarAsync("discord_tag", "user#0001");

// Obter variável
var res = await NightAuthApp.GetUserVarAsync("discord_tag");
string tag = res.Success ? res.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";
Console.WriteLine($"Discord salvo: {tag}");
```

### 11. Registros de Auditoria

```csharp
await NightAuthApp.LogAsync("Detecção de alteração suspeita na memória.");
```

### 12. Banir Usuário & Bloquear Máquina

```csharp
await NightAuthApp.BanAsync("Tentativa de manipulação detectada pelo Anti-Cheat.");
```

### 13. Webhooks no Servidor

```csharp
var body = new JsonObject { ["content"] = "Notificação" };
var resp = await NightAuthApp.TriggerWebhookAsync("7kR0UedlVI", body);
Console.WriteLine($"Resposta Webhook: HTTP {(int)resp.StatusCode}");
```

### 14. Download Seguro na Memória

```csharp
var res = await NightAuthApp.GetFileAsync("385624");

if (res.Success)
{
    byte[] fileBytes = Convert.FromBase64String(res.Payload["data"]?.ToString() ?? "");
    Console.WriteLine($"{fileBytes.Length} bytes baixados com segurança.");
    // Execução direta na RAM (ex.: Assembly.Load)
}
```

### 15. Canais de Chat em Tempo Real

```csharp
// Enviar mensagem
await NightAuthApp.SendChatMessageAsync("Olá comunidade!", channel: "general");

// Buscar mensagens
var res = await NightAuthApp.GetChatMessagesAsync("general");
if (res.Success && res.Payload["messages"] is JsonArray messages)
{
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg?["author"]}]: {msg?["message"]}");
    }
}
```

### 16. Eventos em Tempo Real (SSE)

```csharp
using var cts = new CancellationTokenSource();

await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
{
    Console.WriteLine($"[EVENTO] {evt}");
}
```

### 17. Autenticação de Dois Fatores (2FA)

```csharp
// Configurar TOTP 2FA
var setup = await NightAuthApp.Setup2FAAsync();
string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";

// Confirmar com código de 6 dígitos
var conf = await NightAuthApp.Confirm2FAAsync("123456");

// Desativar 2FA
var disable = await NightAuthApp.Disable2FAAsync(password, "123456");
```

### 18. Ciclo de Vida da Sessão e Logout

```csharp
// Heartbeat para manter sessão
await NightAuthApp.HeartbeatAsync();

// Encerrar e revogar sessão
await NightAuthApp.LogoutAsync();
```

### 19. API Síncrona

Toda chamada de autenticação e gerenciamento também possui uma contraparte síncrona em `api` (ex.: `NightAuthApp`): `Init`, `Login`, `Register`, `License`, `Upgrade`, `Heartbeat`, `Check`, `Logout`, `ChangeUsername`, `ResetHwid`, `Ban`, `CheckBlacklist`, `VerifyFileHash`, `ForgotPassword`, `ResetPassword`, `Setup2FA`, `Confirm2FA`, `Disable2FA`, `GetGlobalVar`, `GetUserVar`, `SetUserVar`, `DownloadFile`, `GetChatMessages`, `SendChatMessage`, `Log`, `GetStats`, `GetOnlineUsers` e `TriggerWebhook`. Elas bloqueiam a thread atual e reportam os resultados por meio de `Response`/`User`/`App` em vez de retornar um `NightAuthResult`:

```csharp
NightAuthApp.Init();

if (NightAuthApp.Login(username, password, totpCode))
{
    Console.WriteLine($"Bem-vindo de volta, {NightAuthApp.User.Username}!");
}
else
{
    Console.WriteLine($"Falha no login: {NightAuthApp.Response.Message}");
}
```

Estado útil exposto pela API síncrona: `Response`, `User` (alias `user_data`), `App` (alias `app_data`), `LatestVersion`, `ApiUrl`, `PendingDownloadUrl` e `ToOptions()` para derivar um `NightAuthOptions` da instância atual.

### 20. Manipuladores de Eventos em Tempo Real

`api` expõe eventos de alto nível disparados pelo mecanismo em tempo real, além de controle explícito sobre o listener em segundo plano:

```csharp
NightAuthApp.OnSessionInvalidated += reason => { Console.WriteLine($"Sessão invalidada: {reason}"); Environment.Exit(0); };
NightAuthApp.OnAppBanned += reason => Console.WriteLine($"App banido: {reason}");
NightAuthApp.OnAppStatusChanged += status => Console.WriteLine($"Status do app alterado: {status}");
NightAuthApp.OnVersionUpdated += version => Console.WriteLine($"Nova versão disponível: {version}");
NightAuthApp.OnChatMessageReceived += (msg, channel) => Console.WriteLine($"[{channel}] {msg.Author}: {msg.Message}");

NightAuthApp.StartRealtimeEventListener(); // listener SSE em segundo plano
// ... depois
NightAuthApp.StopRealtimeEventListener();
```

Para controle total, você também pode consumir o stream SSE manualmente com `ReadEventsAsync` (ver seção 16).

### 21. `NightAuthClient` Independente

O motor assíncrono moderno está disponível como um cliente independente, desacoplado do wrapper legado `api`:

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
    // NightAuthResult expõe Payload (JsonObject), RetryAfter e Error
    string tag = login.Payload["user"]?["username"]?.GetValue<string>() ?? "";
}
```

Auxiliares assíncronos adicionais disponíveis tanto em `NightAuthClient` quanto em `api`: `ChangeUsernameAsync`, `GetUserVariablesAsync` (lista todas as variáveis de usuário), `VerifyFileHashAsync`, `GetStatsAsync`, `GetOnlineUsersAsync` e três sobrecargas de `TriggerWebhookAsync` (payload JSON, JSON com `content`/`parameters` personalizados e upload multipart com arquivo: `TriggerWebhookAsync(id, fileBytes, fileName, payload, content, parameters)`).

### 22. `SecurityProtections`

Auxiliares estáticos anti-manipulação. `RunSecurityChecks()` (invocado automaticamente pelo `Init`) executa apenas a verificação de proxy do sistema; chame as demais verificações explicitamente:

```csharp
SecurityProtections.CheckSystemProxy();          // encerra se houver um proxy HTTP do sistema ativo
SecurityProtections.CheckDebugger();             // encerra se houver um depurador (local ou remoto) anexado
SecurityProtections.CheckBlacklistedProcesses(); // procura ferramentas de análise (HTTPDebugger, Cheat Engine, x64dbg, Wireshark, dnSpy, ...)

string hwid = SecurityProtections.GetHardwareId(); // identificador de hardware estável usado no bloqueio por HWID
```

> [!NOTE]
> Defina a variável de ambiente `NIGHTAUTH_IGNORE_SECURITY=1` para ignorar a varredura de processos em lista negra durante desenvolvimento/testes.

### 23. Atualizações Automáticas Assinadas

A autoatualização não assinada (`ApplyUpdate`) está obsoleta e desativada. Baixe e verifique releases assinados:

```csharp
// Auxiliar completo: baixa o manifesto via HTTPS, verifica a assinatura Ed25519
// contra sua chave de release fixada, rejeita sequências obsoletas, baixa o artefato
// e retorna o caminho do arquivo instalado.
string path = await SignedUpdateInstaller.DownloadAsync(
    manifestUri: new Uri("https://releases.example.com/manifest.json"),
    pinnedReleaseKeyHex: "YOUR_PINNED_ED25519_PUBLIC_KEY_HEX",
    application: "my-app",
    destination: "updates/my-app.exe");

// Ou controle o pipeline manualmente:
var verified = SignedUpdate.Verify(envelopeJson, pinnedKeyHex, expectedApplication, minimumSequence);
Console.WriteLine($"Verificado {verified.Application} v{verified.Version} (seq {verified.Sequence})");
await SignedUpdate.DownloadAsync(verified, "updates/my-app.exe");
```

---

## 🖥️ Executar os Clientes de Exemplo

Inicie os clientes a partir da pasta `source`:

```bash
# Cliente de Console Interativo
dotnet run --project console/NightAuth.Console.csproj

# Cliente Desktop WPF (Windows)
dotnet run --project desktop/NightAuth.Desktop.csproj
```

---

## 📄 Licença

Este projeto é licenciado sob a **Elastic License 2.0**.

* Não é permitido fornecer o software como serviço hospedado ou gerenciado.
* Não é permitido alterar, desativar ou burlar o sistema de licenças.
* Não é permitido remover avisos de direitos autorais.

Consulte o arquivo [LICENSE](../LICENSE) para o texto legal completo.
