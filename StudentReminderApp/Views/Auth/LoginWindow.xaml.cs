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

        public LoginWindow() => InitializeComponent();

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