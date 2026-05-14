using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using StudentReminderApp.BLL;

namespace StudentReminderApp.Views.Auth
{
    public partial class RegisterWindow : Window
    {
        private readonly AuthBLL _bll = new AuthBLL();
        public RegisterWindow() => InitializeComponent();

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Visibility = TxtSuccess.Visibility = Visibility.Collapsed;

            // ── Validate MSSV trước khi gọi BLL ──────────────────
            string mssv = TxtMssv.Text.Trim();
            if (!Regex.IsMatch(mssv, @"^102\d{6}$"))
            {
                TxtError.Text = "⚠ MSSV phải có đúng 9 chữ số và bắt đầu bằng '102'.";
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            var (ok, msg) = _bll.Register(
                mssv,               // username = MSSV
                TxtPwd.Password,
                TxtConfirm.Password,
                TxtHoTen.Text,
                TxtEmail.Text,
                TxtSdt.Text);

            if (!ok)
            {
                TxtError.Text = "⚠ " + msg;
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            TxtSuccess.Text = "✓ " + msg;
            TxtSuccess.Visibility = Visibility.Visible;
            await Task.Delay(1200);
            new LoginWindow().Show();
            Close();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }
    }
}
