using System;
using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;
using System.Windows.Input;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Media;
using System.Linq;

namespace StudentReminderApp.Views.Dialogs
{
    public partial class EventDialog : Window
    {
        private readonly PersonalEvent _event;
        private readonly EventBLL      _bll = new EventBLL();
        
        // Danh sách khách mời tạm thời
        private class GuestItem { public long IdAcc { get; set; } public string Info { get; set; } public string Status { get; set; } }
        private List<GuestItem> _guestList = new List<GuestItem>();
        private CheckBox _chkCompletedDynamic;

        public class TagSelectionItem
        {
            public long IdTag { get; set; }
            public string TagName { get; set; }
            public bool IsSelected { get; set; }
        }
        private List<TagSelectionItem> _availableTags = new List<TagSelectionItem>();

        public List<long> PreSelectedTagIds { get; set; } = new List<long>();

        public EventDialog(PersonalEvent ev = null, List<long> preSelectedTags = null)
        {
            InitializeComponent();
            PreSelectedTagIds = preSelectedTags ?? new List<long>();
            
            // Nếu ev != null thì là chế độ SỬA, ngược lại là THÊM MỚI
            if (ev != null)
            {
                _event = ev;
                TxtDialogTitle.Text = "Chỉnh sửa sự kiện";
                BtnDelete.Visibility = Visibility.Visible; // Hiện nút Xóa khi sửa
            }
            else
            {
                _event = new PersonalEvent
                {
                    IdAcc     = SessionManager.CurrentAccount.IdAcc,
                    StartTime = DateTime.Now,
                    EndTime   = DateTime.Now.AddHours(1),
                    EventType = "PERSONAL"
                };
                TxtDialogTitle.Text = "Thêm sự kiện";
                BtnDelete.Visibility = Visibility.Collapsed; // Ẩn nút Xóa khi thêm mới
            }

            // Đổ dữ liệu ra giao diện
            LoadDataToUI();
        }

        private void LoadDataToUI()
        {
            TxtEventTitle.Text   = _event.Title;
            TxtLocation.Text     = _event.Location;
            TxtDesc.Text         = _event.Description;
            
            // Chuyển UTC -> Local khi hiển thị
            DateTime localStart = _event.StartTime.Kind == DateTimeKind.Utc ? _event.StartTime.ToLocalTime() : _event.StartTime;
            DateTime localEnd = _event.EndTime.Kind == DateTimeKind.Utc ? _event.EndTime.ToLocalTime() : _event.EndTime;

            DpStart.SelectedDate = localStart.Date;
            DpEnd.SelectedDate   = localEnd.Date;
            TxtStartTime.Text    = localStart.ToString("HH:mm");
            TxtEndTime.Text      = localEnd.ToString("HH:mm");

            // Kiểm tra nếu là sự kiện cả ngày (dựa trên khoảng thời gian)
            if ((localEnd - localStart).TotalHours >= 23 || (localStart.TimeOfDay == TimeSpan.Zero && localEnd.TimeOfDay == TimeSpan.Zero && localEnd > localStart))
            {
                ChkAllDay.IsChecked = true;
            }

            CmbType.SelectionChanged -= CmbType_SelectionChanged;
            // Chọn loại sự kiện trong ComboBox
            foreach (ComboBoxItem item in CmbType.Items)
            {
                if (item.Tag?.ToString() == _event.EventType)
                {
                    CmbType.SelectedItem = item;
                    break;
                }
            }
            CmbType.SelectionChanged += CmbType_SelectionChanged;
            
            foreach (ComboBoxItem item in CmbRecurrence.Items)
            {
                if (item.Tag?.ToString() == (_event.RecurrenceRule ?? "NONE"))
                {
                    CmbRecurrence.SelectedItem = item;
                    break;
                }
            }
            LoadReminders();
            LoadGuests();
            LoadTags();
            
            string color = _event.ColorCategory ?? "#1A73E8";
            if (color == "#D93025") RbColorRed.IsChecked = true;
            else if (color == "#1E8E3E") RbColorGreen.IsChecked = true;
            else if (color == "#F9AB00") RbColorYellow.IsChecked = true;
            else if (color == "#9334E6" || color == "#3F51B5") RbColorPurple.IsChecked = true;
            else if (color == "#009688") RbColorTeal.IsChecked = true;
            else RbColorBlue.IsChecked = true;
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadReminders();
            LoadTags();
        }

