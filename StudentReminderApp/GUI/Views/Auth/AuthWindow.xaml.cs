using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using StudentReminderApp.Views.Auth.Components;

namespace StudentReminderApp.Views.Auth
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            FormContainer.Content = new LoginView(this);
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await AnimationWebView.EnsureCoreWebView2Async(null);
            string htmlPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "GUI", "Assets", "login_animation.html");
            AnimationWebView.Source = new System.Uri(htmlPath);
        }

        public void UpdateAnimationState(string state)
        {
            if (AnimationWebView != null && AnimationWebView.CoreWebView2 != null)
            {
                AnimationWebView.ExecuteScriptAsync($"setAnimationState('{state}');");
            }
        }

        private System.DateTime _lastMouseMoveTime = System.DateTime.MinValue;

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (AnimationWebView != null && AnimationWebView.CoreWebView2 != null)
            {
                var now = System.DateTime.Now;
                if ((now - _lastMouseMoveTime).TotalMilliseconds > 20) // Throttle to ~50 FPS
                {
                    _lastMouseMoveTime = now;
                    var pos = e.GetPosition(this); // Get position relative to the window so the math is still correct
                    AnimationWebView.ExecuteScriptAsync($"setMousePosition({pos.X}, {pos.Y});");
                }
            }
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