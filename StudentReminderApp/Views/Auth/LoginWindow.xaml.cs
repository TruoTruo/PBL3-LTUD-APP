using System;
using System.Windows;
using System.Windows.Input;
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
            if (StudentReminderApp.Properties.Settings.Default.RememberMe)
            {
                TxtUsername.Text = StudentReminderApp.Properties.Settings.Default.Username;
                TxtPassword.Password = StudentReminderApp.Properties.Settings.Default.Password;
                ChkRememberMe.IsChecked = true;
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = Visibility.Collapsed;

            string username = TxtUsername.Text.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                TxtError.Text       = "⚠ Vui lòng nhập MSSV hoặc tên đăng nhập.";
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
                TxtError.Text       = "⚠ " + msg;
                TxtError.Visibility = Visibility.Visible;
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
            ForgotPasswordWindow win = new ForgotPasswordWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void BtnToRegister_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }
    }
}