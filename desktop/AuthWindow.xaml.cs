using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NightAuth;


namespace NightAuthWpf
{

    public partial class AuthWindow : Window
    {

        public static api NightAuthApp = new api(
            name: "",
            apikey: "",
            version: "1.0",
            token: null
        );

        public static readonly string AppDisplayName = NightAuthApp.Name;

        private enum AuthMode { LoginCredentials, LoginLicense, Register, ForgotPassword, ResetPasswordToken, Upgrade, ResetHwid }
        private AuthMode _mode = AuthMode.LoginCredentials;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public AuthWindow()
        {
            InitializeComponent();
            Loaded += AuthWindow_Loaded;
            SetMode(AuthMode.LoginCredentials);

            TxtPassword.GotFocus += (s, e) => UpdatePasswordPlaceholder();
            TxtPassword.LostFocus += (s, e) => UpdatePasswordPlaceholder();
            TxtPasswordVisible.GotFocus += (s, e) => UpdatePasswordPlaceholder();
            TxtPasswordVisible.LostFocus += (s, e) => UpdatePasswordPlaceholder();
        }

        private async void AuthWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            Topmost = true;
            Activate();
            SetForegroundWindow(hwnd);
            Topmost = false;

            LblBoundAppName.Text = !string.IsNullOrWhiteSpace(AppDisplayName) ? AppDisplayName : "Unconfigured";
            try
            {
                var hwid = global::NightAuth.SecurityProtections.GetHardwareId();
                LblLocalHwid.Text = $"HWID: {(string.IsNullOrWhiteSpace(hwid) ? "Detected on Auth" : hwid)}";
            }
            catch
            {
                LblLocalHwid.Text = "HWID: Detected on Auth";
            }

            await ConnectAsync();
        }


