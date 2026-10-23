using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NightAuth;

namespace NightAuthWpf
{
    public sealed class SubscriptionViewItem
    {
        public string Subscription { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string ExpiryFormatted { get; set; } = string.Empty;
        public string TimeRemainingFormatted { get; set; } = string.Empty;
    }

    public sealed class UserVariableItem
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public partial class DashboardWindow : Window
    {
        private readonly NightAuthClient _client;
        private readonly string _appName;
        private UserData _user;
        private string? _currentChatRoom;
        private readonly ObservableCollection<SubscriptionViewItem> _subscriptions = new();
        private readonly ObservableCollection<UserVariableItem> _userVariables = new();
        private readonly CancellationTokenSource _eventsCts = new();
        private volatile bool _isLoggingOut;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public DashboardWindow(api app, string appName, UserData user) : this(app.Client, appName, user) { }

        public DashboardWindow(NightAuthClient client, string appName, UserData user)
        {
            InitializeComponent();
            _client = client;
            _appName = appName;
            _user = user;
            ListSubscriptions.ItemsSource = _subscriptions;
            ListDedicatedSubscriptions.ItemsSource = _subscriptions;
            ListUserVariables.ItemsSource = _userVariables;
            StartRealtimeEventListener();
            LblWindowAppName.Text = _appName;
            NavOverview.IsChecked = true;
            Load2faState();
            Closed += (_, _) => _eventsCts.Cancel();
            Loaded += (_, _) =>
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                Topmost = true;
                Activate();
                SetForegroundWindow(hwnd);
                Topmost = false;

                if (string.IsNullOrEmpty(TxtChatRoom.Text)) TxtChatRoom.Text = "general";
            };
        }

