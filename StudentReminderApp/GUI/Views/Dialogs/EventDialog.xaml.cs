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

            // Kiểm tra nếu là sự kiện cả ngày (dựa trên khoảng thời gian)
            bool isAllDay = (localEnd - localStart).TotalHours >= 23 || (localStart.TimeOfDay == TimeSpan.Zero && localEnd.TimeOfDay == TimeSpan.Zero && localEnd > localStart);

            SchedulePanel.Children.Clear();
            AddScheduleRow(localStart.Date, localStart.TimeOfDay, localEnd.Date, localEnd.TimeOfDay, isAllDay);

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

            // Update Label GuestOrCourse
            UpdateGuestOrCourseLabel();
        }

        private void AddScheduleRow(DateTime start, TimeSpan startTime, DateTime end, TimeSpan endTime, bool isAllDay)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var mainStack = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
            Grid.SetColumn(mainStack, 0);
            
            var stCol1 = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            var grid1 = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            grid1.Children.Add(new TextBlock { Text = "BẮT ĐẦU *", Style = (Style)FindResource("FieldLabel"), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0) });
            var chkAllDay = new CheckBox { Content = "Cả ngày", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(95, 99, 104)), IsChecked = isAllDay };
            grid1.Children.Add(chkAllDay);
            stCol1.Children.Add(grid1);

            var grid2 = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var dpStart = new DatePicker { Height = 36, Margin = new Thickness(0, 0, 16, 0), BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)), Background = Brushes.Transparent, SelectedDate = start };
            Grid.SetColumn(dpStart, 0);
            grid2.Children.Add(dpStart);

            var timeStartPanel = CreateTimePickerPanel(startTime, "Start");
            Grid.SetColumn(timeStartPanel, 1);
            grid2.Children.Add(timeStartPanel);
            stCol1.Children.Add(grid2);

            var stCol2 = new StackPanel { };
            stCol2.Children.Add(new TextBlock { Text = "KẾT THÚC *", Style = (Style)FindResource("FieldLabel"), Margin = new Thickness(0,0,0,4) });
            
            var grid3 = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            grid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid3.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var dpEnd = new DatePicker { Height = 36, Margin = new Thickness(0, 0, 16, 0), BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)), Background = Brushes.Transparent, SelectedDate = end };
            Grid.SetColumn(dpEnd, 0);
            grid3.Children.Add(dpEnd);

            var timeEndPanel = CreateTimePickerPanel(endTime, "End");
            Grid.SetColumn(timeEndPanel, 1);
            grid3.Children.Add(timeEndPanel);
            stCol2.Children.Add(grid3);

            mainStack.Children.Add(stCol1);
            mainStack.Children.Add(stCol2);
            grid.Children.Add(mainStack);

            var btnDel = new Button { Content = "✕", Width = 38, Height = 38, Background = Brushes.Transparent, Foreground = Brushes.Red, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(btnDel, 1);
            btnDel.Click += (s, ev) => SchedulePanel.Children.Remove(grid);
            grid.Children.Add(btnDel);
            
            chkAllDay.Checked += (s, ev) => { timeStartPanel.Visibility = Visibility.Collapsed; timeEndPanel.Visibility = Visibility.Collapsed; };
            chkAllDay.Unchecked += (s, ev) => { timeStartPanel.Visibility = Visibility.Visible; timeEndPanel.Visibility = Visibility.Visible; };
            if (isAllDay) { timeStartPanel.Visibility = Visibility.Collapsed; timeEndPanel.Visibility = Visibility.Collapsed; }

            dpStart.SelectedDateChanged += (s, ev) => {
                if (ev.AddedItems.Count > 0 && ev.RemovedItems.Count > 0) {
                    DateTime newStart = (DateTime)ev.AddedItems[0];
                    DateTime oldStart = (DateTime)ev.RemovedItems[0];
                    TimeSpan diff = newStart - oldStart;
                    if (dpEnd.SelectedDate.HasValue) dpEnd.SelectedDate = dpEnd.SelectedDate.Value.AddDays(diff.Days);
                } else if (dpStart.SelectedDate.HasValue && !dpEnd.SelectedDate.HasValue) {
                    dpEnd.SelectedDate = dpStart.SelectedDate;
                }
            };

            SchedulePanel.Children.Add(grid);
        }

        private StackPanel CreateTimePickerPanel(TimeSpan time, string prefix)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var style = (Style)FindResource("ModernComboBoxStyle");
            var itemStyle = (Style)FindResource("ModernComboBoxItemStyle");
            var bg = (Brush)FindResource("PrimaryLightBrush");
            var fg = (Brush)FindResource("TextPrimaryBrush");

            var cmbHour = new ComboBox { Name = prefix + "Hour", Width = 64, Height = 36, Padding = new Thickness(8), BorderThickness = new Thickness(0), Background = bg, Foreground = fg, FontSize = 14, Style = style, ItemContainerStyle = itemStyle, IsEditable = true, IsReadOnly = true };
            var cmbMin = new ComboBox { Name = prefix + "Min", Width = 64, Height = 36, Padding = new Thickness(8), BorderThickness = new Thickness(0), Background = bg, Foreground = fg, FontSize = 14, Style = style, ItemContainerStyle = itemStyle, IsEditable = true, IsReadOnly = true };
            var cmbSec = new ComboBox { Name = prefix + "Sec", Width = 64, Height = 36, Padding = new Thickness(8), BorderThickness = new Thickness(0), Background = bg, Foreground = fg, FontSize = 14, Style = style, ItemContainerStyle = itemStyle, IsEditable = true, IsReadOnly = true };

            for (int i = 0; i < 24; i++) cmbHour.Items.Add(new ComboBoxItem { Content = i.ToString("D2") });
            for (int i = 0; i < 60; i++) cmbMin.Items.Add(new ComboBoxItem { Content = i.ToString("D2") });
            for (int i = 0; i < 60; i++) cmbSec.Items.Add(new ComboBoxItem { Content = i.ToString("D2") });

            cmbHour.Text = time.Hours.ToString("D2");
            cmbMin.Text = time.Minutes.ToString("D2");
            cmbSec.Text = time.Seconds.ToString("D2");

            panel.Children.Add(cmbHour);
            panel.Children.Add(new TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,4,0), FontWeight = FontWeights.Bold });
            panel.Children.Add(cmbMin);
            panel.Children.Add(new TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,4,0), FontWeight = FontWeights.Bold });
            panel.Children.Add(cmbSec);

            return panel;
        }

        private TimeSpan GetTimeFromPanel(StackPanel panel)
        {
            int h = 0, m = 0, s = 0;
            if (panel.Children.Count >= 5) {
                if (panel.Children[0] is ComboBox cbH && int.TryParse(cbH.Text, out int parsedH)) h = parsedH;
                if (panel.Children[2] is ComboBox cbM && int.TryParse(cbM.Text, out int parsedM)) m = parsedM;
                if (panel.Children[4] is ComboBox cbS && int.TryParse(cbS.Text, out int parsedS)) s = parsedS;
            }
            return new TimeSpan(h, m, s);
        }

        private void BtnAddSchedule_Click(object sender, RoutedEventArgs e)
        {
            AddScheduleRow(DateTime.Today, new TimeSpan(8, 0, 0), DateTime.Today, new TimeSpan(9, 0, 0), false);
        }

        private void UpdateGuestOrCourseLabel()
        {
            if (LblGuestOrCourse != null)
            {
                if (((ComboBoxItem)CmbType.SelectedItem)?.Tag?.ToString() == "ACADEMIC")
                {
                    LblGuestOrCourse.Text = "MÃ HP (Nhập mã học phần để tự động điền)";
                }
                else
                {
                    LblGuestOrCourse.Text = "KHÁCH MỜI (Nhập MSSV hoặc Tên)";
                }
            }
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadReminders();
            LoadTags();
            UpdateGuestOrCourseLabel();
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
            var cmbUnit = new ComboBox { Height = 38, Margin = new Thickness(0,0,8,0), Style = (Style)FindResource("ModernComboBoxStyle"), ItemContainerStyle = (Style)FindResource("ModernComboBoxItemStyle") };
            cmbUnit.Items.Add(new ComboBoxItem { Content = "Phút", Tag = "MINUTES" });
            cmbUnit.Items.Add(new ComboBoxItem { Content = "Giờ", Tag = "HOURS" });
            cmbUnit.Items.Add(new ComboBoxItem { Content = "Ngày", Tag = "DAYS" });
            foreach(ComboBoxItem item in cmbUnit.Items) { if (item.Tag?.ToString() == unit) item.IsSelected = true; }
            if (cmbUnit.SelectedIndex == -1) cmbUnit.SelectedIndex = 0;
            var btnDel = new Button { Content = "✕", Width = 38, Height = 38, Background = System.Windows.Media.Brushes.Transparent, Foreground = System.Windows.Media.Brushes.Red, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
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

            if (((ComboBoxItem)CmbType.SelectedItem)?.Tag?.ToString() == "ACADEMIC")
            {
                // Auto fill from JSON
                try
                {
                    string jsonPath = AppConfig.TkbHK2JsonPath;
                    if (System.IO.File.Exists(jsonPath))
                    {
                        string jsonContent = System.IO.File.ReadAllText(jsonPath);
                        var root = Newtonsoft.Json.Linq.JObject.Parse(jsonContent);
                        var classes = root["classes"] as Newtonsoft.Json.Linq.JArray;
                        bool found = false;
                        if (classes != null)
                        {
                            foreach (var cls in classes)
                            {
                                var courses = cls["courses"] as Newtonsoft.Json.Linq.JArray;
                                if (courses != null)
                                {
                                    foreach (var c in courses)
                                    {
                                        if (c["id"]?.ToString() == search)
                                        {
                                            TxtEventTitle.Text = c["name"]?.ToString();
                                            TxtDesc.Text = "GV: " + c["lecturer_name"]?.ToString() + "\nNhóm: " + c["group"]?.ToString();
                                            
                                            // Tìm địa điểm và thời gian từ các cột thứ
                                            string[] days = {"thu2", "thu3", "thu4", "thu5", "thu6", "thu7", "cn"};
                                            
                                            SchedulePanel.Children.Clear();
                                            bool hasAnySchedule = false;

                                            string weeksStr = c["weeks"]?.ToString();
                                            var weekRanges = new List<(int start, int end)>();
                                            if (!string.IsNullOrEmpty(weeksStr)) {
                                                foreach (var range in weeksStr.Split(';')) {
                                                    var match = System.Text.RegularExpressions.Regex.Match(range, @"(\d+)-?(\d*)");
                                                    if (match.Success) {
                                                        int startW = int.Parse(match.Groups[1].Value);
                                                        int endW = match.Groups[2].Success && !string.IsNullOrEmpty(match.Groups[2].Value) ? int.Parse(match.Groups[2].Value) : startW;
                                                        weekRanges.Add((startW, endW));
                                                    }
                                                }
                                            }
                                            if (weekRanges.Count == 0) weekRanges.Add((22, 22)); // fallback

                                            DateTime firstWeekStart = DateTime.Today;
                                            try {
                                                string twPath = AppConfig.TimeWeekStartJsonPath;
                                                if (System.IO.File.Exists(twPath)) {
                                                    var twRoot = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(twPath));
                                                    string startDateStr = twRoot["firstWeekStartDate"]?.ToString();
                                                    if (DateTime.TryParse(startDateStr, out DateTime parsedFirst)) {
                                                        firstWeekStart = parsedFirst;
                                                    }
                                                }
                                            } catch { }

                                            for (int i = 0; i < days.Length; i++) {
                                                string val = c[days[i]]?.ToString();
                                                if (!string.IsNullOrEmpty(val)) {
                                                    var parts = val.Split(',');
                                                    string periodStr = parts[0].Trim();
                                                    if (parts.Length > 1 && string.IsNullOrEmpty(TxtLocation.Text)) {
                                                        TxtLocation.Text = parts[1].Trim();
                                                    }
                                                    int dayOfWeekOffset = i;

                                                    int startP = 0, endP = 0;
                                                    var pParts = periodStr.Split('-');
                                                    if (pParts.Length == 2 && int.TryParse(pParts[0], out startP) && int.TryParse(pParts[1], out endP)) { }
                                                    else if (pParts.Length == 1 && int.TryParse(pParts[0], out startP)) { endP = startP; }
                                                    
                                                    TimeSpan tStart = new TimeSpan(7,0,0);
                                                    TimeSpan tEnd = new TimeSpan(9,0,0);
                                                    if (startP > 0) {
                                                        try {
                                                            string timeJsonPath = AppConfig.TimePeriodJsonPath;
                                                            if (System.IO.File.Exists(timeJsonPath)) {
                                                                var tRoot = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(timeJsonPath));
                                                                var periods = tRoot["periods"] as Newtonsoft.Json.Linq.JArray;
                                                                if (periods != null) {
                                                                    foreach(var p in periods) {
                                                                        int pNum = (int?)p["period"] ?? 0;
                                                                        if (pNum == startP) TimeSpan.TryParse(p["startTime"]?.ToString(), out tStart);
                                                                        if (pNum == endP) TimeSpan.TryParse(p["endTime"]?.ToString(), out tEnd);
                                                                    }
                                                                }
                                                            }
                                                        } catch { }
                                                    }

                                                    foreach (var range in weekRanges) {
                                                        DateTime targetStart = firstWeekStart.AddDays((range.start - 1) * 7 + dayOfWeekOffset);
                                                        DateTime targetEnd = firstWeekStart.AddDays((range.end - 1) * 7 + dayOfWeekOffset);
                                                        AddScheduleRow(targetStart, tStart, targetEnd, tEnd, false);
                                                        hasAnySchedule = true;
                                                    }
                                                }
                                            }
                                            
                                            if (!hasAnySchedule) {
                                                AddScheduleRow(DateTime.Today, new TimeSpan(7,0,0), DateTime.Today, new TimeSpan(9,0,0), false);
                                            }
                                            
                                            // Tự động set Lặp lại thành Hàng tuần
                                            foreach (ComboBoxItem item in CmbRecurrence.Items) {
                                                if (item.Tag?.ToString() == "WEEKLY") {
                                                    CmbRecurrence.SelectedItem = item;
                                                    break;
                                                }
                                            }
                                            TxtGuestSearch.Text = "";
                                            found = true;
                                            break;
                                        }
                                    }
                                }
                                if (found) break;
                            }
                        }
                        if (!found) ShowError("Không tìm thấy mã học phần này trong dữ liệu.");
                    }
                    else
                    {
                        ShowError("Không tìm thấy file HK2_2025.json.");
                    }
                }
                catch (Exception ex)
                {
                    ShowError("Lỗi đọc dữ liệu HP: " + ex.Message);
                }
                return;
            }

            // Tính mốc UTC để check conflict
            DateTime startUtc = DateTime.UtcNow;
            DateTime endUtc = DateTime.UtcNow.AddHours(1);
            if (SchedulePanel.Children.Count > 0 && SchedulePanel.Children[0] is Grid rowFirst)
            {
                var mainStack = rowFirst.Children[0] as StackPanel;
                var stCol1 = mainStack.Children[0] as StackPanel;
                var stCol2 = mainStack.Children[1] as StackPanel;
                var grid1 = stCol1.Children[0] as Grid;
                var chkAllDay = grid1.Children[1] as CheckBox;
                var grid2 = stCol1.Children[1] as Grid;
                var dpStart = grid2.Children[0] as DatePicker;
                var timeStartPanel = grid2.Children[1] as StackPanel;
                var grid3 = stCol2.Children[1] as Grid;
                var dpEnd = grid3.Children[0] as DatePicker;
                var timeEndPanel = grid3.Children[1] as StackPanel;
                TimeSpan stTime = TimeSpan.Zero, enTime = new TimeSpan(23, 59, 59);
                if (chkAllDay.IsChecked != true) { stTime = GetTimeFromPanel(timeStartPanel); enTime = GetTimeFromPanel(timeEndPanel); }
                startUtc = ((dpStart.SelectedDate ?? DateTime.Today).Date.Add(stTime)).ToUniversalTime();
                endUtc = ((dpEnd.SelectedDate ?? DateTime.Today).Date.Add(enTime)).ToUniversalTime();
            }

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
            
            if (SchedulePanel.Children.Count == 0)
            {
                ShowError("Phải có ít nhất một khung thời gian");
                return;
            }

            var schedulesToSave = new List<(DateTime start, DateTime end, bool isAllDay)>();

            foreach (Grid row in SchedulePanel.Children)
            {
                var mainStack = row.Children[0] as StackPanel;
                var stCol1 = mainStack.Children[0] as StackPanel;
                var stCol2 = mainStack.Children[1] as StackPanel;
                
                var grid1 = stCol1.Children[0] as Grid;
                var chkAllDay = grid1.Children[1] as CheckBox;
                var grid2 = stCol1.Children[1] as Grid;
                var dpStart = grid2.Children[0] as DatePicker;
                var timeStartPanel = grid2.Children[1] as StackPanel;

                var grid3 = stCol2.Children[1] as Grid;
                var dpEnd = grid3.Children[0] as DatePicker;
                var timeEndPanel = grid3.Children[1] as StackPanel;

                TimeSpan startTime = TimeSpan.Zero;
                TimeSpan endTime = new TimeSpan(23, 59, 59);

                if (chkAllDay.IsChecked != true)
                {
                    startTime = GetTimeFromPanel(timeStartPanel);
                    endTime = GetTimeFromPanel(timeEndPanel);
                }

                DateTime start = (dpStart.SelectedDate ?? DateTime.Today).Date.Add(startTime);
                DateTime end   = (dpEnd.SelectedDate ?? start.Date).Date.Add(endTime);

                if (end <= start)
                {
                    ShowError("Giờ kết thúc phải sau giờ bắt đầu");
                    return;
                }

                schedulesToSave.Add((start.ToUniversalTime(), end.ToUniversalTime(), chkAllDay.IsChecked == true));
            }

            string eventType = ((ComboBoxItem)CmbType.SelectedItem)?.Tag?.ToString() ?? "PERSONAL";
            bool isCompleted = _chkCompletedDynamic != null && _chkCompletedDynamic.IsChecked == true;
            string colorCategory = "#1A73E8";
            if (RbColorRed.IsChecked == true) colorCategory = "#D93025";
            else if (RbColorGreen.IsChecked == true) colorCategory = "#1E8E3E";
            else if (RbColorYellow.IsChecked == true) colorCategory = "#F9AB00";
            else if (RbColorPurple.IsChecked == true) colorCategory = eventType == "REMINDER" ? "#3F51B5" : "#9334E6";
            else if (RbColorTeal.IsChecked == true) colorCategory = "#009688";

            string groupId = string.IsNullOrEmpty(_event.GroupId) ? Guid.NewGuid().ToString() : _event.GroupId;

            for (int i = 0; i < schedulesToSave.Count; i++)
            {
                PersonalEvent eventToSave = (i == 0) ? _event : new PersonalEvent { IdAcc = _event.IdAcc };
                
                eventToSave.Title       = TxtEventTitle.Text;
                eventToSave.Location    = TxtLocation.Text;
                eventToSave.Description = TxtDesc.Text;
                eventToSave.StartTime   = schedulesToSave[i].start;
                eventToSave.EndTime     = schedulesToSave[i].end;
                eventToSave.EventType   = eventType;
                eventToSave.IsCompleted = isCompleted;
                eventToSave.ColorCategory = colorCategory;
                eventToSave.IsAllDay    = schedulesToSave[i].isAllDay;
                eventToSave.GroupId     = groupId;

                if (i > 0)
                {
                    eventToSave.RecurrenceRule = ((ComboBoxItem)CmbRecurrence.SelectedItem)?.Tag?.ToString() ?? "NONE";
                }

                var (ok, msg) = _bll.Save(eventToSave);
                if (!ok) 
                { 
                    ShowError(msg); 
                    return; 
                }

                SaveRemindersAndRecurrence(eventToSave);
            }

            this.DialogResult = true; 
        }

        private void SaveRemindersAndRecurrence(PersonalEvent eToSave)
        {
            long eventId = eToSave.IdEvent;
            if (eventId == 0) // Lấy ID mới nhất nếu vừa Insert
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString)) {
                    conn.Open();
                    using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT TOP 1 id_event FROM PERSONAL_EVENT WHERE id_acc = @uid ORDER BY id_event DESC", conn)) {
                        cmd.Parameters.AddWithValue("@uid", eToSave.IdAcc);
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
                    using (var cmdDel = new System.Data.SqlClient.SqlCommand("DELETE FROM EVENT_ATTENDEE WHERE id_event = @id", conn)) {
                        cmdDel.Parameters.AddWithValue("@id", eventId); cmdDel.ExecuteNonQuery();
                    }
                    foreach (var g in _guestList) {
                        using (var cmdIns = new System.Data.SqlClient.SqlCommand("INSERT INTO EVENT_ATTENDEE (id_event, id_acc, response_status) VALUES (@id, @uid, @st)", conn)) {
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
            if (!string.IsNullOrEmpty(_event.GroupId))
            {
                var resAc = MessageBox.Show($"Sự kiện này thuộc một nhóm các sự kiện liên kết.\n\nBạn có muốn xóa TOÀN BỘ các sự kiện trong nhóm này không?\n\n- Chọn 'Yes' để xóa toàn bộ.\n- Chọn 'No' để chỉ xóa sự kiện hiện tại.\n- Chọn 'Cancel' để hủy.", "Xác nhận xóa", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                
                if (resAc == MessageBoxResult.Cancel) return;
                
                if (resAc == MessageBoxResult.Yes)
                {
                    _bll.DeleteEventGroup(_event.IdAcc, _event.GroupId);
                    this.DialogResult = true; // Đóng và báo cho CalendarPage Render lại
                    return;
                }
            }
            else if (_event.EventType == "ACADEMIC")
            {
                var resAc = MessageBox.Show($"Sự kiện này là lịch học của môn '{_event.Title}'.\n\nBạn có muốn xóa TOÀN BỘ các lịch học của môn này không?\n\n- Chọn 'Yes' để xóa toàn bộ.\n- Chọn 'No' để chỉ xóa khung giờ này.\n- Chọn 'Cancel' để hủy.", "Xác nhận xóa", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                
                if (resAc == MessageBoxResult.Cancel) return;
                
                if (resAc == MessageBoxResult.Yes)
                {
                    _bll.DeleteRelatedEvents(_event.IdAcc, _event.Title, _event.EventType);
                    this.DialogResult = true; // Đóng và báo cho CalendarPage Render lại
                    return;
                }
            }

            var res = MessageBox.Show("Xóa sự kiện này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                _bll.Delete(_event.IdEvent);
                this.DialogResult = true; // Đóng và báo cho CalendarPage Render lại
            }
        }

        private void Link_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TextBlock tb) tb.TextDecorations = TextDecorations.Underline;
        }

        private void Link_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TextBlock tb) tb.TextDecorations = null;
        }

        private void ShowError(string msg)
        {
            TxtErr.Text = "⚠ " + msg;
            TxtErr.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.DialogResult = false;
    }
}