        private void TagCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateTagsComboBoxText();
        }

        private void UpdateTagsComboBoxText()
        {
            if (TagsDropdownToggle == null) return;
            var selected = _availableTags.Where(t => t.IsSelected).Select(t => t.TagName).ToList();
            if (selected.Count == 0) TagsDropdownToggle.Tag = "Chọn Tag...";
            else if (selected.Count == 1) TagsDropdownToggle.Tag = selected[0];
            else TagsDropdownToggle.Tag = $"{selected[0]} (+{selected.Count - 1})";
        }

        private void LoadTags()
        {
            string eventType = ((ComboBoxItem)CmbType.SelectedItem)?.Tag?.ToString() ?? "PERSONAL";
            var dal = new StudentReminderApp.DAL.EventDAL();
            var allTags = dal.GetTags(SessionManager.CurrentAccount.IdAcc)
                             .Where(t => t.TagType == eventType).ToList();
            
            var selectedTagIds = new List<long>();
            if (_event.IdEvent > 0)
            {
                selectedTagIds = dal.GetTagIdsForEvent(_event.IdEvent);
            }
            else if (PreSelectedTagIds != null && PreSelectedTagIds.Count > 0)
            {
                selectedTagIds = PreSelectedTagIds;
            }

            _availableTags.Clear();
            foreach (var t in allTags)
            {
                _availableTags.Add(new TagSelectionItem { IdTag = t.IdTag, TagName = t.TagName, IsSelected = selectedTagIds.Contains(t.IdTag) });
            }
            
            TagsListControl.ItemsSource = null;
            TagsListControl.ItemsSource = _availableTags;
            UpdateTagsComboBoxText();
        }

        private void LoadReminders()
        {
            ReminderPanel.Children.Clear();
            
            // THÊM CHECKBOX HOÀN THÀNH (ĐỘNG) CHO NHẮC NHỞ
            if (((ComboBoxItem)CmbType.SelectedItem)?.Tag?.ToString() == "REMINDER")
            {
                _chkCompletedDynamic = new CheckBox {
                    Content = "Đánh dấu đã hoàn thành",
                    IsChecked = _event.IsCompleted,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(63, 81, 181)), // #3F51B5
                    Margin = new Thickness(0, 0, 0, 15)
                };
                ReminderPanel.Children.Add(_chkCompletedDynamic);
            }
            else
            {
                _chkCompletedDynamic = null;
            }

