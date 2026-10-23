using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NightAuth;

namespace NightAuth.ConsoleApp
{
    internal static class Program
    {

        public static api NightAuthApp = new api(
            name: "",
            apikey: "",
            version: "1.0",
            token: null
        );

        private const string C_RESET = "\x1b[0m";
        private const string C_BOLD = "\x1b[1m";
        private const string C_DIM = "\x1b[38;2;100;116;139m";
        private const string C_MUTED = "\x1b[38;2;148;163;184m";
        private const string C_TEXT = "\x1b[38;2;226;232;240m";
        private const string C_WHITE = "\x1b[38;2;248;250;252m";
        private const string C_BLUE = "\x1b[38;2;59;130;246m";
        private const string C_GLOW = "\x1b[38;2;96;165;250m";
        private const string C_CYAN = "\x1b[38;2;56;189;248m";
        private const string C_GREEN = "\x1b[38;2;52;211;153m";
        private const string C_AMBER = "\x1b[38;2;251;191;36m";
        private const string C_RED = "\x1b[38;2;248;113;113m";
        private const string C_BORDER = "\x1b[38;2;42;50;70m";
        private const string C_BADGE_BG = "\x1b[48;2;23;37;68m";
        private const string C_GREEN_BG = "\x1b[48;2;14;46;38m";
        private const string C_RED_BG = "\x1b[48;2;56;20;26m";
        
        private static UserData _user = new();
        private static CancellationTokenSource? _sessionCts;

        private static async Task Main(string[] args)
        {
            if (!Console.IsInputRedirected)
            {
                try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
                try { Console.Title = "NightAuth — Secure Authentication"; } catch { }
            }

            string? hwid = null;
            try { hwid = SecurityProtections.GetHardwareId(); } catch { }
            ClearScreen();
            RenderBanner(NightAuthApp.Name, hwid, false);
            await PerformHandshakeAsync();

            ClearScreen();
            RenderBanner(NightAuthApp.Name, hwid, true);

            if (!await HandleAuthenticationFlowAsync())
            {
                await NightAuthApp.DisposeAsync();
                return;
            }

            ClearScreen();
            RenderBanner(NightAuthApp.Name, hwid, true);
            RenderUserProfile();

            StartRealtimeEventListener();
            StartHeartbeatThread();
            await RunApplicationFeaturesMenuAsync();

            _sessionCts?.Cancel();
            await NightAuthApp.DisposeAsync();
        }

        #region UI Rendering

        private static void ClearScreen()
        {
            if (!Console.IsInputRedirected)
            {
                try { Console.Clear(); return; } catch { }
            }
            Console.Write("\x1b[2J\x1b[H");
        }

        private static void PrintHr()
        {
            Console.WriteLine($"  {C_BORDER}────────────────────────────────────────────────────────────────────────────{C_RESET}");
        }

        private static void PrintSectionHeader(string title, string subtitle)
        {
            Console.WriteLine($"\n  {C_BLUE}▌{C_RESET} {C_WHITE}{C_BOLD}{title}{C_RESET}  {C_DIM}·  {subtitle}{C_RESET}");
            PrintHr();
        }

        private static void PrintOk(string title, string detail = "")
        {
            Console.Write($"\n  {C_GREEN_BG}{C_GREEN}{C_BOLD} ✓ SUCCESS {C_RESET} {C_WHITE}{C_BOLD}{title}{C_RESET}");
            if (!string.IsNullOrEmpty(detail))
            {
                Console.Write($" {C_MUTED}— {detail}{C_RESET}");
            }
            Console.WriteLine("\n");
        }

        private static void PrintErr(string title, string detail = "")
        {
            Console.Write($"\n  {C_RED_BG}{C_RED}{C_BOLD} ✕ ERROR {C_RESET} {C_WHITE}{C_BOLD}{title}{C_RESET}");
            if (!string.IsNullOrEmpty(detail))
            {
                Console.Write($" {C_RED}— {detail}{C_RESET}");
            }
            Console.WriteLine("\n");
        }

        private static void PrintInfo(string label, string val)
        {
            Console.WriteLine($"  {C_BLUE}•{C_RESET} {C_MUTED}{label,-18}{C_RESET} {C_CYAN}{val}{C_RESET}");
        }

        private static string Ask(string prompt)
        {
            Console.Write($"  {C_GLOW}❯{C_RESET} {C_TEXT}{prompt}{C_RESET}");
            string? line = Console.ReadLine();
            return line?.Trim() ?? string.Empty;
        }

