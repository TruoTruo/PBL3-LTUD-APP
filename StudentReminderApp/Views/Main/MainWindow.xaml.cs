using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.Helpers;
using StudentReminderApp.Services;
using StudentReminderApp.Views.Dialogs;
using StudentReminderApp.Views.Pages;

namespace StudentReminderApp.Views.Main
{
    public partial class MainWindow : Window
    {
        private readonly ReminderService _reminder = new ReminderService();
        private Button _activeNavBtn;

        public MainWindow()
        {
            InitializeComponent();
            TxtUserName.Text = $"Xin chào, {SessionManager.CurrentUser?.HoTen ?? "Sinh viên"}";
            _reminder.NotificationReady += ShowPopup;
            _reminder.Start();
            SetActiveNav(BtnDashboard);
            ContentFrame.Navigate(new DashboardPage());
        }

        private void NavBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            SetActiveNav(btn);
            ContentFrame.Navigate(btn?.Tag?.ToString() switch
            {
                "Dashboard" => (object)new DashboardPage(),
                "Calendar"  => new CalendarPage(),
                "Course"    => new CoursePage(),
                "Advisor"   => new AdvisorPage(),
                "Forum"     => new ForumPage(),
                "Profile"   => new ProfilePage(),
                _           => new DashboardPage()
            });
        }

        private void SetActiveNav(Button btn)
        {
            if (_activeNavBtn != null)
                _activeNavBtn.Style = (Style)FindResource("NavBtn");
            _activeNavBtn = btn;
            if (btn != null)
                btn.Style = (Style)FindResource("NavBtnActive");
        }

        private void ShowPopup(string title, string content)
        {
            Dispatcher.Invoke(() =>
            {
                var popup = new NotificationPopup(title, content) { Owner = this };
                popup.Show();
            });
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            _reminder.Stop();
            SessionManager.Clear();
            new Auth.LoginWindow().Show();
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _reminder.Stop();
            base.OnClosed(e);
        }
    }
}
