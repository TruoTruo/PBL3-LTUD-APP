using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using StudentReminderApp.Views.Auth;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Main
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Chờ 3 giây (3000 milliseconds)
            await Task.Delay(3000);

            // Bắt đầu hiệu ứng mờ dần (Fade Out) trước khi mở Login
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(500));
            fadeOut.Completed += (s, ev) =>
            {
                // KIỂM TRA TỰ ĐỘNG ĐĂNG NHẬP
                if (StudentReminderApp.Properties.Settings.Default.RememberMe)
                {
                    string savedUser = StudentReminderApp.Properties.Settings.Default.Username;
                    string savedPass = StudentReminderApp.Properties.Settings.Default.Password;

                    AccountBLL bll = new AccountBLL();
                    var result = bll.Login(savedUser, savedPass);

                    if (result.Item1) // Nếu đăng nhập thành công
                    {
                        SessionManager.SetSession(result.Item3, result.Item4);
                        MainWindow mainWindow = new MainWindow();
                        Application.Current.MainWindow = mainWindow;
                        mainWindow.Show();
                        this.Close();
                        return; // Kết thúc tại đây, bỏ qua việc mở LoginWindow
                    }
                }

                // Sau khi mờ hẳn thì mới khởi tạo và hiển thị cửa sổ Đăng nhập
                AuthWindow authWindow = new AuthWindow();
                
                // Cực kỳ quan trọng: Set màn hình đăng nhập làm màn hình chính của App
                Application.Current.MainWindow = authWindow;
                authWindow.Show();

                // Đóng Splash Screen lại
                this.Close();
            };
            
            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}