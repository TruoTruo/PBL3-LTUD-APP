using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Auth.Components
{
    public partial class LoginView : UserControl
    {
        private readonly AccountBLL _bll = new AccountBLL();
        private AuthWindow _parent;

        public LoginView(AuthWindow parent = null)
        {
            InitializeComponent();
            _parent = parent;
            
            // Tự động focus vào ô Username khi mở
            Loaded += (s, e) => TxtUsername.Focus();

            TxtUsername.GotFocus += (s, e) => _parent?.UpdateAnimationState("state-email");
            TxtUsername.LostFocus += (s, e) => _parent?.UpdateAnimationState("");

            TxtPassword.GotFocus += (s, e) => _parent?.UpdateAnimationState(_isPasswordVisible ? "state-ignoring" : "state-peeking");
            TxtPassword.LostFocus += (s, e) => _parent?.UpdateAnimationState("");
            
            TxtPasswordVisible.GotFocus += (s, e) => _parent?.UpdateAnimationState("state-ignoring");
            TxtPasswordVisible.LostFocus += (s, e) => _parent?.UpdateAnimationState("");

            if (StudentReminderApp.Properties.Settings.Default.RememberMe)
            {
                TxtUsername.Text = StudentReminderApp.Properties.Settings.Default.Username;
                TxtPassword.Password = StudentReminderApp.Properties.Settings.Default.Password;
                ChkRememberMe.IsChecked = true;
            }
        }

        private bool _isPasswordVisible = false;

        // Sự kiện click vào icon con mắt
        private void BtnTogglePassword_Click(object sender, MouseButtonEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            if (_isPasswordVisible)
            {
                TxtPasswordVisible.Text = TxtPassword.Password;
                TxtPasswordVisible.Visibility = Visibility.Visible;
                TxtPassword.Visibility = Visibility.Collapsed;
                BtnTogglePassword.Text = "👁️";
                
                TxtPasswordVisible.Focus();
                TxtPasswordVisible.CaretIndex = TxtPasswordVisible.Text.Length;

                _parent?.UpdateAnimationState("state-ignoring");
            }
            else
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
                TxtPasswordVisible.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;
                BtnTogglePassword.Text = "🙈";

                TxtPassword.Focus();
                // SelectAll or setting caret is tricky for PasswordBox, but Focus() works to resume typing
                GetType().GetMethod("SelectAll")?.Invoke(TxtPassword, null); // optional, just to keep caret at end, though PasswordBox resets it to start without a hack

                _parent?.UpdateAnimationState("state-peeking");
            }
        }

        private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
                ClearErrorState();
            }
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isPasswordVisible)
                ClearErrorState();
        }

        private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearErrorState();
        }

        private void ClearErrorState()
        {
            LblPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#777777"));
            TxtPassword.ClearValue(Control.BorderBrushProperty);
            TxtPasswordVisible.ClearValue(Control.BorderBrushProperty);
            TxtError.Visibility = Visibility.Hidden;
        }

        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TxtPassword.Focus();
            }
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, new RoutedEventArgs());
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            ClearErrorState();

            string username = TxtUsername.Text.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                TxtError.Text = "Please enter your username.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            var result =
                _bll.Login(username, TxtPassword.Password);

            bool ok = result.Item1;
            Account? acc = result.Item3;
            User? user = result.Item4;

            if (!ok)
            {
                TxtError.Text = "Invalid username or password.";
                TxtError.Visibility = Visibility.Visible;

                var redBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4F"));
                LblPassword.Foreground = redBrush;
                TxtPassword.BorderBrush = redBrush;
                TxtPasswordVisible.BorderBrush = redBrush;
                return;
            }

            if (ChkRememberMe.IsChecked == true)
            {
                StudentReminderApp.Properties.Settings.Default.Username = username;
                StudentReminderApp.Properties.Settings.Default.Password = TxtPassword.Password;
                StudentReminderApp.Properties.Settings.Default.RememberMe = true;
            }
            else
            {
                StudentReminderApp.Properties.Settings.Default.Username = string.Empty;
                StudentReminderApp.Properties.Settings.Default.Password = string.Empty;
                StudentReminderApp.Properties.Settings.Default.RememberMe = false;
            }
            StudentReminderApp.Properties.Settings.Default.Save();

            SessionManager.SetSession(acc, user);
            new Main.MainWindow().Show();
            Window.GetWindow(this)?.Close();
        }

        private void TxtForgotPwd_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is AuthWindow parent)
                parent.Navigate(new ForgotPasswordView());
        }

        private void BtnToRegister_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is AuthWindow parent)
                parent.Navigate(new RegisterView());
        }
    }
}