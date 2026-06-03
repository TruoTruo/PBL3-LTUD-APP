using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.BLL;

namespace StudentReminderApp.Views.Auth.Components
{
    public partial class RegisterView : UserControl
    {
        private readonly AccountBLL _bll = new AccountBLL();

        public RegisterView()
        {
            InitializeComponent();
            Loaded += (s, e) => TxtHoTen.Focus();
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = TxtSuccess.Visibility = Visibility.Collapsed;

            string mssv = TxtMssv.Text.Trim();
            if (!Regex.IsMatch(mssv, @"^102\d{6}$"))
            {
                TxtError.Text = "⚠ MSSV phải có đúng 9 chữ số và bắt đầu bằng '102'.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            Tuple<bool, string> result = _bll.Register(
                mssv,
                TxtPwd.Password,
                TxtConfirm.Password,
                TxtHoTen.Text,
                TxtEmail.Text,
                ""); 

            bool ok = result.Item1;
            string msg = result.Item2;

            if (!ok)
            {
                TxtError.Text = "⚠ " + msg;
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            TxtSuccess.Text = "✓ " + msg;
            TxtSuccess.Visibility = Visibility.Visible;
            await Task.Delay(1200);
            
            if (Window.GetWindow(this) is AuthWindow parent)
                parent.Navigate(new LoginView());
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is AuthWindow parent)
                parent.Navigate(new LoginView());
        }
    }
}