            if (_event.IdEvent > 0)
            {
                try {
                    using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT minutes_before FROM EVENT_REMINDER WHERE id_event = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", _event.IdEvent);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int totalMins = Convert.ToInt32(reader["minutes_before"]);
                                    if (totalMins % 1440 == 0) AddReminderRow(totalMins / 1440, "DAYS");
                                    else if (totalMins % 60 == 0) AddReminderRow(totalMins / 60, "HOURS");
                                    else AddReminderRow(totalMins, "MINUTES");
                                }
                            }
                        }
                    }
                } catch { }
            }
            if (ReminderPanel.Children.Count == 0) AddReminderRow(15, "MINUTES");
        }

        private void AddReminderRow(int value, string unit)
        {
            var grid = new Grid { Margin = new Thickness(0,0,0,8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var txtValue = new TextBox { Text = value.ToString(), Height = 38, Margin = new Thickness(0,0,8,0), VerticalContentAlignment = VerticalAlignment.Center, Style = (Style)FindResource("InputModern") };
            var cmbUnit = new ComboBox { Height = 38, Margin = new Thickness(0,0,8,0) };
            cmbUnit.Items.Add(new ComboBoxItem { Content = "Phút", Tag = "MINUTES" });
            cmbUnit.Items.Add(new ComboBoxItem { Content = "Giờ", Tag = "HOURS" });
            cmbUnit.Items.Add(new ComboBoxItem { Content = "Ngày", Tag = "DAYS" });
            foreach(ComboBoxItem item in cmbUnit.Items) { if (item.Tag?.ToString() == unit) item.IsSelected = true; }
            if (cmbUnit.SelectedIndex == -1) cmbUnit.SelectedIndex = 0;
            var btnDel = new Button { Content = "✕", Width = 38, Height = 38, Background = System.Windows.Media.Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            btnDel.Click += (s, ev) => ReminderPanel.Children.Remove(grid);
            Grid.SetColumn(txtValue, 0); Grid.SetColumn(cmbUnit, 1); Grid.SetColumn(btnDel, 2);
            grid.Children.Add(txtValue); grid.Children.Add(cmbUnit); grid.Children.Add(btnDel);
            ReminderPanel.Children.Add(grid);
        }

        private void BtnAddReminder_Click(object sender, RoutedEventArgs e) => AddReminderRow(15, "MINUTES");

        // ============================================================
        // LOGIC KHÁCH MỜI VÀ CẢNH BÁO XUNG ĐỘT (FREE/BUSY)
        // ============================================================
        private void LoadGuests()
        {
            if (_event.IdEvent == 0) return;
            try
            {
                using (var conn = new SqlConnection(AppConfig.ConnectionString)) {
                    conn.Open();
                    string sql = "SELECT a.id_acc, a.response_status, u.ho_ten, acc.username FROM EVENT_ATTENDEE a JOIN [USER] u ON a.id_acc = u.id_acc JOIN ACCOUNT acc ON a.id_acc = acc.id_acc WHERE a.id_event = @id";
                    using (var cmd = new SqlCommand(sql, conn)) {
                        cmd.Parameters.AddWithValue("@id", _event.IdEvent);
                        using (var reader = cmd.ExecuteReader()) {
                            while (reader.Read()) {
                                _guestList.Add(new GuestItem { IdAcc = Convert.ToInt64(reader["id_acc"]), Info = $"{reader["ho_ten"]} ({reader["username"]})", Status = reader["response_status"].ToString() });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi LoadGuests: " + ex.Message);
            }
            RenderGuestList();
        }

        private void BtnAddGuest_Click(object sender, RoutedEventArgs e)
        {
            string search = TxtGuestSearch.Text.Trim();
            if (string.IsNullOrEmpty(search)) return;

            // Tính mốc UTC để check conflict
            DateTime startUtc = ((DpStart.SelectedDate ?? DateTime.Today).Date.Add(TimeSpan.Parse(TxtStartTime.Text))).ToUniversalTime();
            DateTime endUtc = ((DpEnd.SelectedDate ?? DateTime.Today).Date.Add(TimeSpan.Parse(TxtEndTime.Text))).ToUniversalTime();

            using (var conn = new SqlConnection(AppConfig.ConnectionString)) {
                conn.Open();
                // 1. Tìm User
                string sqlFind = "SELECT u.id_acc, u.ho_ten, a.username FROM [USER] u JOIN ACCOUNT a ON u.id_acc = a.id_acc WHERE a.username = @s OR u.ho_ten LIKE '%' + @s + '%'";
                long targetId = 0; string info = "";
                using (var cmd = new SqlCommand(sqlFind, conn)) {
                    cmd.Parameters.AddWithValue("@s", search);
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            targetId = Convert.ToInt64(reader["id_acc"]);
                            info = $"{reader["ho_ten"]} ({reader["username"]})";
                        }
                    }
                }
                if (targetId == 0 || targetId == SessionManager.CurrentAccount.IdAcc) { ShowError("Không tìm thấy người dùng hợp lệ."); return; }
                if (_guestList.Exists(g => g.IdAcc == targetId)) return;

                // 2. Check Xung đột (Conflict Warning)
                string sqlConflict = @"SELECT COUNT(*) FROM PERSONAL_EVENT e LEFT JOIN EVENT_ATTENDEE a ON e.id_event = a.id_event 
                                       WHERE (e.id_acc = @uid OR (a.id_acc = @uid AND a.response_status = 'ACCEPTED')) 
                                       AND e.start_time < @end AND e.end_time > @start";
                bool isConflict = false;
                using (var cmdC = new SqlCommand(sqlConflict, conn)) {
                    cmdC.Parameters.AddWithValue("@uid", targetId);
                    cmdC.Parameters.AddWithValue("@start", startUtc);
                    cmdC.Parameters.AddWithValue("@end", endUtc);
                    isConflict = (int)cmdC.ExecuteScalar() > 0;
                }

                _guestList.Add(new GuestItem { IdAcc = targetId, Info = info, Status = "PENDING" });
                RenderGuestList();
                TxtGuestSearch.Text = "";

                if (isConflict) ShowError($"Cảnh báo: {info} đang bị trùng lịch vào khung giờ này!");
            }
        }

        private void RenderGuestList()
        {
            GuestListPanel.Children.Clear();
            foreach (var g in _guestList) {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };
                string icon = g.Status == "ACCEPTED" ? "✅" : g.Status == "DECLINED" ? "❌" : "⏳";
                sp.Children.Add(new TextBlock { Text = $"{icon} {g.Info}", Foreground = new SolidColorBrush(Color.FromRgb(60,64,67)), VerticalAlignment = VerticalAlignment.Center });
                GuestListPanel.Children.Add(sp);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TxtErr.Visibility = Visibility.Collapsed;

            // 1. Kiểm tra tiêu đề
            if (string.IsNullOrWhiteSpace(TxtEventTitle.Text))
            {
                ShowError("Tiêu đề không được để trống");
                return;
            }
            
            // 2. Kiểm tra định dạng giờ
            TimeSpan startTime = TimeSpan.Zero;
            TimeSpan endTime = new TimeSpan(23, 59, 59);

            if (ChkAllDay.IsChecked != true)
            {
                if (!TimeSpan.TryParse(TxtStartTime.Text, out startTime) || 
                    !TimeSpan.TryParse(TxtEndTime.Text, out endTime))
                {
                    ShowError("Giờ sai định dạng HH:mm (VD: 08:30)");
                    return;
                }
            }

            // 3. Gộp ngày và giờ
            DateTime start = (DpStart.SelectedDate ?? DateTime.Today).Date.Add(startTime);
            DateTime end   = (DpEnd.SelectedDate ?? start.Date).Date.Add(endTime);

            if (end <= start)
            {
                ShowError("Giờ kết thúc phải sau giờ bắt đầu");
                return;
            }

            // 4. Chuyển sang chuẩn UTC trước khi lưu xuống DB
            _event.Title       = TxtEventTitle.Text;
            _event.Location    = TxtLocation.Text;
            _event.Description = TxtDesc.Text;
            _event.StartTime   = start.ToUniversalTime();
            _event.EndTime     = end.ToUniversalTime();
            _event.EventType   = ((ComboBoxItem)CmbType.SelectedItem)?.Tag?.ToString() ?? "PERSONAL";
            
            if (_chkCompletedDynamic != null) _event.IsCompleted = _chkCompletedDynamic.IsChecked == true;

            if (RbColorRed.IsChecked == true) _event.ColorCategory = "#D93025";
            else if (RbColorGreen.IsChecked == true) _event.ColorCategory = "#1E8E3E";
            else if (RbColorYellow.IsChecked == true) _event.ColorCategory = "#F9AB00";
            else if (RbColorPurple.IsChecked == true) _event.ColorCategory = _event.EventType == "REMINDER" ? "#3F51B5" : "#9334E6";
            else if (RbColorTeal.IsChecked == true) _event.ColorCategory = "#009688";
            else _event.ColorCategory = "#1A73E8";

            // 5. Lưu (BLL sẽ tự check: nếu Id > 0 thì UPDATE, Id = 0 thì INSERT)
            var (ok, msg) = _bll.Save(_event);
            if (!ok) 
            { 
                ShowError(msg); 
                return; 
            }

            SaveRemindersAndRecurrence();
            this.DialogResult = true; 
        }

        private void SaveRemindersAndRecurrence()
        {
            long eventId = _event.IdEvent;
            if (eventId == 0) // Lấy ID mới nhất nếu vừa Insert
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString)) {
                    conn.Open();
                    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT TOP 1 id_event FROM PERSONAL_EVENT WHERE id_acc = @uid ORDER BY id_event DESC", conn)) {
                        cmd.Parameters.AddWithValue("@uid", _event.IdAcc);
                        var obj = cmd.ExecuteScalar();
                        if (obj != null) eventId = Convert.ToInt64(obj);
                    }
                }
            }
            if (eventId == 0) return;
            string recRule = ((ComboBoxItem)CmbRecurrence.SelectedItem)?.Tag?.ToString() ?? "NONE";
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString)) {
                    conn.Open();
                    using (var cmd = new System.Data.SqlClient.SqlCommand("UPDATE PERSONAL_EVENT SET recurrence_rule = @rr WHERE id_event = @id", conn)) {
                        cmd.Parameters.AddWithValue("@rr", recRule);
                        cmd.Parameters.AddWithValue("@id", eventId);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmdDel = new System.Data.SqlClient.SqlCommand("DELETE FROM EVENT_REMINDER WHERE id_event = @id", conn)) {
                        cmdDel.Parameters.AddWithValue("@id", eventId);
                        cmdDel.ExecuteNonQuery();
                    }
                    foreach (Grid row in ReminderPanel.Children) {
                        if (row.Children[0] is TextBox txt && int.TryParse(txt.Text, out int val)) {
                            string unit = ((ComboBoxItem)((ComboBox)row.Children[1]).SelectedItem)?.Tag?.ToString() ?? "MINUTES";
                            int mins = val;
                            if (unit == "HOURS") mins *= 60;
                            if (unit == "DAYS") mins *= 1440;
                            using (var cmdIns = new System.Data.SqlClient.SqlCommand("INSERT INTO EVENT_REMINDER (id_event, minutes_before) VALUES (@id, @m)", conn)) {
                                cmdIns.Parameters.AddWithValue("@id", eventId);
                                cmdIns.Parameters.AddWithValue("@m", mins);
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                    }
                    
                    // Lưu Khách mời
                    using (var cmdDel = new SqlCommand("DELETE FROM EVENT_ATTENDEE WHERE id_event = @id", conn)) {
                        cmdDel.Parameters.AddWithValue("@id", eventId); cmdDel.ExecuteNonQuery();
                    }
                    foreach (var g in _guestList) {
                        using (var cmdIns = new SqlCommand("INSERT INTO EVENT_ATTENDEE (id_event, id_acc, response_status) VALUES (@id, @uid, @st)", conn)) {
                            cmdIns.Parameters.AddWithValue("@id", eventId);
                            cmdIns.Parameters.AddWithValue("@uid", g.IdAcc);
                            cmdIns.Parameters.AddWithValue("@st", g.Status);
                            cmdIns.ExecuteNonQuery();
                        }
                    }
                }
                
                // Lưu Tags
                var dal = new StudentReminderApp.DAL.EventDAL();
                var selectedTagIds = new List<long>();
                foreach(var t in _availableTags) {
                    if (t.IsSelected) selectedTagIds.Add(t.IdTag);
                }
                dal.SaveTagIdsForEvent(eventId, selectedTagIds);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi SaveRemindersAndRecurrence: " + ex.Message);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Xóa sự kiện này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                _bll.Delete(_event.IdEvent);
                this.DialogResult = true; // Đóng và báo cho CalendarPage Render lại
            }
        }

        private void ShowError(string msg)
        {
            TxtErr.Text = "⚠ " + msg;
            TxtErr.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;

        private void ChkAllDay_Checked(object sender, RoutedEventArgs e)
        {
            if (TxtStartTime != null) TxtStartTime.Visibility = Visibility.Collapsed;
            if (TxtEndTime != null) TxtEndTime.Visibility = Visibility.Collapsed;
        }

        private void ChkAllDay_Unchecked(object sender, RoutedEventArgs e)
        {
            if (TxtStartTime != null) TxtStartTime.Visibility = Visibility.Visible;
            if (TxtEndTime != null) TxtEndTime.Visibility = Visibility.Visible;
        }
    }
}