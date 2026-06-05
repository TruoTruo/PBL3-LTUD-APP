using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Threading;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;
using StudentReminderApp.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StudentReminderApp.Services
{
    public class NotificationService
    {
        private static DispatcherTimer _timer;
        private static HashSet<string> _notifiedKeys = new HashSet<string>();
        
        private class SnoozedItem {
            public string Title { get; set; }
            public string Body { get; set; }
            public DateTime AlertTime { get; set; }
        }
        private static List<SnoozedItem> _snoozed = new List<SnoozedItem>();

        public static void Start()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _timer.Tick += CheckReminders;
            _timer.Start();
        }

        private static void CheckReminders(object sender, EventArgs e)
        {
            if (SessionManager.CurrentAccount == null) return;
            long currentUserId = SessionManager.CurrentAccount.IdAcc;
            DateTime nowLocal = DateTime.Now;
            DateTime nowUtc = DateTime.UtcNow; // Sử dụng UTC chuẩn Quốc tế

            for (int i = _snoozed.Count - 1; i >= 0; i--)
            {
                if (nowLocal >= _snoozed[i].AlertTime) {
                    ShowToast(_snoozed[i].Title, _snoozed[i].Body);
                    _snoozed.RemoveAt(i);
                }
            }

            string sql = @"SELECT e.id_event, e.title, e.start_time, e.location, r.minutes_before FROM PERSONAL_EVENT e JOIN EVENT_REMINDER r ON e.id_event = r.id_event WHERE e.id_acc = @uid AND e.start_time >= @now AND e.start_time <= DATEADD(day, 2, @now)";

            using (var conn = new SqlConnection(AppConfig.ConnectionString))
            {
                try {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn)) {
                        cmd.Parameters.AddWithValue("@uid", currentUserId);
                        cmd.Parameters.AddWithValue("@now", nowUtc);
                        using (var reader = cmd.ExecuteReader()) {
                            while (reader.Read()) {
                                long id = Convert.ToInt64(reader["id_event"]);
                                int mins = Convert.ToInt32(reader["minutes_before"]);
                                
                                // Đọc giờ UTC từ DB và chuyển về giờ Local
                                DateTime startUtc = DateTime.SpecifyKind(Convert.ToDateTime(reader["start_time"]), DateTimeKind.Utc);
                                DateTime startLocal = startUtc.ToLocalTime();
                                DateTime reminderTimeUtc = startUtc.AddMinutes(-mins);
                                
                                // So sánh thời gian theo UTC
                                if (nowUtc >= reminderTimeUtc && nowUtc <= reminderTimeUtc.AddMinutes(5)) {
                                    string key = $"{id}_{mins}";
                                    if (!_notifiedKeys.Contains(key)) {
                                        _notifiedKeys.Add(key);
                                        ShowToast(reader["title"].ToString(), $"Sắp diễn ra lúc {startLocal:HH:mm} ({mins} phút nữa)\nTại: {reader["location"]}");
                                    }
                                }
                            }
                        }
                    }
                } catch { }
            }
        }

        private static void ShowToast(string title, string body)
        {
            App.Current.Dispatcher.Invoke(() => {
                var toast = new NotificationPopup(title, body);
                if (toast.Content is Border border && border.Child is Panel panel) {
                    var btnSnooze = new Button { Content = "Nhắc lại sau 5 phút", Margin = new Thickness(0, 10, 0, 0), Background = Brushes.Transparent, Foreground = new SolidColorBrush(Color.FromRgb(26, 115, 232)), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right };
                    btnSnooze.Click += (s, ev) => { 
                        _snoozed.Add(new SnoozedItem { Title = title, Body = body, AlertTime = DateTime.Now.AddMinutes(5) });
                        toast.Close(); 
                    };
                    panel.Children.Add(btnSnooze);
                }
                toast.Show();
            });
        }
    }
}