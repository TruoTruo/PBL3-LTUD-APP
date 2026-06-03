using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StudentReminderApp.Views.Auth.Components;

namespace StudentReminderApp.Views.Auth
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            // Tải LoginView làm màn hình mặc định khi mở lên
            FormContainer.Content = new LoginView();
        }

        // Cho phép dùng chuột kéo cửa sổ
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.GetPosition(this).Y <= 40)
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

        /// <summary>
        /// Hàm điều hướng, thay đổi UserControl hiển thị trong ContentControl
        /// </summary>
        public void Navigate(UserControl nextView)
        {
            // TODO: Thêm hiệu ứng chuyển cảnh (trượt ngang) ở đây
            this.FormContainer.Content = nextView;

            // Cập nhật tiêu đề cửa sổ
            if (nextView is LoginView)
                TitleText.Text = "Đăng nhập — Student Reminder";
            else if (nextView is RegisterView)
                TitleText.Text = "Đăng ký — Student Reminder";
            else if (nextView is ForgotPasswordView)
                TitleText.Text = "Quên mật khẩu — Student Reminder";
        }
    }
}