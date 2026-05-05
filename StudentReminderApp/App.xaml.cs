using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace StudentReminderApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
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