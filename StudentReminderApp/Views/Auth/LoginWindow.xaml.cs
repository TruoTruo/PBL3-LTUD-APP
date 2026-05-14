using System.Windows;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.Views.Auth
{
    public partial class LoginWindow : Window
    {
        private readonly AuthBLL _bll = new AuthBLL();
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

            // ── KHÔNG validate format MSSV ở đây ──────────────────
            // Validate MSSV chỉ thực hiện lúc ĐĂNG KÝ (RegisterWindow).
            // Màn hình đăng nhập chấp nhận mọi username (kể cả admin_test).

            var (ok, msg, acc, user) = _bll.Login(username, TxtPassword.Password);

            if (!ok)
            {
                TxtError.Text       = "⚠ " + msg;
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            SessionManager.SetSession(acc, user);
            new Main.MainWindow().Show();
            Close();
        }

        private void BtnToRegister_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }
    }
}
