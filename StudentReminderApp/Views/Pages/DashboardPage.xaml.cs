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
            var today = upcoming.Count(e => e.StartTime.Date == DateTime.Today);

            StatUpcoming.Text = upcoming.Count.ToString();
            StatEvents.Text = today.ToString();

            // Tính năng thông báo
            var notifDal = new StudentReminderApp.DAL.NotificationDAL();
            var unreadNotifs = notifDal.GetPending(idAcc).Count;
            StatNotifs.Text = unreadNotifs.ToString();

            // Lấy số lượng học phần đang học
            int dangHoc = 0;
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT COUNT(*) FROM TICH_LUY_TIN_CHI WHERE id_sv=@uid AND trang_thai_hoc='DangHoc'", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", idAcc);
                        var res = cmd.ExecuteScalar();
                        if (res != null) dangHoc = Convert.ToInt32(res);
                    }
                }
            }
            catch { }
            StatCourses.Text = dangHoc.ToString();

            // Cập nhật danh sách sự kiện 7 ngày tới
            if (upcoming.Count > 0)
            {
                EventList.ItemsSource = upcoming.Select(e => new {
                    Title = e.Title,
                    Location = string.IsNullOrEmpty(e.Location) ? "Không có địa điểm" : e.Location,
                    StartTime = e.StartTime,
                    EventTypeIcon = e.EventType == "ACADEMIC" ? "📚" :
                                    e.EventType == "DEADLINE" ? "⚠" :
                                    e.EventType == "EXAM" ? "📝" : "📅"
                }).ToList();
                TxtNoEvent.Visibility = Visibility.Collapsed;
            }
            else
            {
                EventList.ItemsSource = null;
                TxtNoEvent.Visibility = Visibility.Visible;
            }

            // Cập nhật danh sách Lịch học và Lịch thi hôm nay
            var todayClasses = upcoming.Where(e => (e.EventType == "ACADEMIC" || e.EventType == "EXAM") && e.StartTime.Date == DateTime.Today)
                .OrderBy(e => e.StartTime)
                .Select(e => new {
                    TenMonHoc = e.Title,
                    StartTime = e.StartTime.ToString("HH:mm") + " - " + e.EndTime.ToString("HH:mm"),
                    TenPhong = string.IsNullOrEmpty(e.Location) ? "Chưa rõ phòng" : e.Location
                }).ToList();

            if (todayClasses.Count > 0)
            {
                TodaySchedule.ItemsSource = todayClasses;
                TxtNoSchedule.Visibility = Visibility.Collapsed;
            }
            else
            {
                TodaySchedule.ItemsSource = null;
                TxtNoSchedule.Visibility = Visibility.Visible;
            }

            // Cập nhật danh sách Lịch học và Lịch thi ngày mai
            DateTime tomorrow = DateTime.Today.AddDays(1);
            var tomorrowClasses = upcoming.Where(e => (e.EventType == "ACADEMIC" || e.EventType == "EXAM") && e.StartTime.Date == tomorrow)
                .OrderBy(e => e.StartTime)
                .Select(e => new {
                    TenMonHoc = e.Title,
                    StartTime = e.StartTime.ToString("HH:mm") + " - " + e.EndTime.ToString("HH:mm"),
                    TenPhong = string.IsNullOrEmpty(e.Location) ? "Chưa rõ phòng" : e.Location
                }).ToList();

            if (tomorrowClasses.Count > 0)
            {
                TomorrowSchedule.ItemsSource = tomorrowClasses;
                TxtNoScheduleTomorrow.Visibility = Visibility.Collapsed;
            }
            else
            {
                TomorrowSchedule.ItemsSource = null;
                TxtNoScheduleTomorrow.Visibility = Visibility.Visible;
            }
        }

        private void BtnAddEvent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new EventDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) LoadData();
        }
    }
}
