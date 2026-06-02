using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Auth
{
    public partial class LoginWindow : Window
    {
        private readonly AccountBLL _bll = new AccountBLL();

        public LoginWindow()
        {
            InitializeComponent();
            
            // Tự động focus vào ô Username khi mở app
            TxtUsername.Focus();

            if (StudentReminderApp.Properties.Settings.Default.RememberMe)
            {
                TxtUsername.Text = StudentReminderApp.Properties.Settings.Default.Username;
                TxtPassword.Password = StudentReminderApp.Properties.Settings.Default.Password;
                ChkRememberMe.IsChecked = true;
            }
        }

        // Cho phép dùng chuột kéo cửa sổ
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Đóng ứng dụng khi bấm nút X
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Thu nhỏ ứng dụng khi bấm nút -
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
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
                BtnTogglePassword.Text = "🙈"; // Đổi icon
            }
            else
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
                TxtPasswordVisible.Visibility = Visibility.Collapsed;
                TxtPassword.Visibility = Visibility.Visible;
                BtnTogglePassword.Text = "👁️"; // Trở lại icon cũ
            }
        }

        // Đồng bộ dữ liệu khi người dùng sửa mật khẩu lúc đang hiện chữ
        private void TxtPasswordVisible_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                TxtPassword.Password = TxtPasswordVisible.Text;
                ClearErrorState();
            }
        }

        // Gõ phím là tự động xoá trạng thái đỏ báo lỗi đi
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isPasswordVisible)
                ClearErrorState();
        }

        private void TxtUsername_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ClearErrorState();
        }

        // Reset màu sắc về trạng thái xám bình thường
        private void ClearErrorState()
        {
            LblPassword.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#777777"));
            TxtPassword.ClearValue(Control.BorderBrushProperty);
            TxtPasswordVisible.ClearValue(Control.BorderBrushProperty);
            TxtError.Visibility = Visibility.Hidden;
        }

        // Bắt sự kiện nhấn phím Enter ở ô Username -> Chuyển focus sang ô Password
        private void TxtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TxtPassword.Focus();
            }
        }

        // Bắt sự kiện nhấn phím Enter ở ô Mật khẩu -> Đăng nhập
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
                TxtError.Text       = "Please enter your username.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            // Dùng Tuple tường minh — tránh CS8130
            Tuple<bool, string, Account, User> result =
                _bll.Login(username, TxtPassword.Password);

            bool    ok   = result.Item1;
            string  msg  = result.Item2;
            Account acc  = result.Item3;
            User    user = result.Item4;

            if (!ok)
            {
                TxtError.Text       = "Invalid username or password.";
                TxtError.Visibility = Visibility.Visible;

                var redBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4F"));
                LblPassword.Foreground = redBrush;
                TxtPassword.BorderBrush = redBrush;
                TxtPasswordVisible.BorderBrush = redBrush;
                return;
            }

            // Lưu thông tin khi đăng nhập thành công
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

            // user đã chứa TenLop (GetUserWithClass được gọi trong AccountBLL.Login)
            SessionManager.SetSession(acc, user);
            new Main.MainWindow().Show();
            Close();
        }

        private void TxtForgotPwd_Click(object sender, MouseButtonEventArgs e)
        {
            new ForgotPasswordWindow().Show();
            Close();
        }

        private void BtnToRegister_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }
    }
}