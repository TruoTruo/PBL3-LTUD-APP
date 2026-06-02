using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using StudentReminderApp.BLL;

namespace StudentReminderApp.Views.Auth
{
    public partial class RegisterWindow : Window
    {
        private readonly AccountBLL _bll = new AccountBLL();
        public RegisterWindow() => InitializeComponent();

        // Cho phép dùng chuột kéo cửa sổ
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        // Đóng cửa sổ khi bấm nút X
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Thu nhỏ cửa sổ khi bấm nút -
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

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

            // ════════════════════════════════════════════════════════════
            // ĐÃ FIX: Đổi từ rã ValueTuple (ok, msg) sang nhận class Tuple chuẩn
            // ════════════════════════════════════════════════════════════
            Tuple<bool, string> result = _bll.Register(
                mssv,               // username = MSSV
                TxtPwd.Password,
                TxtConfirm.Password,
                TxtHoTen.Text,
                "",  // Đã xóa trường Email trên UI nên truyền rỗng
                ""); // Đã xóa trường SĐT trên UI nên truyền rỗng

            // Trích xuất giá trị từ class Tuple thông qua Item1 và Item2
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