        private static string AskPassword(string prompt)
        {
            Console.Write($"  {C_GLOW}❯{C_RESET} {C_TEXT}{prompt}{C_RESET}");
            if (Console.IsInputRedirected)
            {
                return Console.ReadLine()?.Trim() ?? string.Empty;
            }

            var pass = new System.Text.StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (pass.Length > 0)
                    {
                        pass.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    pass.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return pass.ToString();
        }

        private static string FormatTimestamp(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "-";
            if (long.TryParse(raw, out long epoch))
            {
                if (epoch > 1000000000000) epoch /= 1000;
                return DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime.ToString("yyyy-MM-dd HH:mm UTC");
            }
            if (DateTimeOffset.TryParse(raw, out var dto))
            {
                return dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm UTC");
            }
            return raw;
        }

        private static void RenderBanner(string? appName, string? hwid, bool connected)
        {
            Console.WriteLine();
            Console.WriteLine($"  {C_BLUE}{C_BOLD}  _   _ _       _     _      _         _   _     {C_RESET}");
            Console.WriteLine($"  {C_BLUE}{C_BOLD} | \\ | (_) __ _| |__ | |_   / \\  _   _| |_| |__  {C_RESET}");
            Console.WriteLine($"  {C_GLOW}{C_BOLD} |  \\| | |/ _` | '_ \\| __| / _ \\| | | | __| '_ \\ {C_RESET}");
            Console.WriteLine($"  {C_GLOW}{C_BOLD} | |\\  | | (_| | | | | |_ / ___ \\ |_| | |_| | | |{C_RESET}");
            Console.WriteLine($"  {C_CYAN}{C_BOLD} |_| \\_|_|\\__, |_| |_|\\__/_/   \\_\\__,_|\\__|_| |_|{C_RESET}");
            Console.WriteLine($"  {C_CYAN}{C_BOLD}          |___/                                  {C_RESET}  {C_BADGE_BG}{C_GLOW}{C_BOLD} NATIVE C# CLI {C_RESET}\n");

            string statusWord = connected ? "Connected" : "Initializing";
            string statusIcon = connected ? $"{C_GREEN}● {C_RESET}" : $"{C_AMBER}◌ {C_RESET}";
            string nameDisplay = !string.IsNullOrWhiteSpace(appName) ? appName : "Unconfigured";
            if (nameDisplay.Length > 26) nameDisplay = nameDisplay[..26];

            Console.WriteLine($"  {C_BORDER}╭──────────────────────────────────────────────────────────────────────────╮{C_RESET}");
            Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}APPLICATION{C_RESET}  {C_WHITE}{C_BOLD}{nameDisplay,-26}{C_RESET}  {C_DIM}STATUS{C_RESET}  {statusIcon}{statusWord,-16}     {C_BORDER}│{C_RESET}");
            if (!string.IsNullOrWhiteSpace(hwid))
            {
                string hwidDisplay = hwid.Length > 56 ? hwid[..56] : hwid;
                Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}DEVICE HWID{C_RESET}  {C_CYAN}{hwidDisplay,-56}{C_RESET}  {C_BORDER}│{C_RESET}");
            }
            Console.WriteLine($"  {C_BORDER}╰──────────────────────────────────────────────────────────────────────────╯{C_RESET}");
        }

