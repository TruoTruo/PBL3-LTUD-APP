using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Views.Dialogs;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Pages
{
    public partial class DashboardPage : Page
    {
        private readonly EventBLL _eventBll = new EventBLL();

        public DashboardPage() { InitializeComponent(); Loaded += (s, e) => LoadData(); }

        private void LoadData()
        {
            var idAcc = SessionManager.CurrentAccount.IdAcc;
            var user = SessionManager.CurrentUser;
            var hour = DateTime.Now.Hour;
            var greet = hour < 12 ? "Chào buổi sáng" : hour < 18 ? "Chào buổi chiều" : "Chào buổi tối";

            TxtGreeting.Text = $"{greet}, {user?.HoTen ?? "bạn"}!";
            TxtDate.Text = $"Hôm nay là {DateTime.Now:dddd, dd/MM/yyyy}";

            var upcoming = _eventBll.GetUpcoming(idAcc, 7);
            var deadlines = upcoming.Count(e => e.EventType == "DEADLINE");
            var today = upcoming.Count(e => e.StartTime.Date == DateTime.Today);

            StatEvents.Text = today.ToString();
            StatDeadlines.Text = deadlines.ToString();
            StatCourses.Text = "—";
            StatNotifs.Text = "0";

            if (upcoming.Count > 0)
            {
                EventList.ItemsSource = upcoming;
                TxtNoEvent.Visibility = Visibility.Collapsed;
            }
            else
                TxtNoEvent.Visibility = Visibility.Visible;

            TxtNoSchedule.Visibility = Visibility.Visible;
        }

        private void BtnAddEvent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new EventDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) LoadData();
        }
    }
}
