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
            var (ok, msg, acc, user) = _bll.Login(TxtUsername.Text, TxtPassword.Password);
            if (!ok) { TxtError.Text = "⚠ " + msg; TxtError.Visibility = Visibility.Visible; return; }
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
