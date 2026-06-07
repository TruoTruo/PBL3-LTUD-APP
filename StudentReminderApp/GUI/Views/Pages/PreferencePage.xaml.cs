using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace StudentReminderApp.Views.Pages
{
    public partial class PreferencePage : Page
    {
        public PreferencePage()
        {
            InitializeComponent();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            // Lấy trạng thái từ nút bật/tắt (False = Ưu tiên giáo viên, True = Ưu tiên môn học)
            bool isSubjectPriority = TogglePreference.IsChecked == true;

            // TODO: Bạn có thể lưu isSubjectPriority vào Database (Account) hoặc SessionManager tại đây.

            if (NavigationService != null)
            {
                // Sau khi lưu thiết lập, điều hướng tới trang tiếp theo (ví dụ: Trang chủ Dashboard)
                NavigationService.Navigate(new DashboardPage());
            }
        }
    }
}