        private void StartRealtimeEventListener()
        {
            var token = _eventsCts.Token;
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await foreach (var evt in _client.ReadEventsAsync(token))
                        {
                            HandleRealtimeEvent(evt);
                            if (token.IsCancellationRequested) break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        if (token.IsCancellationRequested) break;
                        try { await Task.Delay(3000, token); } catch (OperationCanceledException) { break; }
                    }
                }
            }, token);
        }

        private void HandleRealtimeEvent(JsonObject node)
        {
            if (_isLoggingOut) return;
            string type = node["type"]?.GetValue<string>() ?? "";

            switch (type)
            {
                case "session-invalid":
                    if (_isLoggingOut) return;
                    Dispatcher.Invoke(() =>
                    {
                        if (_isLoggingOut) return;
                        MessageBox.Show("Your session has been terminated by administrator or system.", "Session Terminated", MessageBoxButton.OK, MessageBoxImage.Stop);
                        Application.Current.Shutdown();
                    });
                    break;

                case "app-banned":
                    string reason = node["reason"]?.GetValue<string>() ?? "Application has been banned.";
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Application banned: {reason}", "Security Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    });
                    break;

                case "app-paused":
                case "app-disabled":
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Application status changed to: {type}", "Application Status Changed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Application.Current.Shutdown();
                    });
                    break;

                case "chat-message":
                    string room = node["room"]?.GetValue<string>() ?? "general";
                    string author = node["author"]?.GetValue<string>() ?? "unknown";
                    string content = node["content"]?.GetValue<string>() ?? "";
                    string timestamp = node["createdAt"]?.GetValue<string>() ?? DateTime.UtcNow.ToString("o");
                    Dispatcher.Invoke(() =>
                    {
                        if (room == _currentChatRoom)
                            AddChatMessageBubble(author, content, timestamp);
                    });
                    break;
            }
        }

        #region Window Chrome

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void RootBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RootBorder.Clip = new RectangleGeometry(new Rect(0, 0, e.NewSize.Width, e.NewSize.Height), 16, 16);
        }

        #endregion

        #region Navigation

        private void NavOverview_Checked(object sender, RoutedEventArgs e)
        {
            ShowPanel(PanelOverview);
            RefreshProfileAndEntitlements();
            _ = LoadStatsAsync();
        }
        private void NavSubscriptions_Checked(object sender, RoutedEventArgs e)
        {
            ShowPanel(PanelSubscriptions);
            RefreshProfileAndEntitlements();
        }
        private void NavVariables_Checked(object sender, RoutedEventArgs e) => ShowPanel(PanelVariables);
        private void NavChat_Checked(object sender, RoutedEventArgs e) => ShowPanel(PanelChat);
        private void NavStats_Checked(object sender, RoutedEventArgs e)
        {
            ShowPanel(PanelStats);
            _ = RefreshStatsPanelAsync();
        }
        private void NavSecurity_Checked(object sender, RoutedEventArgs e) => ShowPanel(PanelSecurity);
        private void NavFilesHooks_Checked(object sender, RoutedEventArgs e) => ShowPanel(PanelFilesHooks);

        private void ShowPanel(FrameworkElement target)
        {
            PanelOverview.Visibility = Visibility.Collapsed;
            PanelSubscriptions.Visibility = Visibility.Collapsed;
            PanelVariables.Visibility = Visibility.Collapsed;
            PanelChat.Visibility = Visibility.Collapsed;
            PanelStats.Visibility = Visibility.Collapsed;
            PanelSecurity.Visibility = Visibility.Collapsed;
            PanelFilesHooks.Visibility = Visibility.Collapsed;
            target.Visibility = Visibility.Visible;
        }

        #endregion

        #region Overview

        private static string FormatTimestamp(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "0") return "—";
            if (long.TryParse(raw, out long unix) && unix > 0)
            {
                if (unix > 100_000_000_000L) unix /= 1000;
                var dt = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
                return dt.ToString("yyyy-MM-dd HH:mm 'UTC'");
            }
            if (DateTimeOffset.TryParse(raw, out var parsed))
            {
                return parsed.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'");
            }
            return raw;
        }

        private static string FormatTimeRemaining(long totalSeconds)
        {
            if (totalSeconds <= 0) return "Expired";

            var ts = TimeSpan.FromSeconds(totalSeconds);
            if (ts.TotalDays >= 1)
            {
                int days = (int)ts.TotalDays;
                int hours = ts.Hours;
                return hours > 0 ? $"{days}d {hours}h ({totalSeconds}s)" : $"{days}d ({totalSeconds}s)";
            }
            if (ts.TotalHours >= 1)
            {
                int hours = (int)ts.TotalHours;
                int minutes = ts.Minutes;
                return minutes > 0 ? $"{hours}h {minutes}m ({totalSeconds}s)" : $"{hours}h ({totalSeconds}s)";
            }
            if (ts.TotalMinutes >= 1)
            {
                int minutes = (int)ts.TotalMinutes;
                int seconds = ts.Seconds;
                return seconds > 0 ? $"{minutes}m {seconds}s ({totalSeconds}s)" : $"{minutes}m ({totalSeconds}s)";
            }
            return $"{totalSeconds}s";
        }

        private void RefreshProfileAndEntitlements()
        {
            LblDashboardUsername.Text = string.IsNullOrEmpty(_user.Username) ? "Licensed User" : _user.Username;
            LblDashboardEmail.Text = string.IsNullOrEmpty(_user.Email) ? "Not linked" : _user.Email;
            LblDashboardIp.Text = string.IsNullOrEmpty(_user.Ip) ? "—" : _user.Ip;
            LblDashboardCreated.Text = FormatTimestamp(_user.CreatedAt);
            LblDashboardLastLogin.Text = FormatTimestamp(_user.LastLoginAt);

            _subscriptions.Clear();
            foreach (var s in _user.Subscriptions)
            {
                _subscriptions.Add(new SubscriptionViewItem
                {
                    Subscription = s.Subscription,
                    Key = string.IsNullOrEmpty(s.Key) ? "Active Account" : s.Key,
                    ExpiryFormatted = $"Expires: {FormatTimestamp(s.Expiry)} UTC",
                    TimeRemainingFormatted = FormatTimeRemaining(s.TimeLeft)
                });
            }

            NoSubscriptionsCard.Visibility = _subscriptions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ListSubscriptions.Visibility = _subscriptions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task LoadStatsAsync()
        {
            var statsResult = await _client.GetStatsAsync();
            var onlineResult = await _client.GetOnlineUsersAsync();

            int totalUsers = statsResult.Success
                ? (statsResult.Payload["users"]?["total"]?.GetValue<int>() ?? statsResult.Payload["totalUsers"]?.GetValue<int>() ?? 0)
                : 0;
            int totalLicenses = statsResult.Success
                ? (statsResult.Payload["licenses"]?["total"]?.GetValue<int>() ?? statsResult.Payload["totalLicenses"]?.GetValue<int>() ?? statsResult.Payload["totalKeys"]?.GetValue<int>() ?? 0)
                : 0;
            int onlineCount = onlineResult.Success
                ? (onlineResult.Payload["online"]?.GetValue<int>() ?? (onlineResult.Payload["users"] is JsonArray arr ? arr.Count : 0))
                : 0;

            Dispatcher.Invoke(() =>
            {
                if (statsResult.Success)
                {
                    TxtStatUsers.Text = totalUsers.ToString();
                    TxtStatLicenses.Text = totalLicenses.ToString();
                }
                else if (statsResult.Error == "feature_disabled")
                {
                    TxtStatUsers.Text = "N/A";
                    TxtStatLicenses.Text = "N/A";
                }

                if (onlineResult.Success)
                {
                    TxtStatOnline.Text = onlineCount.ToString();
                }
                else if (onlineResult.Error == "feature_disabled")
                {
                    TxtStatOnline.Text = "N/A";
                }
            });
        }

        private async void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoggingOut) return;
            _isLoggingOut = true;

            try
            {
                _eventsCts.Cancel();
            }
            catch { }

            try
            {
                await _client.LogoutAsync();
            }
            catch { }

            new AuthWindow().Show();
            Close();
        }

        #endregion

        #region Variables

        private async void BtnFetchVar_Click(object sender, RoutedEventArgs e)
        {
            string key = TxtVarKey.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;

            UpdateStatus("Fetching global variable...", "#3b82f6");

            var result = await _client.GetGlobalVarAsync(key);
            string val = result.Success ? result.Payload["value"]?.GetValue<string>() ?? "" : "";

            TxtVarValue.Text = string.IsNullOrEmpty(val) ? $"(not found: {result.DisplayMessage})" : val;
            UpdateStatus(string.IsNullOrEmpty(val) ? "Variable not found." : "Global variable fetched.", string.IsNullOrEmpty(val) ? "#f43f5e" : "#10b981");
        }

        private async void BtnGetUserVar_Click(object sender, RoutedEventArgs e)
        {
            string key = TxtSetVarKey.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;

            UpdateStatus("Fetching user variable...", "#3b82f6");

            var result = await _client.GetUserVarAsync(key);
            string val = result.Success ? result.Payload["variable"]?["value"]?.GetValue<string>() ?? "" : "";

            if (!string.IsNullOrEmpty(val))
            {
                TxtSetVarValue.Text = val;
                UpdateStatus("User variable loaded.", "#10b981");
            }
            else
            {
                UpdateStatus($"User variable '{key}' not found.", "#f43f5e");
            }
        }

        private async void BtnSetVar_Click(object sender, RoutedEventArgs e)
        {
            string key = TxtSetVarKey.Text.Trim();
            string val = TxtSetVarValue.Text;
            if (string.IsNullOrEmpty(key)) return;

            var result = await _client.SetUserVarAsync(key, val);
            UpdateStatus(result.Success ? "User variable saved successfully." : $"Failed: {result.DisplayMessage}", result.Success ? "#10b981" : "#f43f5e");
        }

        #endregion

        #region Chat Rooms

        private void AddChatMessageBubble(string author, string message, string timestamp)
        {
            ChatEmptyPlaceholder.Visibility = Visibility.Collapsed;

            bool isMe = string.Equals(author, _user.Username, StringComparison.OrdinalIgnoreCase);

            var bubbleBorder = new Border
            {
                Background = isMe
                    ? new SolidColorBrush(Color.FromArgb(0x35, 0x3b, 0x82, 0xf6))
                    : new SolidColorBrush(Color.FromArgb(0x22, 0xff, 0xff, 0xff)),
                BorderBrush = isMe
                    ? new SolidColorBrush(Color.FromArgb(0x55, 0x3b, 0x82, 0xf6))
                    : new SolidColorBrush(Color.FromArgb(0x18, 0xff, 0xff, 0xff)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8),
                MaxWidth = 550,
                HorizontalAlignment = isMe ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var stack = new StackPanel();

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };

            var authorText = new TextBlock
            {
                Text = isMe ? "You" : author,
                FontWeight = FontWeights.Bold,
                FontSize = 11.5,
                Foreground = isMe
                    ? (SolidColorBrush)FindResource("AccentBlueBrush")
                    : (SolidColorBrush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 8, 0)
            };

            var timeText = new TextBlock
            {
                Text = FormatTimestamp(timestamp),
                FontSize = 10,
                Foreground = (SolidColorBrush)FindResource("TextMutedBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };

            headerPanel.Children.Add(authorText);
            headerPanel.Children.Add(timeText);

            var contentText = new TextBlock
            {
                Text = message,
                FontSize = 12.5,
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
                TextWrapping = TextWrapping.Wrap
            };

            stack.Children.Add(headerPanel);
            stack.Children.Add(contentText);
            bubbleBorder.Child = stack;

            ChatMessagesContainer.Children.Add(bubbleBorder);
            ScrollChatMessages.ScrollToEnd();
        }

        private async void BtnJoinRoom_Click(object sender, RoutedEventArgs e)
        {
            string room = TxtChatRoom.Text.Trim();
            if (string.IsNullOrEmpty(room)) return;

            _currentChatRoom = room;
            LblCurrentRoom.Text = $"#{room}";
            LblCurrentRoom.Foreground = (SolidColorBrush)FindResource("AccentBlueBrush");
            DotRoomStatus.Fill = (SolidColorBrush)FindResource("AccentBlueBrush");
            BadgeRoomStatus.Background = new SolidColorBrush(Color.FromArgb(0x28, 0x3b, 0x82, 0xf6));
            BadgeRoomStatus.BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0x3b, 0x82, 0xf6));

            ChatMessagesContainer.Children.Clear();
            ChatEmptyPlaceholder.Visibility = Visibility.Visible;
            ChatMessagesContainer.Children.Add(ChatEmptyPlaceholder);

            UpdateStatus($"Connecting to #{room}...", "#3b82f6");

            var result = await _client.GetChatMessagesAsync(room);
            if (result.Success && result.Payload["messages"] is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item == null) continue;
                    string author = item["author"]?.ToString() ?? "";
                    string content = item["content"]?.ToString() ?? item["message"]?.ToString() ?? "";
                    string timestamp = item["timestamp"]?.ToString() ?? item["createdAt"]?.ToString() ?? "";
                    AddChatMessageBubble(author, content, timestamp);
                }
            }
            UpdateStatus($"Connected to #{room}", "#10b981");
        }

        private void TxtChatMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnSendChat_Click(sender, e);
        }

        private async void BtnSendChat_Click(object sender, RoutedEventArgs e)
        {
            string msg = TxtChatMessage.Text.Trim();
            if (string.IsNullOrEmpty(_currentChatRoom))
            {
                UpdateStatus("Connect to a room first.", "#f43f5e");
                return;
            }
            if (string.IsNullOrEmpty(msg)) return;

            TxtChatMessage.Text = "";
            var result = await _client.SendChatMessageAsync(msg, _currentChatRoom!);
            if (!result.Success) UpdateStatus($"Message failed: {result.DisplayMessage}", "#f43f5e");
        }

        #endregion

        #region Security — Change Username / Upgrade

        private async void BtnChangeUsername_Click(object sender, RoutedEventArgs e)
        {
            string newUsername = TxtNewUsername.Text.Trim();
            string password = TxtChangeUsernamePassword.Password;
            if (string.IsNullOrEmpty(newUsername) || string.IsNullOrEmpty(password)) return;

            var result = await _client.ChangeUsernameAsync(newUsername, password);

            if (result.Success)
            {
                MessageBox.Show(
                    "Username changed. For security, this ends your current session — please sign in again with your new username.",
                    "Username Changed", MessageBoxButton.OK, MessageBoxImage.Information);
                BtnLogout_Click(sender, e);
            }
            else
            {
                UpdateStatus($"Failed: {result.DisplayMessage}", "#f43f5e");
            }
        }

        private async void BtnUpgrade_Click(object sender, RoutedEventArgs e)
        {
            string key = TxtUpgradeKey.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;

            var result = await _client.UpgradeAsync(_user.Username, key);

            if (result.Success)
            {
                TxtUpgradeKey.Text = "";
                var check = await _client.CheckAsync();
                if (check.Success) _user = check.ToUserData();
                RefreshProfileAndEntitlements();
                UpdateStatus(result.DisplayMessage, "#10b981");
            }
            else
            {
                UpdateStatus($"Upgrade failed: {result.DisplayMessage}", "#f43f5e");
            }
        }

        #endregion

        #region Security — 2FA

        private void Load2faState()
        {
            if (_user.TwoFactorEnabled)
            {
                Panel2faStart.Visibility = Visibility.Collapsed;
                Panel2faQr.Visibility = Visibility.Collapsed;
                Panel2faBackup.Visibility = Visibility.Collapsed;
                Panel2faDisable.Visibility = Visibility.Visible;
                SetTwoFactorEnabledBadge();
            }
            else
            {
                Panel2faStart.Visibility = Visibility.Visible;
                Panel2faQr.Visibility = Visibility.Collapsed;
                Panel2faBackup.Visibility = Visibility.Collapsed;
                Panel2faDisable.Visibility = Visibility.Collapsed;
                Lbl2faStatus.Text = "SET UP AVAILABLE";
                Badge2faStatus.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#71717a22")!;
                Badge2faStatus.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#71717a44")!;
                Lbl2faStatus.Foreground = (SolidColorBrush)FindResource("TextMutedBrush");
            }
        }

        private async void BtnSetup2fa_Click(object sender, RoutedEventArgs e)
        {
            var result = await _client.Setup2FAAsync();

            if (!result.Success)
            {
                if (result.Error == "2fa_already_enabled")
                {
                    Panel2faStart.Visibility = Visibility.Collapsed;
                    Panel2faDisable.Visibility = Visibility.Visible;
                    SetTwoFactorEnabledBadge();
                }
                else
                {
                    UpdateStatus($"2FA setup failed: {result.DisplayMessage}", "#f43f5e");
                }
                return;
            }

            Txt2faSecret.Text = result.Payload["secret"]?.GetValue<string>() ?? "";
            Img2faQr.Source = Base64ToBitmapImage(result.Payload["qr"]?.GetValue<string>() ?? "");
            LblCopy2faSecretText.Text = "Copy Key";
            Panel2faStart.Visibility = Visibility.Collapsed;
            Panel2faQr.Visibility = Visibility.Visible;
        }

        private void BtnCopy2faSecret_Click(object sender, RoutedEventArgs e)
        {
            Copy2faSecretToClipboard();
        }

        private void Txt2faSecret_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Copy2faSecretToClipboard();
        }

        private async void Copy2faSecretToClipboard()
        {
            string secret = Txt2faSecret.Text.Trim();
            if (string.IsNullOrEmpty(secret)) return;

            try
            {
                Clipboard.SetText(secret);
                LblCopy2faSecretText.Text = "Copied!";
                UpdateStatus("2FA secret key copied to clipboard.", "#10b981");
                await Task.Delay(2000);
                LblCopy2faSecretText.Text = "Copy Key";
            }
            catch
            {
                UpdateStatus("Failed to copy to clipboard.", "#f43f5e");
            }
        }

        private void BtnCancel2faSetup_Click(object sender, RoutedEventArgs e)
        {
            Panel2faQr.Visibility = Visibility.Collapsed;
            Panel2faStart.Visibility = Visibility.Visible;
            Txt2faConfirmCode.Text = "";
            UpdateStatus("2FA setup cancelled.", "#71717a");
        }

        private async void BtnConfirm2fa_Click(object sender, RoutedEventArgs e)
        {
            string code = Txt2faConfirmCode.Text.Trim();
            if (code.Length != 6) return;

            var result = await _client.Confirm2FAAsync(code);

            if (!result.Success)
            {
                UpdateStatus($"Confirmation failed: {result.DisplayMessage}", "#f43f5e");
                return;
            }

            var backupCodes = new System.Collections.Generic.List<string>();
            if (result.Payload["backupCodes"] is JsonArray arr)
                foreach (var b in arr)
                    if (b != null) backupCodes.Add(b.ToString());

            ListBackupCodes.ItemsSource = backupCodes;
            Panel2faQr.Visibility = Visibility.Collapsed;
            Panel2faBackup.Visibility = Visibility.Visible;
            _user.TwoFactorEnabled = true;
            SetTwoFactorEnabledBadge();
        }

        private void BtnFinish2faSetup_Click(object sender, RoutedEventArgs e)
        {
            Panel2faBackup.Visibility = Visibility.Collapsed;
            Panel2faDisable.Visibility = Visibility.Visible;
        }

        private async void BtnDisable2fa_Click(object sender, RoutedEventArgs e)
        {
            string password = Txt2faDisablePassword.Password;
            string code = Txt2faDisableCode.Text.Trim();
            if (string.IsNullOrEmpty(password) || code.Length != 6)
            {
                UpdateStatus("Password and a current 6-digit code are both required to disable 2FA.", "#f43f5e");
                return;
            }

            var result = await _client.Disable2FAAsync(password, code);

            if (result.Success)
            {
                _user.TwoFactorEnabled = false;
                Txt2faDisablePassword.Password = "";
                Txt2faDisableCode.Text = "";
                Panel2faDisable.Visibility = Visibility.Collapsed;
                Panel2faStart.Visibility = Visibility.Visible;
                Lbl2faStatus.Text = "SET UP AVAILABLE";
                Badge2faStatus.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#71717a22")!;
                Badge2faStatus.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#71717a44")!;
                Lbl2faStatus.Foreground = (SolidColorBrush)FindResource("TextMutedBrush");
                UpdateStatus("2FA disabled.", "#10b981");
            }
            else
            {
                UpdateStatus($"Failed: {result.DisplayMessage}", "#f43f5e");
            }
        }

        private void SetTwoFactorEnabledBadge()
        {
            Lbl2faStatus.Text = "ENABLED";
            Badge2faStatus.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#10b98122")!;
            Badge2faStatus.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#10b98144")!;
            Lbl2faStatus.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
        }

        private static BitmapImage Base64ToBitmapImage(string dataUrl)
        {
            string base64 = dataUrl.Contains(",") ? dataUrl[(dataUrl.IndexOf(',') + 1)..] : dataUrl;
            byte[] bytes = Convert.FromBase64String(base64);
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        #endregion

        #region Security — Audit Log

        private async void BtnSendLog_Click(object sender, RoutedEventArgs e)
        {
            string msg = TxtAuditMessage.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            var result = await _client.LogAsync(msg);
            if (result.Success)
            {
                TxtAuditMessage.Text = "";
                UpdateStatus("Audit log recorded on server dashboard.", "#10b981");
            }
            else
            {
                UpdateStatus($"Failed: {result.DisplayMessage}", "#f43f5e");
            }
        }

        #endregion

        #region Extended Features: User Variables, Stats, Security Tools, Files & Hooks

        private async void BtnFetchAllUserVars_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Fetching user cloud variables...", "#3b82f6");
            var result = await _client.GetUserVariablesAsync();
            if (!result.Success)
            {
                UpdateStatus($"Failed to fetch variables: {result.DisplayMessage}", "#f43f5e");
                return;
            }
            _userVariables.Clear();
            if (result.Payload["variables"] is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item is JsonObject obj)
                    {
                        string k = obj["key"]?.ToString() ?? "";
                        string v = obj["value"]?.ToString() ?? "";
                        _userVariables.Add(new UserVariableItem { Key = k, Value = v });
                    }
                }
            }
            UpdateStatus($"Loaded {_userVariables.Count} user variable(s).", "#10b981");
        }

        private void BtnRefreshSubscriptions_Click(object sender, RoutedEventArgs e)
        {
            RefreshProfileAndEntitlements();
            UpdateStatus("Subscriptions refreshed.", "#10b981");
        }

        private async void BtnRefreshStats_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStatsPanelAsync();
        }

        private async Task RefreshStatsPanelAsync()
        {
            UpdateStatus("Fetching telemetry & active sessions...", "#3b82f6");
            var stats = await _client.GetStatsAsync();
            var online = await _client.GetOnlineUsersAsync();

            if (stats.Success)
            {
                LblStatsTotalUsers.Text = stats.Payload["users"]?["total"]?.ToString() ?? stats.Payload["totalUsers"]?.ToString() ?? "—";
                LblStatsTotalKeys.Text = stats.Payload["licenses"]?["total"]?.ToString() ?? stats.Payload["totalLicenses"]?.ToString() ?? stats.Payload["totalKeys"]?.ToString() ?? "—";
                LblStatsAppVersion.Text = stats.Payload["version"]?.ToString() ?? stats.Payload["appVersion"]?.ToString() ?? "1.0";
            }
            else if (stats.Error == "feature_disabled")
            {
                LblStatsTotalUsers.Text = "Disabled";
                LblStatsTotalKeys.Text = "Disabled";
            }

            if (online.Success)
            {
                int onlineCount = online.Payload["online"]?.GetValue<int>()
                    ?? (online.Payload["users"] is JsonArray arr ? arr.Count : 0);
                LblStatsOnlineUsers.Text = onlineCount.ToString();

                ListOnlineSessions.Items.Clear();
                if (online.Payload["users"] is JsonArray users && users.Count > 0)
                {
                    foreach (var u in users)
                    {
                        string? name = u?.ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            name = name.Trim().Trim('"').TrimStart('•', ' ', '	');
                            ListOnlineSessions.Items.Add(name);
                        }
                    }
                }
            }
            else if (online.Error == "feature_disabled")
            {
                LblStatsOnlineUsers.Text = "Disabled";
            }

            UpdateStatus(stats.Success || online.Success ? "Statistics updated." : $"Telemetry failed: {stats.DisplayMessage}",
                stats.Success || online.Success ? "#10b981" : "#f43f5e");
        }

        private async void BtnCheckBlacklist_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Querying blacklist status...", "#3b82f6");
            var result = await _client.CheckBlacklistAsync();
            if (result.Success)
            {
                bool blacklisted = result.Payload["blacklisted"]?.GetValue<bool>() ?? false;
                if (blacklisted)
                {
                    string reason = result.Payload["reason"]?.ToString() ?? "Blacklisted";
                    LblBlacklistResult.Text = $"⚠ BLACKLISTED: {reason}";
                    LblBlacklistResult.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                }
                else
                {
                    LblBlacklistResult.Text = "✓ Device is clean. No active hardware blacklists detected.";
                    LblBlacklistResult.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
                }
            }
            else
            {
                LblBlacklistResult.Text = $"Blacklist check error: {result.DisplayMessage}";
                LblBlacklistResult.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
            }
        }

        private async void BtnVerifyHash_Click(object sender, RoutedEventArgs e)
        {
            string hash = TxtVerifyHash.Text.Trim();
            if (string.IsNullOrEmpty(hash))
            {
                LblVerifyHashResult.Text = "Please enter a SHA-256 hash.";
                LblVerifyHashResult.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                return;
            }
            UpdateStatus("Verifying file hash signature...", "#3b82f6");
            var result = await _client.VerifyFileHashAsync(hash);
            if (result.Success)
            {
                LblVerifyHashResult.Text = "✓ File integrity signature matches official server release.";
                LblVerifyHashResult.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
            }
            else
            {
                LblVerifyHashResult.Text = $"✗ File integrity signature mismatch: {result.DisplayMessage}";
                LblVerifyHashResult.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
            }
        }

        private async void BtnSelfBan_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to permanently trigger a killswitch / ban for this account and hardware?",
                "Confirm Emergency Killswitch",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            string reason = TxtBanReason.Text.Trim();
            var result = await _client.BanAsync(string.IsNullOrEmpty(reason) ? "Self-triggered emergency ban" : reason);
            if (result.Success)
            {
                MessageBox.Show("Killswitch triggered. This account has been banned.", "Account Banned", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            else
            {
                UpdateStatus($"Ban failed: {result.DisplayMessage}", "#f43f5e");
            }
        }

        private async void BtnDownloadFile_Click(object sender, RoutedEventArgs e)
        {
            string fileId = TxtDownloadFileId.Text.Trim();
            if (string.IsNullOrEmpty(fileId))
            {
                LblDownloadResult.Text = "Please enter a valid File ID.";
                LblDownloadResult.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                return;
            }
            UpdateStatus("Downloading protected cloud asset...", "#3b82f6");
            var result = await _client.GetFileAsync(fileId);
            if (result.Success)
            {
                string b64 = result.Payload["data"]?.ToString() ?? "";
                byte[] raw = Convert.FromBase64String(b64);
                LblDownloadResult.Text = $"✓ Downloaded {raw.Length:N0} bytes into memory (secure zero-disk).";
                LblDownloadResult.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
                UpdateStatus($"Download of file {fileId} complete.", "#10b981");
            }
            else
            {
                LblDownloadResult.Text = $"Download failed: {result.DisplayMessage}";
                LblDownloadResult.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                UpdateStatus("Download failed.", "#f43f5e");
            }
        }

        private async void BtnTriggerWebhook_Click(object sender, RoutedEventArgs e)
        {
            string hookId = TxtWebhookId.Text.Trim();
            if (string.IsNullOrEmpty(hookId))
            {
                LblWebhookResult.Text = "Please enter a valid Webhook ID.";
                LblWebhookResult.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                return;
            }
            JsonObject? payload = null;
            string raw = TxtWebhookPayload.Text.Trim();
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    payload = JsonNode.Parse(raw) as JsonObject;
                }
                catch
                {
                    LblWebhookResult.Text = "Payload must be a valid JSON object string.";
                    LblWebhookResult.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                    return;
                }
            }
            UpdateStatus("Dispatched webhook to cloud...", "#3b82f6");
            var result = await _client.TriggerWebhookAsync(hookId, payload);
            if (result.Success)
            {
                LblWebhookResult.Text = $"✓ Response HTTP {result.StatusCode}: {result.Payload["response"]?.ToString() ?? result.Message}";
                LblWebhookResult.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
                UpdateStatus("Webhook dispatched successfully.", "#10b981");
            }
            else
            {
                LblWebhookResult.Text = $"Webhook error: {result.DisplayMessage}";
                LblWebhookResult.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                UpdateStatus("Webhook dispatch failed.", "#f43f5e");
            }
        }

        #endregion

        private void UpdateStatus(string message, string hexColor)
        {
            Dispatcher.Invoke(() =>
            {
                LblStatus.Text = message;
                LblStatus.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(hexColor)!;
            });
        }
    }
}