        private async Task ConnectAsync()
        {
            BtnAuthSubmit.IsEnabled = false;

            var result = await NightAuthApp.InitializeAsync();

            if (result.Success)
            {
                BtnAuthSubmit.IsEnabled = true;
            }
            else
            {
                if (result.Error == "version_mismatch" &&
                    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NIGHTAUTH_UPDATE_MANIFEST_URL")) &&
                    MessageBox.Show("A signed update is available. Download and verify it now?", "NightAuth update",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NightAuth", "updates");
                        Directory.CreateDirectory(directory);
                        var manifestUriStr = Environment.GetEnvironmentVariable("NIGHTAUTH_UPDATE_MANIFEST_URL")!;
                        var relKey = Environment.GetEnvironmentVariable("NIGHTAUTH_RELEASE_PUBLIC_KEY") ?? "";
                        var path = await SignedUpdateInstaller.DownloadAsync(new Uri(manifestUriStr, UriKind.Absolute),
                            relKey, AppDisplayName,
                            System.IO.Path.Combine(directory, $"{AppDisplayName}-{Guid.NewGuid():N}.verified"));
                        MessageBox.Show($"Signed update verified and saved to:\n{path}\n\nThis sample does not execute or install downloaded code.",
                            "NightAuth update", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception error) when (error is HttpRequestException or IOException or ArgumentException or CryptographicException)
                    {
                        MessageBox.Show($"Update rejected: {error.Message}", "NightAuth update", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #region Window Chrome

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void RootBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RootBorder.Clip = new RectangleGeometry(new Rect(0, 0, e.NewSize.Width, e.NewSize.Height), 16, 16);
        }

        #endregion

        #region Password show/hide (WPF's PasswordBox has no native reveal)

        private bool _syncingPassword;

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_syncingPassword) return;
            _syncingPassword = true;
            TxtPasswordVisible.Text = TxtPassword.Password;
            _syncingPassword = false;
            UpdatePasswordPlaceholder();
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_syncingPassword) return;
            _syncingPassword = true;
            TxtPassword.Password = TxtPasswordVisible.Text;
            _syncingPassword = false;
            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            bool hasText = !string.IsNullOrEmpty(CurrentPassword);
            bool isFocused = TxtPassword.IsFocused || TxtPasswordVisible.IsFocused;
            PasswordPlaceholder.Visibility = (!isFocused && !hasText) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            bool showing = TxtPasswordVisible.Visibility == Visibility.Visible;
            TxtPasswordVisible.Visibility = showing ? Visibility.Collapsed : Visibility.Visible;
            TxtPassword.Visibility = showing ? Visibility.Visible : Visibility.Collapsed;
            EyeOpenIcon.Visibility = showing ? Visibility.Visible : Visibility.Collapsed;
            EyeClosedIcon.Visibility = showing ? Visibility.Collapsed : Visibility.Visible;
        }

        private string CurrentPassword => TxtPasswordVisible.Visibility == Visibility.Visible ? TxtPasswordVisible.Text : TxtPassword.Password;

        #endregion

        #region Segmented Login tabs

        private void TabCredentials_Checked(object sender, RoutedEventArgs e) { if (TabCredentials.IsChecked == true) SetMode(AuthMode.LoginCredentials); }
        private void TabRegister_Checked(object sender, RoutedEventArgs e) { if (TabRegister.IsChecked == true) SetMode(AuthMode.Register); }
        private void TabLicenseOnly_Checked(object sender, RoutedEventArgs e) { if (TabLicenseOnly.IsChecked == true) SetMode(AuthMode.LoginLicense); }
        private void TabUpgrade_Checked(object sender, RoutedEventArgs e) { if (TabUpgrade.IsChecked == true) SetMode(AuthMode.Upgrade); }
        private void TabResetHwid_Checked(object sender, RoutedEventArgs e) { if (TabResetHwid.IsChecked == true) SetMode(AuthMode.ResetHwid); }

        #endregion

        #region Secondary link entry points

        private void BtnForgotLink_Click(object sender, RoutedEventArgs e) => SetMode(AuthMode.ForgotPassword);

        private void BtnForgotToggleToken_Click(object sender, RoutedEventArgs e)
        {
            if (_mode == AuthMode.ForgotPassword)
                SetMode(AuthMode.ResetPasswordToken);
            else
                SetMode(AuthMode.ForgotPassword);
        }

        private void BtnSwitchMode_Click(object sender, RoutedEventArgs e)
        {
            switch (_mode)
            {
                case AuthMode.Register:
                    SetMode(AuthMode.LoginCredentials);
                    break;
                case AuthMode.ForgotPassword:
                case AuthMode.ResetPasswordToken:
                    SetMode(AuthMode.LoginCredentials);
                    break;
                case AuthMode.Upgrade:
                case AuthMode.ResetHwid:
                case AuthMode.LoginLicense:
                    SetMode(AuthMode.LoginCredentials);
                    break;
                default:
                    SetMode(AuthMode.Register);
                    break;
            }
        }

        #endregion

        #region Mode switching

        private void SetMode(AuthMode mode)
        {
            _mode = mode;
            AuthErrorText.Visibility = Visibility.Collapsed;
            TxtTotp.Text = "";

            bool isTabbedMode = mode == AuthMode.LoginCredentials || mode == AuthMode.Register
                || mode == AuthMode.LoginLicense || mode == AuthMode.Upgrade || mode == AuthMode.ResetHwid;
            LoginSegmentBorder.Visibility = isTabbedMode ? Visibility.Visible : Visibility.Collapsed;
            if (mode == AuthMode.LoginCredentials) TabCredentials.IsChecked = true;
            if (mode == AuthMode.Register) TabRegister.IsChecked = true;
            if (mode == AuthMode.LoginLicense) TabLicenseOnly.IsChecked = true;
            if (mode == AuthMode.Upgrade) TabUpgrade.IsChecked = true;
            if (mode == AuthMode.ResetHwid) TabResetHwid.IsChecked = true;

            FieldUsernamePanel.Visibility = Visibility.Collapsed;
            FieldPasswordPanel.Visibility = Visibility.Collapsed;
            FieldTotpPanel.Visibility = Visibility.Collapsed;
            FieldEmailPanel.Visibility = Visibility.Collapsed;
            FieldTokenPanel.Visibility = Visibility.Collapsed;
            BtnForgotToggleToken.Visibility = Visibility.Collapsed;
            FieldLicensePanel.Visibility = Visibility.Collapsed;
            LicenseHintText.Visibility = Visibility.Collapsed;
            AuthHeadingText.Visibility = Visibility.Collapsed;
            AuthSubheadingText.Visibility = Visibility.Collapsed;
            BtnForgotLink.Visibility = Visibility.Visible;
            BtnSwitchMode.Visibility = Visibility.Visible;
            LblUsernameField.Text = "USERNAME";
            LblEmailField.Text = "EMAIL";

            switch (mode)
            {
                case AuthMode.LoginCredentials:
                    AuthHeadingText.Text = "Sign in to your account";
                    AuthHeadingText.Visibility = Visibility.Visible;
                    AuthSubheadingText.Text = "Enter your credentials to negotiate a secure zero-trust session.";
                    AuthSubheadingText.Visibility = Visibility.Visible;
                    FieldUsernamePanel.Visibility = Visibility.Visible;
                    FieldPasswordPanel.Visibility = Visibility.Visible;
                    FieldTotpPanel.Visibility = Visibility.Visible;
                    BtnSubmitText.Text = "Sign In";
                    BtnSwitchMode.Content = "Need an account? Register";
                    break;

                case AuthMode.LoginLicense:
                    AuthHeadingText.Text = "Instant license access";
                    AuthHeadingText.Visibility = Visibility.Visible;
                    AuthSubheadingText.Text = "Authenticate directly using an active license key without username.";
                    AuthSubheadingText.Visibility = Visibility.Visible;
                    FieldLicensePanel.Visibility = Visibility.Visible;
                    LicenseHintText.Visibility = Visibility.Visible;
                    BtnSubmitText.Text = "Continue with License";
                    BtnSwitchMode.Content = "Need an account? Register";
                    break;

                case AuthMode.Register:
                    AuthHeadingText.Text = "Create your account";
                    AuthHeadingText.Visibility = Visibility.Visible;
                    AuthSubheadingText.Text = "Registers a username bound to an unused license key.";
                    AuthSubheadingText.Visibility = Visibility.Visible;
                    FieldUsernamePanel.Visibility = Visibility.Visible;
                    FieldPasswordPanel.Visibility = Visibility.Visible;
                    BtnForgotLink.Visibility = Visibility.Collapsed;
                    FieldLicensePanel.Visibility = Visibility.Visible;
                    FieldEmailPanel.Visibility = Visibility.Visible;
                    LblEmailField.Text = "EMAIL (OPTIONAL)";
                    BtnSubmitText.Text = "Create Account";
                    BtnSwitchMode.Content = "Already have an account? Sign In";
                    break;

                case AuthMode.ForgotPassword:
                    AuthHeadingText.Text = "Forgot password";
                    AuthHeadingText.Visibility = Visibility.Visible;
                    AuthSubheadingText.Text = "Enter your email to receive a reset link.";
                    AuthSubheadingText.Visibility = Visibility.Visible;
                    FieldEmailPanel.Visibility = Visibility.Visible;
                    BtnForgotToggleToken.Content = "Already have a reset token?";
                    BtnForgotToggleToken.Visibility = Visibility.Visible;
                    BtnSubmitText.Text = "Send Reset Link";
                    BtnSwitchMode.Content = "Back to Sign In";
                    break;

                case AuthMode.ResetPasswordToken:
                    AuthHeadingText.Text = "Reset password with token";
                    AuthHeadingText.Visibility = Visibility.Visible;
                    AuthSubheadingText.Text = "Enter the reset token received by email and your new password.";
                    AuthSubheadingText.Visibility = Visibility.Visible;
                    FieldTokenPanel.Visibility = Visibility.Visible;
                    FieldPasswordPanel.Visibility = Visibility.Visible;
                    BtnForgotLink.Visibility = Visibility.Collapsed;
                    BtnForgotToggleToken.Content = "Need a token? Request email";
                    BtnForgotToggleToken.Visibility = Visibility.Visible;
                    BtnSubmitText.Text = "Set New Password";
                    BtnSwitchMode.Content = "Back to Sign In";
                    break;

                case AuthMode.Upgrade:
                    AuthHeadingText.Text = "Upgrade plan with license key";
                    AuthHeadingText.Visibility = Visibility.Visible;
                    AuthSubheadingText.Text = "Adds this license's time to the target account's subscription.";
                    AuthSubheadingText.Visibility = Visibility.Visible;
                    LblUsernameField.Text = "TARGET USERNAME";
                    FieldUsernamePanel.Visibility = Visibility.Visible;
                    FieldLicensePanel.Visibility = Visibility.Visible;
                    BtnSubmitText.Text = "Apply Upgrade";
                    BtnSwitchMode.Content = "Back to Sign In";
                    break;

                case AuthMode.ResetHwid:
                    AuthHeadingText.Text = "Reset hardware ID";
                    AuthHeadingText.Visibility = Visibility.Visible;
                    AuthSubheadingText.Text = "For when a HWID mismatch blocks login itself.";
                    AuthSubheadingText.Visibility = Visibility.Visible;
                    FieldUsernamePanel.Visibility = Visibility.Visible;
                    FieldPasswordPanel.Visibility = Visibility.Visible;
                    FieldTotpPanel.Visibility = Visibility.Visible;
                    BtnForgotLink.Visibility = Visibility.Collapsed;
                    BtnSubmitText.Text = "Reset HWID";
                    BtnSwitchMode.Content = "Back to Sign In";
                    break;
            }
        }

        #endregion

        #region Submit

        private void SetButtonLoading(bool isLoading, string defaultText)
        {
            BtnAuthSubmit.IsEnabled = !isLoading;
            if (isLoading)
            {
                BtnSpinnerGlyph.Visibility = Visibility.Visible;
                BtnSubmitText.Text = "Processing...";

                var anim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(0.85),
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                };
                SpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, anim);
            }
            else
            {
                SpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, null);
                BtnSpinnerGlyph.Visibility = Visibility.Collapsed;
                BtnSubmitText.Text = defaultText;
            }
        }

        private async void BtnAuthSubmit_Click(object sender, RoutedEventArgs e)
        {
            string currentBtnText = BtnSubmitText.Text;
            SetButtonLoading(true, currentBtnText);
            AuthErrorText.Visibility = Visibility.Collapsed;

            string user = TxtUsername.Text.Trim();
            string pass = CurrentPassword;
            string email = TxtEmail.Text.Trim();
            string key = TxtLicenseKey.Text.Trim();
            string totp = TxtTotp.Text.Trim();

            bool goToDashboard = false;
            string message = "";
            NightAuthResult result;

            switch (_mode)
            {
                case AuthMode.LoginCredentials:
                    result = await NightAuthApp.LoginAsync(user, pass, string.IsNullOrEmpty(totp) ? null : totp);
                    goToDashboard = result.Success;
                    break;
                case AuthMode.LoginLicense:
                    result = await NightAuthApp.LicenseAsync(key);
                    goToDashboard = result.Success;
                    break;
                case AuthMode.Register:
                    result = await NightAuthApp.RegisterAsync(user, pass, key, email);
                    goToDashboard = result.Success;
                    break;
                case AuthMode.ForgotPassword:
                    result = await NightAuthApp.ForgotPasswordAsync(email);
                    message = "If an account matches, a reset link has been sent to that email.";
                    break;
                case AuthMode.ResetPasswordToken:
                    string token = TxtResetToken.Text.Trim();
                    result = await NightAuthApp.ResetPasswordAsync(token, pass);
                    if (result.Success)
                    {
                        message = "Password updated successfully! You can now sign in.";
                        SetMode(AuthMode.LoginCredentials);
                    }
                    else
                    {
                        message = result.DisplayMessage;
                    }
                    break;
                case AuthMode.Upgrade:
                    result = await NightAuthApp.UpgradeAsync(user, key);
                    message = result.DisplayMessage;
                    break;
                case AuthMode.ResetHwid:
                    result = await NightAuthApp.ResetHwidAsync(user, pass, string.IsNullOrEmpty(totp) ? null : totp);
                    message = result.DisplayMessage;
                    break;
                default:
                    result = await NightAuthApp.CheckAsync();
                    break;
            }

            if (!result.Success) message = result.DisplayMessage;

            SetButtonLoading(false, currentBtnText);

            if (!result.Success && (_mode == AuthMode.LoginCredentials || _mode == AuthMode.ResetHwid)
                && (result.Error == "totp_required" || result.Error == "totp_invalid"))
            {
                TxtTotp.Focus();
                AuthErrorText.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                AuthErrorText.Text = result.Error == "totp_required"
                    ? "Two-factor authentication is required. Please enter your 6-digit 2FA code."
                    : message;
                AuthErrorText.Visibility = Visibility.Visible;
                return;
            }

            if (!result.Success)
            {
                AuthErrorText.Foreground = (SolidColorBrush)FindResource("ErrorBrush");
                AuthErrorText.Text = message;
                AuthErrorText.Visibility = Visibility.Visible;
                return;
            }

            if (goToDashboard)
            {
                new DashboardWindow(NightAuthApp, AppDisplayName, result.ToUserData()).Show();
                Close();
                return;
            }

            SetMode(AuthMode.LoginCredentials);
            AuthErrorText.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
            AuthErrorText.Text = message;
            AuthErrorText.Visibility = Visibility.Visible;
        }

        #endregion


    }
}