        private static void RenderPreSessionMenu()
        {
            PrintSectionHeader("AUTHENTICATION & PRE-SESSION GATEWAY", "Select an operation to continue");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 01 {C_RESET} {C_WHITE}Sign In (Username & Password){C_RESET}          {C_BADGE_BG}{C_GLOW}{C_BOLD} 06 {C_RESET} {C_WHITE}Complete Password Reset (Token){C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 02 {C_RESET} {C_WHITE}Register New Account + License{C_RESET}         {C_BADGE_BG}{C_GLOW}{C_BOLD} 07 {C_RESET} {C_WHITE}Reset Hardware Binding (HWID){C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 03 {C_RESET} {C_WHITE}Instant License Key Login{C_RESET}              {C_BADGE_BG}{C_GLOW}{C_BOLD} 08 {C_RESET} {C_WHITE}Check HWID Blacklist Status{C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 04 {C_RESET} {C_WHITE}Upgrade Existing Account Plan{C_RESET}          {C_BADGE_BG}{C_GLOW}{C_BOLD} 09 {C_RESET} {C_WHITE}Verify Binary SHA-256 Hash{C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 05 {C_RESET} {C_WHITE}Forgot Password (Email Link){C_RESET}           {C_RED_BG}{C_RED}{C_BOLD} 10 {C_RESET} {C_MUTED}Exit Application{C_RESET}\n");
        }

        private static void RenderUserProfile()
        {
            Console.WriteLine();
            Console.WriteLine($"  {C_BORDER}╭─ {C_GLOW}{C_BOLD}AUTHENTICATED IDENTITY & ENTITLEMENTS{C_BORDER} ──────────────────────────────────╮{C_RESET}");
            string uName = string.IsNullOrEmpty(_user.Username) ? "Authenticated" : _user.Username;
            string uStatus = "ACTIVE";
            string uEmail = string.IsNullOrEmpty(_user.Email) ? "Not linked" : _user.Email;
            string uIp = string.IsNullOrEmpty(_user.Ip) ? "Protected / TLS" : _user.Ip;
            string uCreated = FormatTimestamp(_user.CreatedAt);
            string uLast = FormatTimestamp(_user.LastLoginAt);

            if (uName.Length > 24) uName = uName[..24];
            if (uEmail.Length > 24) uEmail = uEmail[..24];
            if (uCreated.Length > 24) uCreated = uCreated[..24];
            if (uLast.Length > 20) uLast = uLast[..20];

            Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}Username :{C_RESET} {C_WHITE}{C_BOLD}{uName,-24}{C_RESET}  {C_DIM}Status     :{C_RESET} {C_GREEN}{C_BOLD}{uStatus,-20}{C_RESET}  {C_BORDER}│{C_RESET}");
            Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}Email    :{C_RESET} {C_TEXT}{uEmail,-24}{C_RESET}  {C_DIM}Client IP  :{C_RESET} {C_CYAN}{uIp,-20}{C_RESET}  {C_BORDER}│{C_RESET}");
            Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}Created  :{C_RESET} {C_MUTED}{uCreated,-24}{C_RESET}  {C_DIM}Last Login :{C_RESET} {C_MUTED}{uLast,-20}{C_RESET}  {C_BORDER}│{C_RESET}");
            Console.WriteLine($"  {C_BORDER}├──────────────────────────────────────────────────────────────────────────┤{C_RESET}");

            if (_user.Subscriptions != null && _user.Subscriptions.Count > 0)
            {
                foreach (var s in _user.Subscriptions)
                {
                    string subName = s.Subscription ?? "Default";
                    string keyStr = string.IsNullOrEmpty(s.Key) ? "Active Account" : s.Key;
                    string expStr = FormatTimestamp(s.Expiry);
                    if (subName.Length > 16) subName = subName[..16];
                    if (keyStr.Length > 22) keyStr = keyStr[..22];
                    if (expStr.Length > 16) expStr = expStr[..16];
                    Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_GREEN}●{C_RESET} {C_WHITE}{C_BOLD}{subName,-16}{C_RESET} {C_DIM}Key:{C_RESET} {C_CYAN}{keyStr,-22}{C_RESET} {C_DIM}Exp:{C_RESET} {C_AMBER}{expStr,-16}{C_RESET}    {C_BORDER}│{C_RESET}");
                }
            }
            else
            {
                Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}No active subscription tiers attached to this account.{C_RESET}                   {C_BORDER}│{C_RESET}");
            }
            Console.WriteLine($"  {C_BORDER}╰──────────────────────────────────────────────────────────────────────────╯{C_RESET}\n");
        }

        private static void RenderWorkspaceMenu()
        {
            PrintSectionHeader("WORKSPACE MODULES & ACTIONS", "Select a command [1-21]");
            Console.WriteLine($"  {C_DIM}CLOUD VARIABLES & CHAT{C_RESET}                    {C_DIM}SECURITY & TELEMETRY{C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 01 {C_RESET} {C_WHITE}Read Global Variable{C_RESET}                 {C_BADGE_BG}{C_GLOW}{C_BOLD} 10 {C_RESET} {C_WHITE}Setup Two-Factor Auth (2FA){C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 02 {C_RESET} {C_WHITE}Get User Cloud Variable{C_RESET}              {C_BADGE_BG}{C_GLOW}{C_BOLD} 11 {C_RESET} {C_WHITE}Disable Two-Factor Auth (2FA){C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 03 {C_RESET} {C_WHITE}Set User Cloud Variable{C_RESET}              {C_BADGE_BG}{C_GLOW}{C_BOLD} 12 {C_RESET} {C_WHITE}Download Protected Asset{C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 04 {C_RESET} {C_WHITE}List All User Variables{C_RESET}              {C_BADGE_BG}{C_GLOW}{C_BOLD} 13 {C_RESET} {C_WHITE}Trigger Server Webhook{C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 05 {C_RESET} {C_WHITE}Enter Live Chat Channel{C_RESET}              {C_BADGE_BG}{C_GLOW}{C_BOLD} 14 {C_RESET} {C_WHITE}Send Audit Log Event{C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 19 {C_RESET} {C_WHITE}Fetch Paginated Chat History{C_RESET}         {C_BADGE_BG}{C_GLOW}{C_BOLD} 15 {C_RESET} {C_WHITE}Verify Session Entitlement{C_RESET}\n");

            Console.WriteLine($"  {C_DIM}ACCOUNT & INSIGHTS{C_RESET}                        {C_DIM}SESSION LIFECYCLE{C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 06 {C_RESET} {C_WHITE}Application Live Statistics{C_RESET}          {C_BADGE_BG}{C_GLOW}{C_BOLD} 16 {C_RESET} {C_WHITE}Send Heartbeat Ping{C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 07 {C_RESET} {C_WHITE}View Online Active Users{C_RESET}             {C_BADGE_BG}{C_GLOW}{C_BOLD} 17 {C_RESET} {C_WHITE}Emergency Killswitch / Ban{C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 08 {C_RESET} {C_WHITE}Change Account Username{C_RESET}              {C_BADGE_BG}{C_GLOW}{C_BOLD} 18 {C_RESET} {C_WHITE}Stream Live Events (SSE){C_RESET}");
            Console.WriteLine($"  {C_BADGE_BG}{C_GLOW}{C_BOLD} 09 {C_RESET} {C_WHITE}Upgrade Plan with License Key{C_RESET}        {C_BADGE_BG}{C_GLOW}{C_BOLD} 20 {C_RESET} {C_WHITE}Session Revocation Audit Demo{C_RESET}");
            Console.WriteLine($"                                            {C_RED_BG}{C_RED}{C_BOLD} 21 {C_RESET} {C_MUTED}Sign Out & Terminate Session{C_RESET}\n");
        }

        #endregion

        #region Setup & Handshake

        private static async Task PerformHandshakeAsync()
        {
            Console.Write($"\n  {C_MUTED}◌ Establishing encrypted session with NightAuth Cloud...{C_RESET}\r");
            var init = await NightAuthApp.InitializeAsync();
            Console.Write("                                                                      \r");

            if (!init.Success)
            {
                PrintErr("Handshake Failed", init.DisplayMessage);
                Console.WriteLine($"  {C_DIM}Please verify your App Name, API Key, and Server URL.{C_RESET}\n");
                Ask("Press Enter to exit...");
                Environment.Exit(1);
            }
        }

        #endregion

        #region Pre-Session Flows

        private static async Task<bool> HandleAuthenticationFlowAsync()
        {
            while (true)
            {
                RenderPreSessionMenu();
                string choice = Ask("Select option [1-10]: ");

                switch (choice)
                {
                    case "1":
                    case "01":
                        if (await HandleLoginAsync()) return true;
                        break;
                    case "2":
                    case "02":
                        if (await HandleRegisterAsync()) return true;
                        break;
                    case "3":
                    case "03":
                        if (await HandleLicenseOnlyAsync()) return true;
                        break;
                    case "4":
                    case "04":
                        await HandleUpgradeAsync();
                        break;
                    case "5":
                    case "05":
                        await HandleForgotPasswordAsync();
                        break;
                    case "6":
                    case "06":
                        await HandleResetPasswordAsync();
                        break;
                    case "7":
                    case "07":
                        await HandleResetHwidAsync();
                        break;
                    case "8":
                    case "08":
                        await HandleCheckBlacklistAsync();
                        break;
                    case "9":
                    case "09":
                        await HandleVerifyHashAsync();
                        break;
                    case "10":
                    case "0":
                        return false;
                }
            }
        }

        private static async Task<bool> HandleLoginAsync()
        {
            PrintSectionHeader("ACCOUNT SIGN IN", "Authenticate with your username and password");
            string user = Ask("Username: ");
            string pass = AskPassword("Password: ");
            string totp = Ask($"2FA Code {C_DIM}(Optional, Enter to skip){C_TEXT}: ");

            var result = await NightAuthApp.LoginAsync(user, pass, string.IsNullOrWhiteSpace(totp) ? null : totp);
            if (result.Success)
            {
                _user = result.ToUserData();
                PrintOk("Session Authenticated", "Welcome back to NightAuth.");
                return true;
            }

            PrintErr("Login Failed", result.DisplayMessage);
            return false;
        }

        private static async Task<bool> HandleRegisterAsync()
        {
            PrintSectionHeader("CREATE NEW ACCOUNT", "Activate a license key and bind your credentials");
            string user = Ask("Username: ");
            string pass = AskPassword("Password: ");
            string key = Ask("License Key: ");
            string email = Ask($"Email Address {C_DIM}(Optional, Enter to skip){C_TEXT}: ");

            var result = await NightAuthApp.RegisterAsync(user, pass, key, email ?? "");
            if (result.Success)
            {
                _user = result.ToUserData();
                PrintOk("Account Registered & Activated", "Session started automatically.");
                return true;
            }

            PrintErr("Registration Failed", result.DisplayMessage);
            return false;
        }

        private static async Task<bool> HandleLicenseOnlyAsync()
        {
            PrintSectionHeader("LICENSE KEY AUTHENTICATION", "Instant passwordless login bound to your HWID");
            string key = Ask("Enter License Key: ");

            var result = await NightAuthApp.LicenseAsync(key);
            if (result.Success)
            {
                _user = result.ToUserData();
                PrintOk("License Validated", "Direct hardware-bound session active.");
                return true;
            }

            PrintErr("License Validation Failed", result.DisplayMessage);
            return false;
        }

        private static async Task<bool> HandleUpgradeAsync(string? knownUser = null)
        {
            PrintSectionHeader("UPGRADE ACCOUNT SUBSCRIPTION", "Attach a new license key tier to an account");
            string user = knownUser ?? Ask("Target Username: ");
            if (!string.IsNullOrEmpty(knownUser))
            {
                PrintInfo("Target Account:", user);
            }
            string key = Ask("Upgrade License Key: ");

            var result = await NightAuthApp.UpgradeAsync(user, key);
            if (result.Success)
            {
                PrintOk("Upgrade Complete", result.DisplayMessage);
                return true;
            }
            PrintErr("Upgrade Failed", result.DisplayMessage);
            return false;
        }

        private static async Task HandleForgotPasswordAsync()
        {
            PrintSectionHeader("PASSWORD RECOVERY", "Request a reset link to your registered email");
            string email = Ask("Registered Email: ");
            var result = await NightAuthApp.ForgotPasswordAsync(email);
            if (result.Success)
            {
                PrintOk("Recovery Dispatched", "If an account matches, a reset link was sent to your email.");
            }
            else
            {
                PrintErr("Request Failed", result.DisplayMessage);
            }
        }

        private static async Task HandleResetPasswordAsync()
        {
            PrintSectionHeader("COMPLETE PASSWORD RESET", "Confirm recovery token and set a new password");
            string token = Ask("Reset Token: ");
            string newPass = AskPassword("New Password: ");
            var result = await NightAuthApp.ResetPasswordAsync(token, newPass);
            if (result.Success)
            {
                PrintOk("Password Updated", "Your password has been reset successfully.");
            }
            else
            {
                PrintErr("Reset Failed", result.DisplayMessage);
            }
        }

        private static async Task HandleResetHwidAsync()
        {
            PrintSectionHeader("RESET HARDWARE FINGERPRINT (HWID)", "Unbind previous device to authorize a new machine");
            string user = Ask("Username: ");
            string pass = AskPassword("Password: ");
            string totp = Ask($"2FA Code {C_DIM}(Optional, Enter to skip){C_TEXT}: ");
            var result = await NightAuthApp.ResetHwidAsync(user, pass, string.IsNullOrWhiteSpace(totp) ? null : totp);
            if (result.Success)
            {
                PrintOk("HWID Reset", result.DisplayMessage);
            }
            else
            {
                PrintErr("HWID Reset Failed", result.DisplayMessage);
            }
        }

        private static async Task HandleCheckBlacklistAsync()
        {
            PrintSectionHeader("HARDWARE BLACKLIST CHECK", "Verify if a machine fingerprint is restricted");
            Console.WriteLine($"  {C_MUTED}Querying device blacklist status with NightAuth Cloud...{C_RESET}");
            var result = await NightAuthApp.CheckBlacklistAsync();
            if (result.Success)
            {
                bool blacklisted = result.Payload["blacklisted"]?.GetValue<bool>() ?? false;
                if (blacklisted)
                {
                    PrintErr("Blacklisted", result.Payload["reason"]?.ToString() ?? "This hardware ID is restricted.");
                }
                else
                {
                    PrintOk("Clean Fingerprint", "Hardware ID is not on the blacklist.");
                }
            }
            else
            {
                PrintErr("Blacklist Query Failed", result.DisplayMessage);
            }
        }

        private static async Task HandleVerifyHashAsync()
        {
            PrintSectionHeader("BINARY INTEGRITY CHECK", "Validate SHA-256 checksum against server registry");
            string hash = Ask("SHA-256 File Hash: ");
            var result = await NightAuthApp.VerifyFileHashAsync(hash);
            if (result.Success)
            {
                PrintOk("Integrity Verified", "Checksum matches authorized server release.");
            }
            else
            {
                PrintErr("Verification Failed", result.DisplayMessage);
            }
        }

        #endregion

        #region Workspace Features Menu

        private static async Task RunApplicationFeaturesMenuAsync()
        {
            while (true)
            {
                RenderWorkspaceMenu();
                string choice = Ask("Action [1-21]: ");
                if (!int.TryParse(choice, out int num)) continue;

                switch (num)
                {
                    case 1:
                    {
                        string gKey = Ask("Global Variable Key: ");
                        var res = await NightAuthApp.GetGlobalVarAsync(gKey);
                        string val = res.Success ? res.Payload["value"]?.GetValue<string>() ?? "" : "";
                        if (res.Success && !string.IsNullOrEmpty(val)) PrintOk(gKey, val);
                        else PrintErr("Fetch Failed", res.DisplayMessage);
                        break;
                    }
                    case 2:
                    {
                        string uKey = Ask("User Variable Key: ");
                        var res = await NightAuthApp.GetUserVarAsync(uKey);
                        string val = res.Success ? res.Payload["variable"]?["value"]?.GetValue<string>() ?? res.Payload["value"]?.GetValue<string>() ?? "" : "";
                        if (res.Success && !string.IsNullOrEmpty(val)) PrintOk(uKey, val);
                        else PrintErr("Fetch Failed", res.DisplayMessage);
                        break;
                    }
                    case 3:
                    {
                        string sKey = Ask("User Variable Key: ");
                        string sVal = Ask("Value to Store: ");
                        var res = await NightAuthApp.SetUserVarAsync(sKey, sVal);
                        if (res.Success) PrintOk("Variable Saved", sKey);
                        else PrintErr("Save Failed", res.DisplayMessage);
                        break;
                    }
                    case 4:
                    {
                        var res = await NightAuthApp.GetUserVariablesAsync();
                        PrintSectionHeader("USER CLOUD VARIABLES", "Per-user encrypted key-value pairs");
                        if (res.Success && res.Payload["variables"] is JsonArray arr && arr.Count > 0)
                        {
                            foreach (var item in arr)
                            {
                                string k = item?["name"]?.ToString() ?? item?["key"]?.ToString() ?? "";
                                string v = item?["value"]?.ToString() ?? item?["data"]?.ToString() ?? "";
                                Console.WriteLine($"  {C_BLUE}•{C_RESET} {C_CYAN}{C_BOLD}{k,-22}{C_RESET} = {C_WHITE}{v}{C_RESET}");
                            }
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine($"  {C_DIM}No user variables stored for this account yet.{C_RESET}\n");
                        }
                        break;
                    }
                    case 5:
                    {
                        await HandleLiveChatAsync();
                        break;
                    }
                    case 6:
                    {
                        await RenderStatsTableAsync();
                        break;
                    }
                    case 7:
                    {
                        var res = await NightAuthApp.GetOnlineUsersAsync();
                        int onlineCount = res.Success ? res.Payload["online"]?.GetValue<int>() ?? 0 : 0;
                        PrintSectionHeader("ONLINE ACTIVE SESSIONS", $"{onlineCount} active session(s)");
                        if (res.Success && res.Payload["users"] is JsonArray arr)
                        {
                            foreach (var u in arr)
                            {
                                Console.WriteLine($"  {C_GREEN}●{C_RESET} {C_WHITE}{u?.ToString()}{C_RESET}");
                            }
                        }
                        Console.WriteLine();
                        break;
                    }
                    case 8:
                    {
                        string newU = Ask("New Username: ");
                        string p1 = AskPassword("Confirm Password: ");
                        var res = await NightAuthApp.ChangeUsernameAsync(newU, p1);
                        if (res.Success)
                        {
                            PrintOk("Username Updated", newU);
                            Console.WriteLine($"  {C_AMBER}For security, your session has been closed. Please sign in with your new username.{C_RESET}\n");
                            return;
                        }
                        PrintErr("Update Failed", res.DisplayMessage);
                        break;
                    }
                    case 9:
                    {
                        if (await HandleUpgradeAsync(_user.Username))
                        {
                            var check = await NightAuthApp.CheckAsync();
                            if (check.Success) _user = check.ToUserData();
                            RenderUserProfile();
                        }
                        break;
                    }
                    case 10:
                    {
                        await HandleSetup2FAAsync();
                        break;
                    }
                    case 11:
                    {
                        string p3 = AskPassword("Account Password: ");
                        string code3 = Ask("6-digit Authenticator Code: ");
                        var res = await NightAuthApp.Disable2FAAsync(p3, code3);
                        if (res.Success) PrintOk("2FA Disabled", "Two-factor protection removed.");
                        else PrintErr("Disable Failed", res.DisplayMessage);
                        break;
                    }
                    case 12:
                    {
                        string fileId = Ask("Enter File ID or Asset Name: ");
                        var res = await NightAuthApp.GetFileAsync(fileId);
                        if (res.Success)
                        {
                            string b64 = res.Payload["data"]?.ToString() ?? "";
                            byte[] raw = Convert.FromBase64String(b64);
                            string savedAs = $"downloaded_{fileId}.bin";
                            File.WriteAllBytes(savedAs, raw);
                            PrintOk("Download Complete", $"Saved '{savedAs}' ({raw.Length} bytes) to disk.");
                        }
                        else
                        {
                            PrintErr("Download Failed", res.DisplayMessage);
                        }
                        break;
                    }
                    case 13:
                    {
                        string hookId = Ask("Webhook ID: ");
                        string msg = Ask("JSON / Message Payload: ");
                        JsonObject? obj = null;
                        if (!string.IsNullOrEmpty(msg))
                        {
                            try { obj = JsonNode.Parse(msg) as JsonObject; } catch { }
                        }
                        var res = await NightAuthApp.TriggerWebhookAsync(hookId, obj ?? new JsonObject());
                        if (res.Success) PrintOk("Webhook Dispatched", $"HTTP {res.StatusCode} OK");
                        else PrintErr("Webhook Failed", res.DisplayMessage);
                        break;
                    }
                    case 14:
                    {
                        string logMsg = Ask("Audit Log Message: ");
                        var res = await NightAuthApp.LogAsync(logMsg);
                        if (res.Success) PrintOk("Audit Log Recorded", "Delivered to application telemetry feed.");
                        else PrintErr("Log Failed", res.DisplayMessage);
                        break;
                    }
                    case 15:
                    {
                        var res = await NightAuthApp.CheckAsync();
                        if (res.Success) PrintOk("Session Verified", "Active and cryptographically valid.");
                        else PrintErr("Session Invalid", res.DisplayMessage);
                        break;
                    }
                    case 16:
                    {
                        var res = await NightAuthApp.HeartbeatAsync();
                        if (res.Success) PrintOk("Heartbeat Acknowledged", res.DisplayMessage);
                        else PrintErr("Heartbeat Failed", res.DisplayMessage);
                        break;
                    }
                    case 17:
                    {
                        string reason = Ask($"Ban Reason {C_DIM}(Optional){C_TEXT}: ");
                        var res = await NightAuthApp.BanAsync(string.IsNullOrWhiteSpace(reason) ? null : reason);
                        if (res.Success)
                        {
                            PrintOk("Killswitch Executed", res.DisplayMessage);
                            return;
                        }
                        PrintErr("Ban Report Failed", res.DisplayMessage);
                        break;
                    }
                    case 18:
                    {
                        PrintSectionHeader("REAL-TIME APPLICATION EVENTS (SSE)", "Press Enter at any time to stop listening");
                        using var cts = new CancellationTokenSource();
                        var listenTask = Task.Run(async () =>
                        {
                            try
                            {
                                await foreach (var evt in NightAuthApp.ReadEventsAsync(null, cts.Token))
                                {
                                    Console.WriteLine($"  {C_BADGE_BG}{C_GLOW} EVENT {C_RESET} {C_CYAN}{evt}{C_RESET}");
                                }
                            }
                            catch { }
                        });
                        Console.ReadLine();
                        cts.Cancel();
                        try { await listenTask; } catch { }
                        PrintOk("Stream Closed", "Disconnected from real-time event feed.");
                        break;
                    }
                    case 19:
                    {
                        string room = Ask("Channel name (e.g. general): ");
                        if (string.IsNullOrEmpty(room)) room = "general";
                        var res = await NightAuthApp.GetChatMessagesAsync(room);
                        if (res.Success && res.Payload["messages"] is JsonArray arr)
                        {
                            PrintSectionHeader(room, $"{arr.Count} message(s) retrieved");
                            foreach (var msg in arr)
                            {
                                string author = msg?["author"]?.ToString() ?? "unknown";
                                string content = msg?["message"]?.ToString() ?? msg?["content"]?.ToString() ?? "";
                                string ts = msg?["timestamp"]?.ToString() ?? "";
                                Console.WriteLine($"  {C_BORDER}│{C_RESET} {C_DIM}[{ts}]{C_RESET} {C_CYAN}{C_BOLD}{author}{C_RESET}: {C_TEXT}{content}{C_RESET}");
                            }
                            Console.WriteLine();
                        }
                        else
                        {
                            PrintErr("History Query Failed", res.DisplayMessage);
                        }
                        break;
                    }
                    case 20:
                    {
                        PrintSectionHeader("SESSION REVOCATION & INVALIDATION VERIFICATION", "3-Step Live Security Audit");
                        Console.WriteLine($"  {C_MUTED}1. Verifying active session before revocation...{C_RESET}");
                        var step1 = await NightAuthApp.CheckAsync();
                        if (step1.Success) PrintOk("Step 1 Passed", "Session is active and authenticated on server.");
                        else { PrintErr("Step 1 Failed", step1.DisplayMessage); break; }

                        Console.WriteLine($"  {C_MUTED}2. Revoking and terminating session on server (/logout)...{C_RESET}");
                        var step2 = await NightAuthApp.LogoutAsync();
                        if (step2.Success) PrintOk("Step 2 Passed", "Server confirmed session revocation.");
                        else PrintErr("Step 2 Failed", step2.DisplayMessage);

                        Console.WriteLine($"  {C_MUTED}3. Verifying that subsequent session check is rejected...{C_RESET}");
                        var step3 = await NightAuthApp.CheckAsync();
                        if (step3.Success) PrintErr("Step 3 Unexpected", "Session was still accepted after revocation.");
                        else PrintOk("Step 3 Passed", "Server rejected revoked session as expected.");
                        return;
                    }
                    case 21:
                    {
                        await NightAuthApp.LogoutAsync();
                        PrintOk("Session Closed", "Successfully signed out.");
                        return;
                    }
                }
            }
        }

        private static async Task HandleLiveChatAsync()
        {
            string room = Ask("Channel name (e.g. general): ");
            if (string.IsNullOrEmpty(room)) room = "general";

            PrintSectionHeader("LIVE CHANNEL CHAT", "Type /exit at any time to leave the channel");
            var msgsRes = await NightAuthApp.GetChatMessagesAsync(room);
            Console.WriteLine($"\n  {C_BORDER}╭─ {C_GLOW}{C_BOLD}#{room,-20}{C_BORDER} ──────────────────────────────────────────────────╮{C_RESET}");
            if (msgsRes.Success && msgsRes.Payload["messages"] is JsonArray arr && arr.Count > 0)
            {
                foreach (var msg in arr)
                {
                    string author = msg?["author"]?.ToString() ?? "unknown";
                    string content = msg?["message"]?.ToString() ?? msg?["content"]?.ToString() ?? "";
                    string ts = msg?["timestamp"]?.ToString() ?? "";
                    Console.WriteLine($"  {C_BORDER}│{C_RESET} {C_DIM}[{ts}]{C_RESET} {C_CYAN}{C_BOLD}{author}{C_RESET}: {C_TEXT}{content}{C_RESET}");
                }
            }
            else
            {
                Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}No messages in this channel yet. Start the conversation below.{C_RESET}          {C_BORDER}│{C_RESET}");
            }
            Console.WriteLine($"  {C_BORDER}╰──────────────────────────────────────────────────────────────────────────╯{C_RESET}\n");

            while (true)
            {
                string input = Ask($"{C_CYAN}#{room}{C_RESET} message: ");
                if (string.Equals(input, "/exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  {C_DIM}Left channel #{room}.{C_RESET}\n");
                    return;
                }
                if (string.IsNullOrEmpty(input)) continue;

                var sendRes = await NightAuthApp.SendChatMessageAsync(input, room);
                if (!sendRes.Success)
                {
                    PrintErr("Send Failed", sendRes.DisplayMessage);
                }
            }
        }

        private static async Task RenderStatsTableAsync()
        {
            var statsResult = await NightAuthApp.GetStatsAsync();
            var onlineResult = await NightAuthApp.GetOnlineUsersAsync();

            int totalUsers = statsResult.Success
                ? (statsResult.Payload["users"]?["total"]?.GetValue<int>() ?? statsResult.Payload["totalUsers"]?.GetValue<int>() ?? 0) : 0;
            int totalLicenses = statsResult.Success
                ? (statsResult.Payload["licenses"]?["total"]?.GetValue<int>() ?? statsResult.Payload["totalLicenses"]?.GetValue<int>() ?? statsResult.Payload["totalKeys"]?.GetValue<int>() ?? 0) : 0;
            int activeUsers = statsResult.Success
                ? (statsResult.Payload["users"]?["active"]?.GetValue<int>() ?? 0) : 0;
            int bannedUsers = statsResult.Success
                ? (statsResult.Payload["users"]?["banned"]?.GetValue<int>() ?? 0) : 0;
            int usedLicenses = statsResult.Success
                ? (statsResult.Payload["licenses"]?["used"]?.GetValue<int>() ?? 0) : 0;
            int unusedLicenses = statsResult.Success
                ? (statsResult.Payload["licenses"]?["notUsed"]?.GetValue<int>() ?? statsResult.Payload["licenses"]?.AsObject().Count ?? 0) : 0;

            Console.WriteLine();
            Console.WriteLine($"  {C_BORDER}╭─ {C_GLOW}{C_BOLD}APPLICATION LIVE TELEMETRY{C_BORDER} ─────────────────────────────────────────────╮{C_RESET}");
            Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}Total Users     :{C_RESET} {C_WHITE}{C_BOLD}{totalUsers,-14}{C_RESET}  {C_DIM}Total Licenses  :{C_RESET} {C_WHITE}{C_BOLD}{totalLicenses,-18}{C_RESET} {C_BORDER}│{C_RESET}");
            Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}Active Users    :{C_RESET} {C_GREEN}{C_BOLD}{activeUsers,-14}{C_RESET}  {C_DIM}Used Licenses   :{C_RESET} {C_CYAN}{C_BOLD}{usedLicenses,-18}{C_RESET} {C_BORDER}│{C_RESET}");
            Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}Banned Users    :{C_RESET} {C_RED}{C_BOLD}{bannedUsers,-14}{C_RESET}  {C_DIM}Unused Licenses :{C_RESET} {C_AMBER}{C_BOLD}{unusedLicenses,-18}{C_RESET} {C_BORDER}│{C_RESET}");
            Console.WriteLine($"  {C_BORDER}│{C_RESET}  {C_DIM}Target Version  :{C_RESET} {C_GLOW}{NightAuthApp.Version,-52}{C_RESET} {C_BORDER}│{C_RESET}");
            Console.WriteLine($"  {C_BORDER}╰──────────────────────────────────────────────────────────────────────────╯{C_RESET}\n");
        }

        private static async Task HandleSetup2FAAsync()
        {
            PrintSectionHeader("TWO-FACTOR AUTHENTICATION (TOTP)", "Enroll an authenticator app (RFC 6238)");
            var setup = await NightAuthApp.Setup2FAAsync();
            if (setup.Success)
            {
                string secret = setup.Payload["secret"]?.GetValue<string>() ?? "";
                string uri = setup.Payload["uri"]?.GetValue<string>() ?? "";
                PrintInfo("TOTP Secret Key:", secret);
                PrintInfo("Provisioning URI:", uri);

                string code = Ask("Enter 6-digit Authenticator code: ");
                var conf = await NightAuthApp.Confirm2FAAsync(code);
                if (conf.Success)
                {
                    PrintOk("2FA Activated", "Store these recovery backup codes safely:");
                    if (conf.Payload["backupCodes"] is JsonArray arr)
                    {
                        foreach (var b in arr)
                        {
                            Console.WriteLine($"    {C_AMBER}• {b?.ToString()}{C_RESET}");
                        }
                    }
                    Console.WriteLine();
                }
                else
                {
                    PrintErr("Verification Failed", conf.DisplayMessage);
                }
            }
            else
            {
                PrintErr("2FA Setup Failed", setup.DisplayMessage);
            }
        }

        #endregion

        #region Background Threads

        private static void StartRealtimeEventListener()
        {
            _sessionCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var node in NightAuthApp.ReadEventsAsync(null, _sessionCts.Token))
                    {
                        string type = node["type"]?.GetValue<string>() ?? "";
                        if (type == "session-terminated" || type == "session_terminated")
                        {
                            Console.WriteLine($"\n  {C_RED_BG}{C_RED}{C_BOLD} SECURITY ALERT {C_RESET} {C_WHITE}Session was terminated by administrator.{C_RESET}");
                            Environment.Exit(0);
                        }
                    }
                }
                catch { }
            });
        }

        private static void StartHeartbeatThread()
        {
            _ = Task.Run(async () =>
            {
                while (_sessionCts is { IsCancellationRequested: false })
                {
                    await Task.Delay(TimeSpan.FromSeconds(45), _sessionCts.Token);
                    try { await NightAuthApp.HeartbeatAsync(); } catch { }
                }
            });
        }

        #endregion
    }
}
