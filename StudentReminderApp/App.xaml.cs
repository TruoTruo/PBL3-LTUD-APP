using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace StudentReminderApp
{
    public partial class App : Application
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
            StudentReminderApp.Services.NotificationService.Start();

            // Khởi tạo icon dưới khay hệ thống (System Tray)
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Icon = System.Drawing.SystemIcons.Information; // Có thể thay bằng đường dẫn file .ico của bạn
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Student Reminder App đang chạy ngầm";
            
            // Bấm đúp chuột để mở lại cửa sổ
            _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();

            // Thêm Menu chuột phải
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Mở ứng dụng", null, (s, args) => ShowMainWindow());
            contextMenu.Items.Add("Thoát", null, (s, args) => {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                Current.Shutdown();
            });
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                if (MainWindow.WindowState == WindowState.Minimized)
                    MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null)
                _notifyIcon.Dispose();
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Ngăn ứng dụng tự thoát ngay lập tức
            e.Handled = true;

            // 1. Ghi lại lỗi chi tiết vào file để phân tích
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "StudentReminderApp_CrashLog.txt");
                string errorDetails = $"[{DateTime.Now}]\nLỗi: {e.Exception.Message}\nStack Trace:\n{e.Exception.StackTrace}\n\n";
                File.AppendAllText(logPath, errorDetails);
            }
            catch { /* Bỏ qua nếu không ghi được log */ }

            // 2. Hiển thị thông báo thân thiện cho người dùng
            MessageBox.Show(
                "Rất tiếc, ứng dụng đã gặp phải một lỗi không mong muốn và cần phải đóng. " +
                "Thông tin chi tiết về lỗi đã được ghi lại để chúng tôi có thể sửa chữa.\n\n" +
                "Chi tiết lỗi: " + e.Exception.Message,
                "Lỗi nghiêm trọng",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // 3. Đóng ứng dụng một cách an toàn
            Current.Shutdown();
        }
    }
}