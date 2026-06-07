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
            public int SnoozeMins { get; set; }
        }
        private static List<SnoozedItem> _snoozed = new List<SnoozedItem>();

        public static void Start()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
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
                    ShowToast(_snoozed[i].Title, _snoozed[i].Body, _snoozed[i].SnoozeMins);
                    _snoozed.RemoveAt(i);
                }
            }

            string sql = @"
                SELECT e.id_event, e.title, e.description, e.start_time, e.end_time, e.location, 
                       COALESCE(r.minutes_before, 15) as minutes_before,
                       COALESCE(c.mins_before, 5) as snooze_mins,
                       COALESCE(c.channel, 'PUSH') as channel,
                       u.email, ISNULL(c.is_enabled, 1) as is_enabled
                FROM PERSONAL_EVENT e 
                LEFT JOIN EVENT_REMINDER r ON e.id_event = r.id_event 
                LEFT JOIN REMINDER_CONFIG c ON e.id_acc = c.id_acc
                LEFT JOIN [USER] u ON e.id_acc = u.id_acc
                WHERE e.id_acc = @uid AND e.start_time >= @now AND e.start_time <= DATEADD(day, 2, @now)
                AND (c.is_enabled = 1 OR r.minutes_before IS NOT NULL)";

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
                                int snoozeMins = Convert.ToInt32(reader["snooze_mins"]);
                                string channel = reader["channel"].ToString();
                                string email = reader["email"]?.ToString();
                                
                                DateTime startUtc = DateTime.SpecifyKind(Convert.ToDateTime(reader["start_time"]), DateTimeKind.Utc);
                                DateTime startLocal = startUtc.ToLocalTime();
                                DateTime endUtc = reader["end_time"] != DBNull.Value ? DateTime.SpecifyKind(Convert.ToDateTime(reader["end_time"]), DateTimeKind.Utc) : startUtc.AddHours(1);
                                DateTime endLocal = endUtc.ToLocalTime();
                                DateTime reminderTimeUtc = startUtc.AddMinutes(-mins);
                                
                                if (nowUtc >= reminderTimeUtc && nowUtc <= reminderTimeUtc.AddMinutes(5)) {
                                    string key = $"{id}_{mins}";
                                    if (!_notifiedKeys.Contains(key)) {
                                        _notifiedKeys.Add(key);
                                        string msgBody = $"Sắp diễn ra lúc {startLocal:HH:mm} ({mins} phút nữa)\nTại: {reader["location"]}";
                                        
                                        bool sendEmail = (channel == "EMAIL" || channel == "BOTH") && !string.IsNullOrWhiteSpace(email);
                                        bool showPush = channel == "PUSH" || channel == "BOTH" || (channel == "EMAIL" && string.IsNullOrWhiteSpace(email));

                                        if (sendEmail) {
                                            string title = reader["title"].ToString();
                                            string location = reader["location"]?.ToString();
                                            if (string.IsNullOrWhiteSpace(location)) location = "Không có địa điểm";
                                            string description = reader["description"]?.ToString();
                                            if (string.IsNullOrWhiteSpace(description)) description = "Không có";
                                            else description = description.Replace("\n", "<br/>");
                                            string htmlBody = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;max-width:480px;margin:auto;border:1px solid #E5E7EB;border-radius:12px;overflow:hidden'>
  <div style='background:#4F46E5;padding:24px 32px'>
    <h2 style='color:white;margin:0'>⏰ Nhắc nhở sự kiện</h2>
  </div>
  <div style='padding:32px'>
    <h3 style='color:#1F2937;margin-top:0;font-size:20px'>{title}</h3>
    <p style='font-size:15px;color:#4B5563'>Sự kiện của bạn sẽ diễn ra sau <strong>{mins} phút</strong> nữa.</p>
    <div style='background:#F3F4F6;border-radius:8px;padding:16px;margin:24px 0'>
      <p style='margin:0 0 12px 0;color:#374151'><strong>Thời gian:</strong> {startLocal:HH:mm} - {endLocal:HH:mm} ({startLocal:dd/MM/yyyy})</p>
      <p style='margin:0 0 12px 0;color:#374151'><strong>Địa điểm:</strong> {location}</p>
      <p style='margin:0;color:#374151'><strong>Mô tả:</strong> {description}</p>
    </div>
    <p style='font-size:13px;color:#9CA3AF;margin-bottom:0'>Email gửi tự động từ Student Reminder App.</p>
  </div>
</div>";
                                            EmailService.SendEmail(email, $"[Nhắc nhở] {title}", htmlBody);
                                        }
                                        
                                        if (showPush) {
                                            ShowToast(reader["title"].ToString(), msgBody, snoozeMins);
                                        }
                                    }
                                }
                            }
                        }
                    }
                } catch { }
            }
        }

        private static void ShowToast(string title, string body, int snoozeMins)
        {
            App.Current.Dispatcher.Invoke(() => {
                var toast = new NotificationPopup(title, body);
                if (toast.Content is Border border && border.Child is Panel panel) {
                var btnSnooze = new Button { Content = $"⏰ Nhắc lại sau {snoozeMins} phút", Margin = new Thickness(0, 15, 0, 0), Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)), Padding = new Thickness(10, 8, 10, 8), FontWeight = FontWeights.SemiBold, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Stretch };
                    btnSnooze.Click += (s, ev) => { 
                        _snoozed.Add(new SnoozedItem { Title = title, Body = body, AlertTime = DateTime.Now.AddMinutes(snoozeMins), SnoozeMins = snoozeMins });
                        toast.Close(); 
                    };
                    panel.Children.Add(btnSnooze);
                }
                toast.Show();
            });
        }
    }
}