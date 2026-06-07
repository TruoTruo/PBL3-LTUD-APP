using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Views.Dialogs;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Pages
{
    public class ReminderCheckItem : INotifyPropertyChanged
    {
        public long IdEvent { get; set; }
        public string Title { get; set; } = string.Empty;

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set { if (_isChecked != value) { _isChecked = value; OnPropertyChanged(nameof(IsChecked)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ScheduleItem
    {
        public string TenMonHoc { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string TenPhong { get; set; } = "";
    }

    public class EventItem
    {
        public string Title { get; set; } = "";
        public string Location { get; set; } = "";
        public DateTime StartTime { get; set; }
        public string EventTypeIcon { get; set; } = "";
    }

    public partial class DashboardPage : Page
    {
        private readonly EventBLL _eventBll = new EventBLL();
        private bool _suppressCheckEvent = false;

        public DashboardPage() { InitializeComponent(); Loaded += (s, e) => LoadData(); }

        private void LoadData()
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (SessionManager.CurrentAccount == null)
            {
                TxtGreeting.Text = "Vui lòng đăng nhập";
                return;
            }

            var idAcc = SessionManager.CurrentAccount.IdAcc;
            var user = SessionManager.CurrentUser;
            var hour = DateTime.Now.Hour;
            var greet = hour < 12 ? "Chào buổi sáng" : hour < 18 ? "Chào buổi chiều" : "Chào buổi tối";

            TxtGreeting.Text = $"{greet}, {user?.HoTen ?? "bạn"}!";
            TxtDate.Text = $"Hôm nay là {DateTime.Now:dddd, dd/MM/yyyy}";

            // SỬA LỖI: Lấy trực tiếp từ database bắt đầu từ 00:00:00 hôm nay
            // Hàm GetUpcoming có thể đang dùng GETDATE() làm sót các sự kiện đã qua trong ngày hôm nay.
            var upcoming = new System.Collections.Generic.List<PersonalEvent>();
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT id_event, title, location, start_time, end_time, event_type, is_completed 
                                     FROM PERSONAL_EVENT 
                                     WHERE id_acc=@uid AND start_time >= @start AND start_time <= @end";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", idAcc);
                        cmd.Parameters.AddWithValue("@start", DateTime.Today.ToUniversalTime()); 
                        cmd.Parameters.AddWithValue("@end", DateTime.Today.AddDays(31).ToUniversalTime());
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                upcoming.Add(new PersonalEvent
                                {
                                    IdEvent = reader["id_event"] != DBNull.Value ? Convert.ToInt64(reader["id_event"]) : 0,
                                    Title = reader["title"]?.ToString() ?? string.Empty,
                                    Location = reader["location"]?.ToString() ?? string.Empty,
                                    StartTime = reader["start_time"] != DBNull.Value ? DateTime.SpecifyKind(Convert.ToDateTime(reader["start_time"]), DateTimeKind.Utc).ToLocalTime() : DateTime.MinValue,
                                    EndTime = reader["end_time"] != DBNull.Value ? DateTime.SpecifyKind(Convert.ToDateTime(reader["end_time"]), DateTimeKind.Utc).ToLocalTime() : DateTime.MinValue,
                                    EventType = reader["event_type"]?.ToString() ?? string.Empty,
                                    IsCompleted = reader["is_completed"] != DBNull.Value && Convert.ToBoolean(reader["is_completed"])
                                });
                            }
                        }
                    }
                }
            }
            catch 
            {
                // Fallback nếu có lỗi
                upcoming = _eventBll.GetUpcoming(idAcc, 31) ?? new System.Collections.Generic.List<PersonalEvent>();
            }

            // Chỉ đếm số lượng Lịch cá nhân cho ô thống kê "Sự kiện hôm nay"
            var today = upcoming.Count(e => e.StartTime.Date == DateTime.Today && e.EventType == "PERSONAL");
            
            // Chỉ hiển thị 7 ngày tới cho thống kê
            var next7Days = upcoming.Where(e => e.StartTime.Date >= DateTime.Today && e.StartTime.Date < DateTime.Today.AddDays(8))
                                    .OrderBy(e => e.StartTime).ToList();

            // Chỉ lấy lịch CÁ NHÂN cho bảng Sự kiện sắp tới
            var personalNext7Days = next7Days.Where(e => e.EventType == "PERSONAL").ToList();

            StatUpcoming.Text = personalNext7Days.Count.ToString();
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
            if (personalNext7Days.Count > 0)
            {
                EventList.ItemsSource = personalNext7Days.Select(e => new EventItem
                {
                    Title = e.Title ?? string.Empty,
                    Location = string.IsNullOrEmpty(e.Location) ? "Không có địa điểm" : e.Location,
                    StartTime = e.StartTime,
                    EventTypeIcon = "📅"
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
                .Select(e => new ScheduleItem
                {
                    TenMonHoc = e.Title ?? string.Empty,
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
                .Select(e => new ScheduleItem
                {
                    TenMonHoc = e.Title ?? string.Empty,
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

            // ── Checklist nhắc nhở 4 ô ──────────────────────────
            // Lấy đủ sự kiện trong tháng hiện tại (31 ngày) - CHỈ LẤY LOẠI NHẮC NHỞ (REMINDER)
            var allForMonth = upcoming.Where(e => e.EventType == "REMINDER").ToList(); 

            // Hôm nay
            LoadChecklist(CheckToday, TxtNoToday,
                allForMonth.Where(e => e.StartTime.Date == DateTime.Today).ToList());

            // Ngày mai
            LoadChecklist(CheckTomorrow, TxtNoTomorrow,
                allForMonth.Where(e => e.StartTime.Date == tomorrow).ToList());

            // Tuần này (7 ngày tới, ngoại trừ hôm nay và ngày mai để tránh trùng lặp)
            DateTime dayAfterTomorrow = DateTime.Today.AddDays(2);
            DateTime endWeek = DateTime.Today.AddDays(7);
            LoadChecklist(CheckWeek, TxtNoWeek,
                allForMonth.Where(e => e.StartTime.Date >= dayAfterTomorrow && e.StartTime.Date < endWeek).ToList());

            // Tháng này (ngoại trừ hôm nay và ngày mai)
            LoadChecklist(CheckMonth, TxtNoMonth,
                allForMonth.Where(e => e.StartTime.Month == DateTime.Today.Month
                                    && e.StartTime.Year == DateTime.Today.Year
                                    && e.StartTime.Date >= dayAfterTomorrow).ToList());
        }

        private void LoadChecklist(ItemsControl ctrl, TextBlock txtEmpty,
            System.Collections.Generic.List<PersonalEvent> events)
        {
            _suppressCheckEvent = true;
            var items = new ObservableCollection<ReminderCheckItem>(
                events.Select(e => new ReminderCheckItem
                {
                    IdEvent = e.IdEvent,
                    Title = e.Title ?? "",
                    IsChecked = e.IsCompleted
                }));
            ctrl.ItemsSource = items;
            txtEmpty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _suppressCheckEvent = false;
        }

        private void OnReminderChecked(object sender, RoutedEventArgs e)
        {
            if (_suppressCheckEvent) return;
            if (sender is CheckBox cb && cb.Tag is long idEvent)
            {
                bool isChecked = cb.IsChecked == true;
                try
                {
                    using var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString);
                    conn.Open();
                    using var cmd = new System.Data.SqlClient.SqlCommand(
                        "UPDATE PERSONAL_EVENT SET is_completed=@v WHERE id_event=@id", conn);
                    cmd.Parameters.AddWithValue("@v", isChecked);
                    cmd.Parameters.AddWithValue("@id", idEvent);
                    cmd.ExecuteNonQuery();
                }
                catch { }
            }
        }

        private void BtnAddEvent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new EventDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) LoadData();
        }
    }
}
