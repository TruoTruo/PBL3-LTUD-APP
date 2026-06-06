using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;
using StudentReminderApp.Views.Dialogs;

namespace StudentReminderApp.Views.Pages
{
    public partial class CalendarPage : Page
    {
        // Biến lưu trữ ngày đang xem (Mặc định là hôm nay)
        private DateTime _current = DateTime.Today;
        private readonly EventBLL _bll = new EventBLL();
        
        // Biến tạm lưu sự kiện đang mở trên Popover
        private CalendarItem _selectedPopupItem;
        private PersonalEvent _pendingCreateEvent;
        
        // DRAG & DROP / SWEEP CREATION VARIABLES
        private bool _isCreating = false;
        private double _createStartY;
        private long? _highlightEventId = null;
        
        private Dictionary<long, string> _guestStatuses = new Dictionary<long, string>();
        
        public class TagSelectionItem
        {
            public long IdTag { get; set; }
            public string TagName { get; set; }
            public bool IsSelected { get; set; }
        }
        private List<TagSelectionItem> _quickAvailableTags = new List<TagSelectionItem>();

        public CalendarPage()
        {
            InitializeComponent();
            
            // Khởi tạo danh sách 24 múi giờ cho trục bên trái (00:00 -> 23:00)
            var hours = new List<object>();
            for (int i = 0; i < 24; i++) hours.Add(new { HourText = $"{i:D2}:00" });
            HourLinesControl.ItemsSource = hours;
            if (WeekHourLinesControl != null) WeekHourLinesControl.ItemsSource = hours;

            // Render lần đầu khi Page được load
            Loaded += (s, e) => 
            {
                LoadTags();
                Render();
            };
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) Render();
        }
        
        private void ChkHidePastEvents_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded && !string.IsNullOrEmpty(TxtSearchEvent.Text)) TxtSearchEvent_TextChanged(null, null);
        }

        private void TxtSearchEvent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            string searchText = TxtSearchEvent.Text?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(searchText) && SearchResultsPopup != null)
            {
                SearchResultsPopup.IsOpen = false;
                Render();
                return;
            }

            Render(); // Cập nhật lưới lịch trước để không làm mất Focus chuột của Popover

            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    bool hidePast = ChkHidePastEvents?.IsChecked == true;
                    string timeFilter = hidePast ? "AND e.start_time >= @now" : "";
                    
                    string sql = $@"
                        SELECT e.id_event, e.title, e.start_time, e.location
                        FROM PERSONAL_EVENT e
                        LEFT JOIN EVENT_ATTENDEE a ON e.id_event = a.id_event
                        WHERE (e.id_acc = @uid OR a.id_acc = @uid) 
                          AND (LOWER(e.title) LIKE @s OR LOWER(e.location) LIKE @s)
                          {timeFilter}
                        ORDER BY e.start_time ASC";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", SessionManager.CurrentAccount.IdAcc);
                        cmd.Parameters.AddWithValue("@s", "%" + searchText.ToLower() + "%");
                        if (hidePast) cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

                        using (var reader = cmd.ExecuteReader())
                        {
                            SearchResultsPanel.Children.Clear();
                            int count = 0;
                            while (reader.Read())
                            {
                                count++;
                                long idEvent = Convert.ToInt64(reader["id_event"]);
                                string title = reader["title"].ToString();
                                DateTime startUtc = Convert.ToDateTime(reader["start_time"]);
                                DateTime startLocal = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).ToLocalTime();

                                var itemBorder = new Border {
                                    Background = Brushes.Transparent, 
                                    Padding = new Thickness(15, 8, 15, 8), 
                                    Cursor = Cursors.Hand
                                };
                                itemBorder.MouseEnter += (s, ev) => itemBorder.Background = new SolidColorBrush(Color.FromRgb(241, 243, 244));
                                itemBorder.MouseLeave += (s, ev) => itemBorder.Background = Brushes.Transparent;
                                
                                var sp = new StackPanel();
                                var titleBlock = new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(60, 64, 67)), TextTrimming = TextTrimming.CharacterEllipsis };
                                var timeBlock = new TextBlock { Text = startLocal.ToString("dd/MM/yyyy HH:mm"), FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(112, 117, 122)), Margin = new Thickness(0, 2, 0, 0) };
                                
                                sp.Children.Add(titleBlock);
                                sp.Children.Add(timeBlock);
                                itemBorder.Child = sp;

                                itemBorder.MouseLeftButtonDown += (s, ev) => {
                                    _highlightEventId = idEvent;
                                    _current = startLocal.Date;
                                    TxtSearchEvent.Text = ""; 
                                    SearchResultsPopup.IsOpen = false;
                                    Render();

                                    Dispatcher.BeginInvoke(new Action(() => {
                                        _highlightEventId = null;
                                    }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);

                                    ev.Handled = true;
                                };
                                
                                SearchResultsPanel.Children.Add(itemBorder);
                            }
                            
                            if (count == 0)
                            {
                                SearchResultsPanel.Children.Add(new TextBlock { Text = "Không tìm thấy sự kiện", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(15, 10, 15, 10) });
                            }
                            
                            SearchResultsPopup.PlacementTarget = TxtSearchEvent;
                            SearchResultsPopup.IsOpen = true;
                        }
                    }
                }
            }
            catch { }
        }

        private List<CalendarItem> GetFilteredEvents(int year, int month)
        {
            var rawEvents = _bll.GetCalendarItemsForMonth(SessionManager.CurrentAccount.IdAcc, year, month);
            var result = new List<CalendarItem>();
            string searchText = TxtSearchEvent?.Text?.Trim().ToLower() ?? "";

            DateTime monthStart = new DateTime(year, month, 1);
            DateTime monthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            DateTime scanStart = monthStart.AddDays(-7);
            DateTime scanEnd = monthEnd.AddDays(7);

            // 0.1 Lấy danh sách mapping Tag của các sự kiện trong tháng
            var eventTagsMapping = new Dictionary<long, List<long>>();
            try {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString)) {
                    conn.Open();
                    string sqlTags = @"
                        SELECT m.id_event, m.id_tag 
                        FROM EVENT_TAG_MAPPING m
                        JOIN PERSONAL_EVENT e ON m.id_event = e.id_event
                        WHERE e.id_acc = @uid";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(sqlTags, conn)) {
                        cmd.Parameters.AddWithValue("@uid", SessionManager.CurrentAccount.IdAcc);
                        using (var r = cmd.ExecuteReader()) {
                            while(r.Read()) {
                                long eId = (long)r["id_event"];
                                long tId = (long)r["id_tag"];
                                if (!eventTagsMapping.ContainsKey(eId)) eventTagsMapping[eId] = new List<long>();
                                eventTagsMapping[eId].Add(tId);
                            }
                        }
                    }
                }
            } catch { }

            var checkedTagIds = new HashSet<long>();
            Action<StackPanel> collectTags = (panel) => {
                if (panel == null) return;
                foreach(var child in panel.Children) {
                    if (child is Grid g) {
                        var chk = g.Children.OfType<CheckBox>().FirstOrDefault();
                        if (chk != null && chk.IsChecked == true && chk.Tag is long tid) {
                            checkedTagIds.Add(tid);
                        }
                    }
                }
            };
            collectTags(PersonalTagsPanel);
            collectTags(AcademicTagsPanel);
            collectTags(ReminderTagsPanel);

            // 0. Chuẩn hóa Múi giờ (Chuyển UTC -> Local)
            foreach (var e in rawEvents) {
                if (e.OriginalEvent is PersonalEvent p) {
                    e.StartTime = DateTime.SpecifyKind(e.StartTime, DateTimeKind.Utc).ToLocalTime();
                    e.EndTime = DateTime.SpecifyKind(e.EndTime, DateTimeKind.Utc).ToLocalTime();
                    p.StartTime = e.StartTime; p.EndTime = e.EndTime;
                }
            }

            // 0.5 Quét các sự kiện được mời (Là Guest)
            try {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString)) {
                    conn.Open();
                    string guestSql = "SELECT e.*, a.response_status FROM PERSONAL_EVENT e JOIN EVENT_ATTENDEE a ON e.id_event = a.id_event WHERE a.id_acc = @uid AND e.start_time <= @scanEnd";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(guestSql, conn)) {
                        cmd.Parameters.AddWithValue("@uid", SessionManager.CurrentAccount.IdAcc);
                        cmd.Parameters.AddWithValue("@scanEnd", scanEnd.ToUniversalTime());
                        using (var reader = cmd.ExecuteReader()) {
                            while (reader.Read()) {
                                long idEv = Convert.ToInt64(reader["id_event"]);
                                DateTime st = DateTime.SpecifyKind(Convert.ToDateTime(reader["start_time"]), DateTimeKind.Utc).ToLocalTime();
                                DateTime en = DateTime.SpecifyKind(Convert.ToDateTime(reader["end_time"]), DateTimeKind.Utc).ToLocalTime();
                                var p = new PersonalEvent { IdEvent = idEv, IdAcc = Convert.ToInt64(reader["id_acc"]), Title = reader["title"].ToString(), StartTime = st, EndTime = en, EventType = reader["event_type"].ToString(), ColorCategory = reader["color_category"].ToString() };
                                string status = reader["response_status"].ToString();
                                _guestStatuses[idEv] = status;
                                rawEvents.Add(new CalendarItem { Title = p.Title, StartTime = p.StartTime, EndTime = p.EndTime, EventType = p.EventType, OriginalEvent = p });
                            }
                        }
                    }
                }
            } catch { }

            // 1. Quét các sự kiện lặp lại (Recurrence) đã tạo trong quá khứ
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT * FROM PERSONAL_EVENT WHERE id_acc = @uid AND recurrence_rule IS NOT NULL AND recurrence_rule != 'NONE' AND start_time <= @scanEnd";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", SessionManager.CurrentAccount.IdAcc);
                        cmd.Parameters.AddWithValue("@scanEnd", scanEnd);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var p = new PersonalEvent {
                                    IdEvent = Convert.ToInt64(reader["id_event"]), IdAcc = Convert.ToInt64(reader["id_acc"]),
                                    Title = reader["title"].ToString(), StartTime = Convert.ToDateTime(reader["start_time"]),
                                    EndTime = Convert.ToDateTime(reader["end_time"]), RecurrenceRule = reader["recurrence_rule"].ToString(),
                                    EventType = reader["event_type"].ToString(), ColorCategory = reader["color_category"].ToString()
                                };
                                if (!rawEvents.Any(e => e.OriginalEvent is PersonalEvent pe && pe.IdEvent == p.IdEvent))
                                {
                                    rawEvents.Add(new CalendarItem { Title = p.Title, StartTime = p.StartTime, EndTime = p.EndTime, EventType = p.EventType, OriginalEvent = p });
                                }
                            }
                        }
                    }
                }
            } catch { }

            // 2. Nội suy đối tượng ảo (Virtual Objects)
            foreach (var e in rawEvents)
            {
                if (e.OriginalEvent is PersonalEvent p && !string.IsNullOrEmpty(p.RecurrenceRule) && p.RecurrenceRule != "NONE")
                {
                    DateTime recurrenceEndDate = p.EndTime.Date > p.StartTime.Date ? p.EndTime.Date : DateTime.MaxValue;
                    
                    TimeSpan duration = p.EndTime.TimeOfDay - p.StartTime.TimeOfDay;
                    if (duration < TimeSpan.Zero) duration = duration.Add(TimeSpan.FromDays(1));
                    
                    DateTime currDate = p.StartTime.Date;
                    while (currDate <= scanEnd && currDate <= recurrenceEndDate)
                    {
                        if (currDate >= scanStart)
                        {
                            if (p.RecurrenceRule == "DAILY" || (p.RecurrenceRule == "WEEKLY" && currDate.DayOfWeek == p.StartTime.DayOfWeek) || (p.RecurrenceRule == "MONTHLY" && currDate.Day == p.StartTime.Day))
                            {
                                var clone = new CalendarItem { Title = e.Title, Description = e.Description, Location = e.Location, StartTime = currDate.Add(p.StartTime.TimeOfDay), EndTime = currDate.Add(p.StartTime.TimeOfDay).Add(duration), EventType = e.EventType, OriginalEvent = p };
                                result.Add(clone);
                            }
                        }
                        currDate = currDate.AddDays(1);
                    }
                }
                else
                {
                    if (e.StartTime >= scanStart && e.StartTime <= scanEnd) result.Add(e);
                }
            }

            return result.Where(e => {
                bool isAcademic = e.EventType == "ACADEMIC" || !(e.OriginalEvent is PersonalEvent);
                bool isReminder = e.EventType == "REMINDER";
                
                bool matchFilter = false;
                if (isAcademic) matchFilter = ChkAcademic?.IsChecked == true;
                else if (isReminder) matchFilter = ChkReminder?.IsChecked == true;
                else matchFilter = ChkPersonal?.IsChecked == true;

                if (!matchFilter) return false;

                if (e.OriginalEvent is PersonalEvent pe && eventTagsMapping.ContainsKey(pe.IdEvent)) {
                    var tagsForEvent = eventTagsMapping[pe.IdEvent];
                    if (tagsForEvent.Count > 0 && !tagsForEvent.Any(t => checkedTagIds.Contains(t))) {
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(searchText))
                {
                    bool matchTitle = e.Title?.ToLower().Contains(searchText) == true;
                    bool matchLoc = e.Location?.ToLower().Contains(searchText) == true;
                    if (!matchTitle && !matchLoc) return false;
                }
                return true;
            }).GroupBy(x => new { x.Title, x.StartTime }).Select(g => g.First()).ToList(); // Tránh trùng lặp
        }

        // Hàm điều phối chính
        private void Render()
        {
            if (TxtMonthYear == null) return;
            
            // Xóa bỏ tình trạng "treo chuột" khi người dùng chuyển trang đột ngột
            _isCreating = false;
            Mouse.Capture(null);
            if (QuickCreatePopup != null) QuickCreatePopup.IsOpen = false;
            if (QuickEventPopup != null) QuickEventPopup.IsOpen = false;
            if (PopupOverlay != null) PopupOverlay.Visibility = Visibility.Collapsed;

            // Đồng bộ ngày với MiniCalendar bên Sidebar
            if (MiniCalendar != null && MiniCalendar.SelectedDate != _current)
                MiniCalendar.SelectedDate = _current;
            
            MonthViewGrid.Visibility = Visibility.Collapsed;
            ScheduleViewScroll.Visibility = Visibility.Collapsed;
            if (WeekViewScroll != null) WeekViewScroll.Visibility = Visibility.Collapsed;
            if (YearViewScroll != null) YearViewScroll.Visibility = Visibility.Collapsed;
            HeaderDateInfo.Visibility = Visibility.Collapsed;
            if (WeekHeaderInfo != null) WeekHeaderInfo.Visibility = Visibility.Collapsed;
            if (AllDayDayContainer != null) AllDayDayContainer.Visibility = Visibility.Collapsed;
            if (AllDayWeekContainer != null) AllDayWeekContainer.Visibility = Visibility.Collapsed;
            
            if (RbMonthView.IsChecked == true)
            {
                TxtMonthYear.Text = $"tháng {_current.Month} năm {_current.Year}";
                MonthViewGrid.Visibility = Visibility.Visible;
                RenderMonthView();
            }
            else if (RbScheduleView.IsChecked == true)
            {
                TxtMonthYear.Text = _current.ToString("dd/MM/yyyy");
                HeaderDateInfo.Visibility = Visibility.Visible;
                ScheduleViewScroll.Visibility = Visibility.Visible;
                RenderTimeGridView();
            }
            else if (RbWeekView.IsChecked == true)
            {
                WeekHeaderInfo.Visibility = Visibility.Visible;
                WeekViewScroll.Visibility = Visibility.Visible;
                RenderWeekView();
            }
            else if (RbYearView.IsChecked == true)
            {
                TxtMonthYear.Text = _current.ToString("yyyy");
                YearViewScroll.Visibility = Visibility.Visible;
                RenderYearView();
            }
        }

        #region LOGIC XEM THEO THÁNG (UniformGrid)

        private void RenderMonthView()
        {
            CalendarGrid.Children.Clear();
            var events = GetFilteredEvents(_current.Year, _current.Month);

            // Tính toán ngày bắt đầu của lưới (bao gồm ngày tháng trước nếu cần)
            DateTime firstOfMonth = new DateTime(_current.Year, _current.Month, 1);
            int startDow = ((int)firstOfMonth.DayOfWeek + 6) % 7; // Thứ 2 là đầu tuần
            var gridStart = firstOfMonth.AddDays(-startDow);

            for (int i = 0; i < 42; i++)
            {
                var date = gridStart.AddDays(i);
                var dayEvts = events.Where(e => e.StartTime.Date == date.Date).OrderBy(e => e.StartTime).ToList();
                CalendarGrid.Children.Add(MakeDayCell(date, dayEvts));
            }
        }

        private Border MakeDayCell(DateTime date, List<CalendarItem> events)
        {
            bool isToday = date.Date == DateTime.Today;
            bool isOtherMonth = date.Month != _current.Month;

            var cell = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(218, 220, 224)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = Brushes.White,
                Padding = new Thickness(0, 4, 0, 0), Cursor = Cursors.Hand
            };

            var panel = new StackPanel();
            
            var dayNumContainer = new Border
            {
                MinWidth = 24, Height = 24, CornerRadius = new CornerRadius(12),
                Padding = date.Day == 1 ? new Thickness(6, 0, 6, 0) : new Thickness(0),
                Background = isToday ? new SolidColorBrush(Color.FromRgb(26, 115, 232)) : Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            };
            
            dayNumContainer.Child = new TextBlock {
                Text = date.Day.ToString(), 
                FontSize = 12, FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Foreground = isToday ? Brushes.White : 
                             isOtherMonth ? new SolidColorBrush(Color.FromRgb(112, 117, 122)) : new SolidColorBrush(Color.FromRgb(60, 64, 67))
            };
            panel.Children.Add(dayNumContainer);

            foreach (var ev in events.Take(3)) panel.Children.Add(CreateEventChip(ev));

            if (events.Count > 3)
            {
                var more = new TextBlock { Text = $"{events.Count - 3} sự kiện khác", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(60, 64, 67)), Margin = new Thickness(8, 2, 0, 0) };
                more.MouseUp += (s, e) => {
                    e.Handled = true;
                    ShowPopupAllEvents(date, events, (UIElement)s);
                };
                panel.Children.Add(more);
            }

            cell.Child = panel;
            cell.MouseLeftButtonUp += (s, e) => { if (!e.Handled) OpenNewEvent(date, cell); };
            return cell;
        }

        #endregion

        #region LOGIC XEM THEO NGÀY (Time Grid 0-24h)

        private void RenderTimeGridView()
        {
            if (TxtCurrentDayName == null || TxtCurrentDayNumber == null || EventsCanvas == null) return;

            // 1. Cập nhật Header ngày tháng
            TxtCurrentDayName.Text = _current.ToString("ddd").ToUpper();
            TxtCurrentDayNumber.Text = _current.Day.ToString();
            if (_current.Date == DateTime.Today)
            {
                TodayCircleBorder.Background = new SolidColorBrush(Color.FromRgb(26, 115, 232));
                TxtCurrentDayNumber.Foreground = Brushes.White;
            }
            else
            {
                TodayCircleBorder.Background = Brushes.Transparent;
                TxtCurrentDayNumber.Foreground = new SolidColorBrush(Color.FromRgb(60, 64, 67));
            }

            // 2. Làm sạch và chuẩn bị Canvas
            var oldChips = EventsCanvas.Children.OfType<Border>().Where(b => b.Tag is CalendarItem).ToList();
            foreach (var chip in oldChips) EventsCanvas.Children.Remove(chip);
            EventsCanvas.Background = Brushes.Transparent; // Quan trọng để bắt chuột

            // 3. Lấy dữ liệu sự kiện
            var events = GetFilteredEvents(_current.Year, _current.Month)
                            .Where(e => e.StartTime.Date == _current.Date)
                            .OrderBy(e => e.StartTime)
                            .ToList();
                            
            // Phân tách sự kiện All-Day và sự kiện bình thường
            var allDayEvents = events.Where(e => e.EndTime.Date > e.StartTime.Date || (e.EndTime - e.StartTime).TotalHours >= 23).ToList();
            var normalEvents = events.Except(allDayEvents).ToList();
            
            // Vẽ sự kiện All-Day (Mới)
            AllDayDayContainer.Visibility = Visibility.Visible;
            AllDayDayEvents.Children.Clear();
            foreach (var ev in allDayEvents)
            {
                var chip = CreateEventChip(ev);
                chip.Margin = new Thickness(0, 0, 0, 4);
                chip.Padding = new Thickness(8, 4, 8, 4);
                chip.CornerRadius = new CornerRadius(4);
                
                // Sửa màu chữ để phù hợp làm All-Day Bar
                if (chip.Child is TextBlock txt) txt.Foreground = Brushes.White;
                
                AllDayDayEvents.Children.Add(chip);
            }

            if (normalEvents.Count == 0) { UpdateNowLine(); return; }

            // 4. Thuật toán xếp chồng (Event Overlap Algorithm)
            var groups = new List<List<CalendarItem>>();
            DateTime maxGroupEnd = DateTime.MinValue;
            List<CalendarItem> currentGroup = null;

            foreach (var ev in normalEvents)
            {
                if (currentGroup == null || ev.StartTime >= maxGroupEnd)
                {
                    currentGroup = new List<CalendarItem>();
                    groups.Add(currentGroup);
                    maxGroupEnd = ev.EndTime;
                }
                currentGroup.Add(ev);
                if (ev.EndTime > maxGroupEnd) maxGroupEnd = ev.EndTime;
            }

            double canvasWidth = EventsCanvas.ActualWidth > 0 ? EventsCanvas.ActualWidth : 500;

            foreach (var group in groups)
            {
                var columns = new List<List<CalendarItem>>();
                foreach (var ev in group)
                {
                    bool placed = false;
                    foreach (var col in columns)
                    {
                        if (ev.StartTime >= col.Last().EndTime)
                        {
                            col.Add(ev);
                            placed = true;
                            break;
                        }
                    }
                    if (!placed) columns.Add(new List<CalendarItem> { ev });
                }

                double widthPerEvent = (canvasWidth - 10) / columns.Count;
                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                {
                    foreach (var ev in columns[colIdx])
                    {
                        double startY = (ev.StartTime.Hour * 60) + ev.StartTime.Minute;
                        double endY = (ev.EndTime.Hour * 60) + ev.EndTime.Minute;
                        double height = Math.Max(25, endY - startY);

                        var chip = new Border {
                            Background = ev.BackgroundColor,
                            CornerRadius = new CornerRadius(6),
                            Padding = new Thickness(8, 4, 8, 4),
                            Width = widthPerEvent - 4,
                            Height = height,
                            Tag = ev,
                            Cursor = Cursors.Hand,
                            BorderBrush = Brushes.White,
                            BorderThickness = new Thickness(1),
                            IsHitTestVisible = true
                        };

                        // Hiệu ứng Pending cho khách mời
                        if (ev.OriginalEvent is PersonalEvent pe && _guestStatuses.ContainsKey(pe.IdEvent) && _guestStatuses[pe.IdEvent] == "PENDING")
                        {
                            chip.Background = Brushes.White;
                            chip.BorderBrush = ev.BackgroundColor;
                            chip.BorderThickness = new Thickness(2);
                            chip.Opacity = 0.8;
                        }

                        var stp = new StackPanel { IsHitTestVisible = false };
                        
                        bool isReminder = ev.EventType == "REMINDER";
                        bool isCompleted = (ev.OriginalEvent is PersonalEvent pEvent && pEvent.IsCompleted);
                        var txtTitle = new TextBlock { Text = (isReminder ? (isCompleted ? "☑ " : "☐ ") : "") + ev.Title, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis };
                        if (isCompleted) { txtTitle.TextDecorations = TextDecorations.Strikethrough; chip.Opacity = 0.6; }

                        stp.Children.Add(txtTitle);
                        stp.Children.Add(new TextBlock { Text = ev.StartTime.ToString("HH:mm"), Foreground = Brushes.White, FontSize = 10, Opacity = 0.8 });
                        
                        bool isRecurrent = !(ev.OriginalEvent is PersonalEvent) || (ev.OriginalEvent is PersonalEvent pEv && !string.IsNullOrEmpty(pEv.RecurrenceRule) && pEv.RecurrenceRule != "NONE");
                        if (isRecurrent)
                        {
                            stp.Children.Add(new TextBlock { Text = "🔁 Lặp lại", Foreground = Brushes.White, FontSize = 10, Margin = new Thickness(0, 2, 0, 0) });
                        }

                        var chipGrid = new Grid();
                        chipGrid.Children.Add(stp);
                        chip.Child = chipGrid;

                        AttachChipEvents(chip, ev); // Sử dụng hàm phân phối chuột

                        Canvas.SetTop(chip, startY);
                        Canvas.SetLeft(chip, colIdx * widthPerEvent + 5);
                        EventsCanvas.Children.Add(chip);
                    }
                }
            }
            UpdateNowLine();
        }

        // HÀM DUY NHẤT ĐỂ MỞ DIALOG (Hợp nhất từ 3 hàm của bạn)
        private void OpenEventDialog(CalendarItem item)
{
    try 
    {
        // Kiểm tra đúng loại sự kiện cá nhân để cho phép sửa/xóa
        if (item.OriginalEvent is PersonalEvent p) 
        {
            var dlg = new EventDialog(p);
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null) dlg.Owner = parentWindow;
            
            if (dlg.ShowDialog() == true) 
            {
                    _current = p.StartTime.Kind == DateTimeKind.Utc ? p.StartTime.ToLocalTime().Date : p.StartTime.Date;
                Render(); // Vẽ lại lịch để cập nhật thay đổi
            }
        }
        else 
        {
            MessageBox.Show("Đây là lịch học, không thể chỉnh sửa trực tiếp!", "Thông báo");
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show("Lỗi khi mở cửa sổ: " + ex.Message);
    }
}
        private void UpdateNowLine()
        {
            if (_current.Date == DateTime.Today)
            {
                CurrentTimeLine.Visibility = Visibility.Visible;
                CurrentTimeDot.Visibility = Visibility.Visible;
                double nowY = (DateTime.Now.Hour * 60) + DateTime.Now.Minute;
                Canvas.SetTop(CurrentTimeLine, nowY);
                Canvas.SetTop(CurrentTimeDot, nowY);
            }
            else
            {
                CurrentTimeLine.Visibility = Visibility.Collapsed;
                CurrentTimeDot.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region LOGIC XEM THEO TUẦN (Week View)
        
        private void RenderWeekView()
        {
            if (WeekEventsCanvas == null || WeekHeaderInfo == null) return;

            int diff = (7 + (_current.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = _current.Date.AddDays(-diff);
            DateTime endOfWeek = startOfWeek.AddDays(6);

            TxtMonthYear.Text = $"{startOfWeek:dd/MM} - {endOfWeek:dd/MM/yyyy}";

            WeekHeaderInfo.Children.Clear();
            for (int i = 0; i < 7; i++)
            {
                DateTime day = startOfWeek.AddDays(i);
                bool isToday = day.Date == DateTime.Today;

                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                sp.Children.Add(new TextBlock 
                { 
                    Text = day.ToString("ddd").ToUpper(), 
                    FontSize = 11, 
                    FontWeight = FontWeights.SemiBold, 
                    Foreground = new SolidColorBrush(Color.FromRgb(112, 117, 122)), 
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                });
                
                var txtNum = new TextBlock 
                { 
                    Text = day.Day.ToString(), 
                    FontSize = 26, 
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = isToday ? Brushes.White : new SolidColorBrush(Color.FromRgb(60, 64, 67))
                };
                
                var border = new Border
                {
                    Background = isToday ? new SolidColorBrush(Color.FromRgb(26, 115, 232)) : Brushes.Transparent,
                    CornerRadius = new CornerRadius(23),
                    Width = 46,
                    Height = 46,
                    Child = txtNum
                };
                sp.Children.Add(border);

                Grid.SetColumn(sp, i);
                WeekHeaderInfo.Children.Add(sp);
            }

            // Dọn dẹp an toàn cho lịch Tuần (Giữ lại bóng mờ sự kiện)
            var oldChipsWeek = WeekEventsCanvas.Children.OfType<Border>().Where(b => b.Tag is CalendarItem).ToList();
            foreach (var chip in oldChipsWeek) WeekEventsCanvas.Children.Remove(chip);

            var oldLines = WeekEventsCanvas.Children.OfType<Line>().ToList();
            foreach (var line in oldLines) WeekEventsCanvas.Children.Remove(line);

            var oldDots = WeekEventsCanvas.Children.OfType<Ellipse>().ToList();
            foreach (var dot in oldDots) WeekEventsCanvas.Children.Remove(dot);
            
            var events = new List<CalendarItem>();
            events.AddRange(GetFilteredEvents(startOfWeek.Year, startOfWeek.Month));
            if (startOfWeek.Month != endOfWeek.Month)
            {
                events.AddRange(GetFilteredEvents(endOfWeek.Year, endOfWeek.Month));
            }
            
            events = events.Where(e => e.StartTime.Date >= startOfWeek && e.StartTime.Date <= endOfWeek)
                           .OrderBy(e => e.StartTime)
                           .Distinct()
                           .ToList();

            double canvasWidth = WeekEventsCanvas.ActualWidth > 0 ? WeekEventsCanvas.ActualWidth : 800;
            double colWidth = canvasWidth / 7.0;

            // Tách All-Day Events ra để hiển thị sticky
            var allDayEvents = events.Where(e => e.EndTime.Date > e.StartTime.Date || (e.EndTime - e.StartTime).TotalHours >= 23).ToList();
            var normalEvents = events.Except(allDayEvents).ToList();

            AllDayWeekContainer.Visibility = Visibility.Visible;
            AllDayWeekEventsGrid.Children.Clear();
            foreach (var ev in allDayEvents)
            {
                int startIdx = Math.Max(0, (ev.StartTime.Date - startOfWeek).Days);
                int endIdx = Math.Min(6, (ev.EndTime.Date - startOfWeek).Days);
                
                var chip = CreateEventChip(ev);
                chip.Margin = new Thickness(2, 2, 2, 2);
                Grid.SetColumn(chip, startIdx);
                Grid.SetColumnSpan(chip, endIdx - startIdx + 1); // Spanning ngang qua lưới Grid!
                
                AllDayWeekEventsGrid.Children.Add(chip);
            }

            for (int dayIdx = 0; dayIdx < 7; dayIdx++)
            {
                DateTime currentDay = startOfWeek.AddDays(dayIdx);
                var dayEvents = normalEvents.Where(e => e.StartTime.Date == currentDay).ToList();

                var groups = new List<List<CalendarItem>>();
                DateTime maxGroupEnd = DateTime.MinValue;
                List<CalendarItem> currentGroup = null;

                foreach (var ev in dayEvents)
                {
                    if (currentGroup == null || ev.StartTime >= maxGroupEnd)
                    {
                        currentGroup = new List<CalendarItem>();
                        groups.Add(currentGroup);
                        maxGroupEnd = ev.EndTime;
                    }
                    currentGroup.Add(ev);
                    if (ev.EndTime > maxGroupEnd) maxGroupEnd = ev.EndTime;
                }

                foreach (var group in groups)
                {
                    var columns = new List<List<CalendarItem>>();
                    foreach (var ev in group)
                    {
                        bool placed = false;
                        foreach (var col in columns)
                        {
                            if (ev.StartTime >= col.Last().EndTime)
                            {
                                col.Add(ev);
                                placed = true;
                                break;
                            }
                        }
                        if (!placed) columns.Add(new List<CalendarItem> { ev });
                    }

                    double widthPerEvent = (colWidth - 4) / columns.Count;
                    for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                    {
                        foreach (var ev in columns[colIdx])
                        {
                            double startY = (ev.StartTime.Hour * 60) + ev.StartTime.Minute;
                            double endY = (ev.EndTime.Hour * 60) + ev.EndTime.Minute;
                            double height = Math.Max(25, endY - startY);

                            var chip = new Border {
                                Background = ev.BackgroundColor,
                                CornerRadius = new CornerRadius(6),
                                Padding = new Thickness(4, 2, 4, 2),
                                Width = widthPerEvent - 2,
                                Height = height,
                                Tag = ev,
                                Cursor = Cursors.Hand,
                                BorderBrush = Brushes.White,
                                BorderThickness = new Thickness(1),
                                IsHitTestVisible = true
                            };

                            var stp = new StackPanel { IsHitTestVisible = false };
                            
                            bool isReminder = ev.EventType == "REMINDER";
                            bool isCompleted = (ev.OriginalEvent is PersonalEvent pEvent2 && pEvent2.IsCompleted);
                            var txtTitle = new TextBlock { Text = (isReminder ? (isCompleted ? "☑ " : "☐ ") : "") + ev.Title, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis };
                            if (isCompleted) { txtTitle.TextDecorations = TextDecorations.Strikethrough; chip.Opacity = 0.6; }

                            stp.Children.Add(txtTitle);
                            stp.Children.Add(new TextBlock { Text = ev.StartTime.ToString("HH:mm"), Foreground = Brushes.White, FontSize = 9, Opacity = 0.8 });
                            
                            bool isRecurrent = !(ev.OriginalEvent is PersonalEvent) || (ev.OriginalEvent is PersonalEvent pEv2 && !string.IsNullOrEmpty(pEv2.RecurrenceRule) && pEv2.RecurrenceRule != "NONE");
                            if (isRecurrent)
                            {
                                stp.Children.Add(new TextBlock { Text = "🔁", Foreground = Brushes.White, FontSize = 9, Margin = new Thickness(0, 1, 0, 0) });
                            }

                            var chipGrid = new Grid();
                            chipGrid.Children.Add(stp);
                            chip.Child = chipGrid;

                            if (_highlightEventId != null && ev.OriginalEvent is PersonalEvent pEvent && pEvent.IdEvent == _highlightEventId)
                            {
                                var blink = new System.Windows.Media.Animation.DoubleAnimation {
                                    From = 1.0, To = 0.2, Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                                    AutoReverse = true, RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(4)
                                };
                                chip.BeginAnimation(UIElement.OpacityProperty, blink);
                            }

                            AttachChipEvents(chip, ev); // Sử dụng hàm phân phối chuột

                            Canvas.SetTop(chip, startY);
                            Canvas.SetLeft(chip, (dayIdx * colWidth) + (colIdx * widthPerEvent) + 2);
                            WeekEventsCanvas.Children.Add(chip);
                        }
                    }
                }
            }
            
            if (DateTime.Today >= startOfWeek && DateTime.Today <= endOfWeek)
            {
                int todayIdx = (DateTime.Today - startOfWeek).Days;
                double nowY = (DateTime.Now.Hour * 60) + DateTime.Now.Minute;
                
                var nowLine = new Line { X1 = todayIdx * colWidth, X2 = (todayIdx + 1) * colWidth, Y1 = nowY, Y2 = nowY, Stroke = new SolidColorBrush(Color.FromRgb(234, 67, 53)), StrokeThickness = 2 };
                var nowDot = new Ellipse { Fill = new SolidColorBrush(Color.FromRgb(234, 67, 53)), Width = 12, Height = 12, Margin = new Thickness(-6, -6, 0, 0) };
                
                Canvas.SetTop(nowDot, nowY);
                Canvas.SetLeft(nowDot, todayIdx * colWidth);
                
                WeekEventsCanvas.Children.Add(nowLine);
                WeekEventsCanvas.Children.Add(nowDot);
            }
        }
        
        private void WeekEventsCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RbWeekView.IsChecked == true && e.NewSize.Width > 0 && Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 5)
            {
                RenderWeekView();
            }
        }
        
        #endregion

        #region LOGIC XEM THEO NĂM (Year View)

        private void RenderYearView()
        {
            if (YearCalendarContainer == null) return;
            YearCalendarContainer.Children.Clear();

            // Lấy trước sự kiện nguyên năm để tối ưu
            var yearEvents = new List<CalendarItem>();
            for (int m = 1; m <= 12; m++)
            {
                yearEvents.AddRange(GetFilteredEvents(_current.Year, m));
            }
            var datesWithEvents = yearEvents.Select(e => e.StartTime.Date).Distinct().ToHashSet();

            string[] dowNames = { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };

            for (int month = 1; month <= 12; month++)
            {
                var monthContainer = new StackPanel { Margin = new Thickness(15, 10, 15, 20) };
                
                var monthTitle = new TextBlock { 
                    Text = $"tháng {month}", 
                    FontSize = 14, 
                    FontWeight = FontWeights.SemiBold, 
                    Foreground = new SolidColorBrush(Color.FromRgb(60, 64, 67)),
                    Margin = new Thickness(10, 0, 0, 10)
                };
                monthContainer.Children.Add(monthTitle);

                var dowGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 7, Margin = new Thickness(0, 0, 0, 5) };
                foreach (var dow in dowNames)
                {
                    dowGrid.Children.Add(new TextBlock {
                        Text = dow,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(112, 117, 122)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                monthContainer.Children.Add(dowGrid);

                var daysGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 7 };
                DateTime firstDay = new DateTime(_current.Year, month, 1);
                int startDow = ((int)firstDay.DayOfWeek + 6) % 7; 
                int daysInMonth = DateTime.DaysInMonth(_current.Year, month);

                for (int i = 0; i < startDow; i++) daysGrid.Children.Add(new UIElement());

                for (int d = 1; d <= daysInMonth; d++)
                {
                    DateTime date = new DateTime(_current.Year, month, d);
                    bool isToday = date == DateTime.Today;
                    bool hasEvent = datesWithEvents.Contains(date);

                    var dayBorder = new Border {
                        Width = 32, Height = 32,
                        CornerRadius = new CornerRadius(16),
                        Background = isToday ? new SolidColorBrush(Color.FromRgb(26, 115, 232)) : Brushes.Transparent,
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(0, 2, 0, 2)
                    };

                    dayBorder.MouseEnter += (s, e) => {
                        if (!isToday) dayBorder.Background = new SolidColorBrush(Color.FromRgb(241, 243, 244));
                    };
                    dayBorder.MouseLeave += (s, e) => {
                        if (!isToday) dayBorder.Background = Brushes.Transparent;
                    };

                    var dayContent = new Grid();
                    dayContent.Children.Add(new TextBlock {
                        Text = d.ToString(),
                        FontSize = 12,
                        FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                        Foreground = isToday ? Brushes.White : new SolidColorBrush(Color.FromRgb(60, 64, 67)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    if (hasEvent)
                    {
                        dayContent.Children.Add(new Border {
                            Width = 12, Height = 2,
                            CornerRadius = new CornerRadius(1),
                            Background = isToday ? Brushes.White : new SolidColorBrush(Color.FromRgb(26, 115, 232)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Margin = new Thickness(0, 0, 0, 4)
                        });
                    }

                    dayBorder.Child = dayContent;
                    dayBorder.MouseLeftButtonUp += (s, e) => {
                        _current = date;
                        RbScheduleView.IsChecked = true;
                        Render();
                    };

                    daysGrid.Children.Add(dayBorder);
                }

                monthContainer.Children.Add(daysGrid);
                YearCalendarContainer.Children.Add(monthContainer);
            }
        }

        #endregion

        #region DRAG & DROP AND SWEEP CREATION

        private void AttachChipEvents(Border chip, CalendarItem ev)
        {
            chip.PreviewMouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                ShowQuickViewPopup(ev, chip);
            };
            
            // Hiệu ứng Hover làm nổi bật sự kiện
            chip.MouseEnter += (s, e) =>
            {
                Canvas.SetZIndex(chip, 999);
                chip.Effect = new DropShadowEffect { BlurRadius = 10, Opacity = 0.3, ShadowDepth = 2, Direction = 270 };
            };
            chip.MouseLeave += (s, e) =>
            {
                Canvas.SetZIndex(chip, 1);
                chip.Effect = null;
            };
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Kiểm tra xem chuột có đang nằm trên một chip sự kiện đã có không
            if (e.OriginalSource is DependencyObject src)
            {
                DependencyObject current = src;
                while (current != null)
                {
                    if (current is Border b && b.Tag is CalendarItem)
                    {
                        return; // Bỏ qua, không bắt sự kiện tạo mới đè lên chip cũ
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            var canvas = sender as Canvas;
            if (canvas == null) return;

            _isCreating = true;
            var pos = e.GetPosition(canvas);
            
            _createStartY = Math.Floor(pos.Y / 30.0) * 30; // Đổi snap thành 30 phút
            
            Border ghost = canvas.Name == "WeekEventsCanvas" ? WeekGhostEventBorder : GhostEventBorder;
            if (ghost == null) return;
            
            double colWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth / 7.0 : 1;
            int dayIdx = canvas.Name == "WeekEventsCanvas" ? (int)(pos.X / colWidth) : 0;
            
            double left = canvas.Name == "WeekEventsCanvas" ? dayIdx * colWidth : 5;
            double width = canvas.Name == "WeekEventsCanvas" ? colWidth - 2 : canvas.ActualWidth - 10;

            Canvas.SetTop(ghost, _createStartY);
            Canvas.SetLeft(ghost, left);
            ghost.Width = width;
            ghost.Height = 60; // Mặc định 1 giờ khi bắt đầu click
            ghost.Visibility = Visibility.Visible;
            ghost.Opacity = 1.0;
            
            canvas.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var canvas = sender as Canvas;
            if (canvas == null) return;
            Border ghost = canvas.Name == "WeekEventsCanvas" ? WeekGhostEventBorder : GhostEventBorder;
            if (ghost == null) return;

            // Kiểm tra xem chuột có đang nằm trên một chip sự kiện đã có không
            if (e.OriginalSource is DependencyObject src)
            {
                DependencyObject current = src;
                while (current != null)
                {
                    if (current is Border b && b.Tag is CalendarItem)
                    {
                        if (!_isCreating) ghost.Visibility = Visibility.Collapsed;
                        return;
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            if (_isCreating)
            {
                var pos = e.GetPosition(canvas);
                double currentY = Math.Max(0, pos.Y);
                double snappedY = Math.Ceiling(currentY / 30.0) * 30; // Snap 30 phút
                
                double top = Math.Min(_createStartY, snappedY);
                double height = Math.Abs(snappedY - _createStartY);
                if (height == 0) height = 30;

                Canvas.SetTop(ghost, top);
                ghost.Height = height;
            }
            else
            {
                var pos = e.GetPosition(canvas);
                double snappedY = Math.Floor(pos.Y / 30.0) * 30; // Snap mỗi 30 phút
                
                double colWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth / 7.0 : 1;
                int dayIdx = canvas.Name == "WeekEventsCanvas" ? (int)(pos.X / colWidth) : 0;
                double left = canvas.Name == "WeekEventsCanvas" ? dayIdx * colWidth : 5;
                double width = canvas.Name == "WeekEventsCanvas" ? colWidth - 2 : canvas.ActualWidth - 10;

                Canvas.SetTop(ghost, snappedY);
                Canvas.SetLeft(ghost, left);
                ghost.Width = width;
                ghost.Height = 60; // 60 phút hover (1 giờ)
                ghost.Visibility = Visibility.Visible;
                ghost.Opacity = 0.3; // Mờ đi khi hover
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            if (canvas == null || !_isCreating) return;

            _isCreating = false;
            canvas.ReleaseMouseCapture();
            
            Border ghost = canvas.Name == "WeekEventsCanvas" ? WeekGhostEventBorder : GhostEventBorder;

            double top = Canvas.GetTop(ghost);
            double height = ghost.Height;
            if (height < 30) height = 60; // Mặc định thời lượng là 1 tiếng (60 phút) nếu chỉ click chuột

            int startHour = (int)(top / 60);
            int startMin = (int)(top % 60);
            int endHour = (int)((top + height) / 60);
            int endMin = (int)((top + height) % 60);

            DateTime targetDate = _current.Date;
            if (canvas.Name == "WeekEventsCanvas")
            {
                double colWidth = canvas.ActualWidth / 7.0;
                int dayIdx = (int)Math.Round(Canvas.GetLeft(ghost) / colWidth); // Dùng Math.Round để chống sai số thập phân
                int diff = (7 + (_current.DayOfWeek - DayOfWeek.Monday)) % 7;
                targetDate = _current.Date.AddDays(-diff + dayIdx);
            }

            DateTime startTime = targetDate.AddHours(startHour).AddMinutes(startMin);
            DateTime endTime = targetDate.AddHours(endHour).AddMinutes(endMin);

            _pendingCreateEvent = new PersonalEvent { 
                IdAcc = SessionManager.CurrentAccount.IdAcc, 
                StartTime = startTime, 
                EndTime = endTime, 
                EventType = "PERSONAL" 
            };
            
            QuickTitle.Text = "";
            QuickDateDisplay.Text = _pendingCreateEvent.StartTime.ToString("dddd, d MMMM");
            QuickStartTime.Text = _pendingCreateEvent.StartTime.ToString("HH:mm");
            QuickEndTime.Text = _pendingCreateEvent.EndTime.ToString("HH:mm");
            
            QuickCreatePopup.PlacementTarget = ghost;
            QuickCreatePopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            QuickCreatePopup.IsOpen = true;

            Dispatcher.BeginInvoke(new Action(() => {
                QuickTitle.Focus();
                System.Windows.Input.Keyboard.Focus(QuickTitle);
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isCreating)
            {
                var canvas = sender as Canvas;
                Border ghost = canvas?.Name == "WeekEventsCanvas" ? WeekGhostEventBorder : GhostEventBorder;
                if (ghost != null) ghost.Visibility = Visibility.Collapsed;
            }
        }

        private void WeekViewScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Nhấn đè SHIFT + Lăn chuột sẽ cuộn qua lại giữa các tuần
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (e.Delta < 0) BtnNext_Click(null, null);
                else BtnPrev_Click(null, null);
                e.Handled = true;
            }
        }
        #endregion

        #region EVENT HANDLERS & HELPERS

        private Border CreateEventChip(CalendarItem ev)
        {
            var chip = new Border { 
                Background = ev.BackgroundColor, 
                CornerRadius = new CornerRadius(4), 
                Padding = new Thickness(6, 2, 6, 2), 
                Margin = new Thickness(0, 0, 0, 2), 
                Tag = ev, 
                Cursor = Cursors.Hand 
            };
            
            bool isReminder = ev.EventType == "REMINDER";
            bool isCompleted = (ev.OriginalEvent is PersonalEvent peReminder && peReminder.IsCompleted);
            string titlePrefix = isReminder ? (isCompleted ? "☑ " : "☐ ") : "";

            var text = new TextBlock { Text = titlePrefix + ev.Title, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis };
            
            if (isCompleted) { text.TextDecorations = TextDecorations.Strikethrough; chip.Opacity = 0.6; }

            if (ev.OriginalEvent is PersonalEvent pe && _guestStatuses.ContainsKey(pe.IdEvent) && _guestStatuses[pe.IdEvent] == "PENDING")
            {
                chip.Background = Brushes.White;
                chip.BorderBrush = ev.BackgroundColor;
                chip.BorderThickness = new Thickness(1);
                text.Foreground = ev.BackgroundColor;
            }
            chip.Child = text;
            chip.PreviewMouseLeftButtonUp += (s, e) => { e.Handled = true; ShowQuickViewPopup(ev, chip); };
            return chip;
        }

        // ==========================================
        // LOGIC MỚI CHO SIDEBAR VÀ POPOVER
        // ==========================================

        private void MiniCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MiniCalendar.SelectedDate.HasValue && MiniCalendar.SelectedDate.Value != _current)
            {
                _current = MiniCalendar.SelectedDate.Value;
                Render();
            }
        }

        private void ShowQuickViewPopup(CalendarItem item, UIElement targetElement)
        {
            _selectedPopupItem = item;
            
            PopupEventTitle.Text = item.Title;
            PopupEventTime.Text = $"{item.StartTime:dd/MM/yyyy HH:mm} - {item.EndTime:HH:mm}";
            
            if (!string.IsNullOrWhiteSpace(item.Location)) {
                PopupEventLocation.Text = item.Location;
                PopupLocationPanel.Visibility = Visibility.Visible;
            } else {
                PopupLocationPanel.Visibility = Visibility.Collapsed;
            }

            // Check quyền chỉnh sửa
            bool canEdit = item.OriginalEvent is PersonalEvent p && !_guestStatuses.ContainsKey(p.IdEvent);
            BtnQuickEdit.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            BtnQuickDelete.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;

            // Nút Tham gia/Từ chối cho Khách
            if (item.OriginalEvent is PersonalEvent pe && _guestStatuses.ContainsKey(pe.IdEvent)) {
                RsvpPanel.Visibility = Visibility.Visible;
                BtnAccept.Visibility = _guestStatuses[pe.IdEvent] != "ACCEPTED" ? Visibility.Visible : Visibility.Collapsed;
                BtnDecline.Visibility = _guestStatuses[pe.IdEvent] != "DECLINED" ? Visibility.Visible : Visibility.Collapsed;
            } else {
                RsvpPanel.Visibility = Visibility.Collapsed;
            }
            
            if (item.EventType == "REMINDER" && item.OriginalEvent is PersonalEvent peRem)
            {
                ChkQuickCompleted.Visibility = Visibility.Visible;
                ChkQuickCompleted.Checked -= ChkQuickCompleted_CheckedChanged;
                ChkQuickCompleted.Unchecked -= ChkQuickCompleted_CheckedChanged;
                ChkQuickCompleted.IsChecked = peRem.IsCompleted;
                ChkQuickCompleted.Tag = peRem;
                ChkQuickCompleted.Checked += ChkQuickCompleted_CheckedChanged;
                ChkQuickCompleted.Unchecked += ChkQuickCompleted_CheckedChanged;
            }
            else
            {
                ChkQuickCompleted.Visibility = Visibility.Collapsed;
            }

            QuickEventPopup.PlacementTarget = targetElement;
            QuickEventPopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            PopupOverlay.Visibility = Visibility.Visible;
            QuickEventPopup.IsOpen = true;
        }
        
        private void ChkQuickCompleted_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk && chk.Tag is PersonalEvent pe)
            {
                pe.IsCompleted = chk.IsChecked == true;
                _bll.Save(pe);
                Render();
            }
        }

        private void BtnQuickAddAtSameTime_Click(object sender, RoutedEventArgs e)
        {
            QuickEventPopup.IsOpen = false;
            if (_selectedPopupItem != null)
            {
                OpenNewEvent(_selectedPopupItem.StartTime.Date, null, _selectedPopupItem.StartTime.TimeOfDay);
            }
        }

        private void BtnQuickEdit_Click(object sender, RoutedEventArgs e)
        {
            QuickEventPopup.IsOpen = false;
            if (_selectedPopupItem != null) OpenEventDialog(_selectedPopupItem);
        }

        private void BtnQuickDelete_Click(object sender, RoutedEventArgs e)
        {
            QuickEventPopup.IsOpen = false;
            if (_selectedPopupItem?.OriginalEvent is PersonalEvent p)
            {
                if (!string.IsNullOrEmpty(p.GroupId))
                {
                    var resAc = MessageBox.Show($"Sự kiện này thuộc một nhóm các sự kiện liên kết.\n\nBạn có muốn xóa TOÀN BỘ các sự kiện trong nhóm này không?\n\n- Chọn 'Yes' để xóa toàn bộ.\n- Chọn 'No' để chỉ xóa sự kiện hiện tại.\n- Chọn 'Cancel' để hủy.", "Xác nhận xóa", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (resAc == MessageBoxResult.Cancel) return;
                    if (resAc == MessageBoxResult.Yes)
                    {
                        _bll.DeleteEventGroup(p.IdAcc, p.GroupId);
                        Render();
                        return;
                    }
                }
                else if (p.EventType == "ACADEMIC")
                {
                    var resAc = MessageBox.Show($"Sự kiện này là lịch học của môn '{p.Title}'.\n\nBạn có muốn xóa TOÀN BỘ các lịch học của môn này không?\n\n- Chọn 'Yes' để xóa toàn bộ.\n- Chọn 'No' để chỉ xóa khung giờ này.\n- Chọn 'Cancel' để hủy.", "Xác nhận xóa", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (resAc == MessageBoxResult.Cancel) return;
                    if (resAc == MessageBoxResult.Yes)
                    {
                        _bll.DeleteRelatedEvents(p.IdAcc, p.Title, p.EventType);
                        Render();
                        return;
                    }
                }

                var res = MessageBox.Show("Bạn có chắc muốn xóa sự kiện này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes) {
                    _bll.Delete(p.IdEvent);
                    Render();
                }
            }
        }

        private void UpdateRsvpStatus(string status)
        {
            if (_selectedPopupItem?.OriginalEvent is PersonalEvent p) {
                try
                {
                    using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString)) {
                        conn.Open();
                        var cmd = new System.Data.SqlClient.SqlCommand("UPDATE EVENT_ATTENDEE SET response_status = @st WHERE id_event = @id AND id_acc = @uid", conn);
                        cmd.Parameters.AddWithValue("@st", status); cmd.Parameters.AddWithValue("@id", p.IdEvent); cmd.Parameters.AddWithValue("@uid", SessionManager.CurrentAccount.IdAcc);
                        cmd.ExecuteNonQuery();
                    }
                    QuickEventPopup.IsOpen = false; Render();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi cập nhật trạng thái: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void BtnAccept_Click(object sender, RoutedEventArgs e) => UpdateRsvpStatus("ACCEPTED");
        private void BtnDecline_Click(object sender, RoutedEventArgs e) => UpdateRsvpStatus("DECLINED");
        private void ShowPopupAllEvents(DateTime date, List<CalendarItem> events, UIElement target)
        {
            var popup = new System.Windows.Controls.Primitives.Popup { AllowsTransparency = true, StaysOpen = false, PlacementTarget = target };
            var border = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(8), Padding = new Thickness(10), BorderThickness = new Thickness(1), BorderBrush = Brushes.LightGray };
            border.Effect = new DropShadowEffect { BlurRadius = 10, Opacity = 0.2 };
            var stack = new StackPanel { Width = 160 };
            stack.Children.Add(new TextBlock { Text = date.ToString("dd/MM"), FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,0,5) });
            foreach (var ev in events) stack.Children.Add(CreateEventChip(ev));
            border.Child = stack; popup.Child = border; popup.IsOpen = true;
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (RbScheduleView.IsChecked == true) _current = _current.AddDays(-1);
            else if (RbWeekView.IsChecked == true) _current = _current.AddDays(-7);
            else if (RbYearView.IsChecked == true) _current = _current.AddYears(-1);
            else _current = _current.AddMonths(-1);
            Render();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (RbScheduleView.IsChecked == true) _current = _current.AddDays(1);
            else if (RbWeekView.IsChecked == true) _current = _current.AddDays(7);
            else if (RbYearView.IsChecked == true) _current = _current.AddYears(1);
            else _current = _current.AddMonths(1);
            Render();
        }

        private void BtnToday_Click(object sender, RoutedEventArgs e) { _current = DateTime.Today; Render(); }

        private void BtnImportTKB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == true)
                {
                    string jsonString = System.IO.File.ReadAllText(openFileDialog.FileName);
                    var doc = Newtonsoft.Json.Linq.JArray.Parse(jsonString);
                    
                    DateTime startOfWeek = DateTime.Today;
                    while (startOfWeek.DayOfWeek != DayOfWeek.Monday) startOfWeek = startOfWeek.AddDays(-1);

                    string[] starts = { "07:00", "08:00", "09:00", "10:00", "11:00", "13:00", "14:00", "15:00", "16:00", "17:00" };
                    string[] ends = { "07:50", "08:50", "09:50", "10:50", "11:50", "13:50", "14:50", "15:50", "16:50", "17:50" };

                    int count = 0;
                    foreach (var element in doc)
                    {
                        string courseName = element["CourseName"]?.ToString();
                        string classCode = element["ClassCode"]?.ToString();
                        string group = element["Group"]?.ToString();
                        string lecturer = element["LecturerName"]?.ToString();
                        string scheduleStr = element["ScheduleStr"]?.ToString();
                        string roomStr = element["RoomStr"]?.ToString();

                        if (string.IsNullOrWhiteSpace(scheduleStr)) continue;

                        var roomDict = new Dictionary<int, string>();
                        if (!string.IsNullOrWhiteSpace(roomStr))
                        {
                            var rParts = roomStr.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var rp in rParts)
                            {
                                var m = System.Text.RegularExpressions.Regex.Match(rp, @"Thứ (\d):\s*(.+)");
                                if (m.Success) roomDict[int.Parse(m.Groups[1].Value)] = m.Groups[2].Value.Trim();
                            }
                        }

                        var sParts = scheduleStr.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var sp in sParts)
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(sp, @"Thứ (\d):\s*(\d+)-(\d+)");
                            if (m.Success)
                            {
                                int day = int.Parse(m.Groups[1].Value);
                                int startPeriod = int.Parse(m.Groups[2].Value);
                                int endPeriod = int.Parse(m.Groups[3].Value);

                                if (startPeriod >= 1 && startPeriod <= 10 && endPeriod >= 1 && endPeriod <= 10)
                                {
                                    string startTimeStr = starts[startPeriod - 1];
                                    string endTimeStr = ends[endPeriod - 1];

                                    DateTime eventDate = startOfWeek.AddDays(day - 2);
                                    DateTime eventStart = eventDate.Add(TimeSpan.Parse(startTimeStr));
                                    DateTime eventEnd = eventDate.Add(TimeSpan.Parse(endTimeStr));
                                    string room = roomDict.ContainsKey(day) ? roomDict[day] : "";

                                    var ev = new PersonalEvent
                                    {
                                        IdAcc = SessionManager.CurrentAccount.IdAcc,
                                        Title = courseName,
                                        Description = $"Mã lớp: {classCode}\nNhóm: {group}\nGiảng viên: {lecturer}",
                                        Location = room,
                                        StartTime = eventStart,
                                        EndTime = eventEnd,
                                        EventType = "ACADEMIC",
                                        ColorCategory = "#EA4335",
                                        RecurrenceRule = "FREQ=WEEKLY;INTERVAL=1;COUNT=15",
                                        IsAllDay = false
                                    };

                                    _bll.Save(ev, 15); // Add event with 15 mins reminder
                                    count++;
                                }
                            }
                        }
                    }

                    MessageBox.Show($"Nhập thành công {count} lịch học vào Thời khóa biểu!\nLịch đã được lên tự động lặp lại cho 15 tuần tiếp theo.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    Render();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi khi nhập file JSON: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnMonthSelector_Click(object sender, RoutedEventArgs e)
        {
            var popup = new System.Windows.Controls.Primitives.Popup 
            { 
                PlacementTarget = (UIElement)sender, 
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade
            };

            var calendar = new System.Windows.Controls.Calendar 
            { 
                DisplayDate = _current, 
                SelectedDate = _current,
                IsTodayHighlighted = true,
                Style = (Style)FindResource("GoogleCalendarStyle")
            };
            
            calendar.SelectedDatesChanged += (s, ev) => 
            {
                if (calendar.SelectedDate.HasValue) 
                {
                    _current = calendar.SelectedDate.Value;
                    Render();
                    popup.IsOpen = false;
                }
            };

            var border = new Border 
            { 
                Background = Brushes.White, 
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), 
                BorderThickness = new Thickness(1), 
                CornerRadius = new CornerRadius(8), 
                Padding = new Thickness(5),
                Child = calendar,
                Effect = new DropShadowEffect { BlurRadius = 15, Opacity = 0.15, ShadowDepth = 4 }
            };

            popup.Child = border;
            popup.IsOpen = true;
        }

        private void SwitchView_Click(object sender, RoutedEventArgs e) => Render();

        private void OpenNewEvent(DateTime date, UIElement target = null, TimeSpan? time = null)
        {
            TimeSpan startT = time ?? TimeSpan.FromHours(DateTime.Now.Hour);
            _pendingCreateEvent = new PersonalEvent { 
                IdAcc = SessionManager.CurrentAccount.IdAcc, 
                StartTime = date.Date.Add(startT), 
                EndTime = date.Date.Add(startT).AddHours(1), 
                EventType = "PERSONAL" 
            };
            
            QuickTitle.Text = "";
            QuickDateDisplay.Text = _pendingCreateEvent.StartTime.ToString("dddd, d MMMM");
            QuickStartTime.Text = _pendingCreateEvent.StartTime.ToString("HH:mm");
            QuickEndTime.Text = _pendingCreateEvent.EndTime.ToString("HH:mm");
            
            // Đặt mặc định là loại Cá nhân
            QuickEventType.SelectedIndex = 0;
            LoadQuickTags();

            if (target != null) {
                QuickCreatePopup.PlacementTarget = target;
                QuickCreatePopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            } else {
                QuickCreatePopup.PlacementTarget = MonthViewGrid;
                QuickCreatePopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
            }
            
            PopupOverlay.Visibility = Visibility.Visible;
            QuickCreatePopup.IsOpen = true;
            Dispatcher.BeginInvoke(new Action(() => {
                QuickTitle.Focus();
                System.Windows.Input.Keyboard.Focus(QuickTitle);
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void QuickEventType_SelectionChanged(object sender, SelectionChangedEventArgs e) { LoadQuickTags(); }

        private void QuickTagCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateQuickTagsComboBoxText();
        }

        private void UpdateQuickTagsComboBoxText()
        {
            if (QuickTagsDropdownToggle == null) return;
            var selected = _quickAvailableTags.Where(t => t.IsSelected).Select(t => t.TagName).ToList();
            if (selected.Count == 0) QuickTagsDropdownToggle.Tag = "Chọn Tag...";
            else if (selected.Count == 1) QuickTagsDropdownToggle.Tag = selected[0];
            else QuickTagsDropdownToggle.Tag = $"{selected[0]} (+{selected.Count - 1})";
        }

        private void LoadQuickTags()
        {
            if (QuickTagsListControl == null || SessionManager.CurrentAccount == null) return;
            string eventType = ((ComboBoxItem)QuickEventType.SelectedItem)?.Tag?.ToString() ?? "PERSONAL";
            var dal = new StudentReminderApp.DAL.EventDAL();
            var allTags = dal.GetTags(SessionManager.CurrentAccount.IdAcc).Where(t => t.TagType == eventType).ToList();
            
            _quickAvailableTags.Clear();
            foreach (var t in allTags) _quickAvailableTags.Add(new TagSelectionItem { IdTag = t.IdTag, TagName = t.TagName, IsSelected = false });
            
            QuickTagsListControl.ItemsSource = null;
            QuickTagsListControl.ItemsSource = _quickAvailableTags;
            UpdateQuickTagsComboBoxText();
            QuickTagsContainer.Visibility = _quickAvailableTags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SaveQuickEvent_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingCreateEvent != null)
            {
                _pendingCreateEvent.Title = string.IsNullOrWhiteSpace(QuickTitle.Text) ? "(Không có tiêu đề)" : QuickTitle.Text;
                
                if (QuickEventType.SelectedItem is ComboBoxItem selectedType)
                {
                    _pendingCreateEvent.EventType = selectedType.Tag.ToString();
                    if (_pendingCreateEvent.EventType == "REMINDER") _pendingCreateEvent.ColorCategory = "#3F51B5";
                    else if (_pendingCreateEvent.EventType == "ACADEMIC") _pendingCreateEvent.ColorCategory = "#D93025";
                    else _pendingCreateEvent.ColorCategory = "#1A73E8";
                }
                
                if (TimeSpan.TryParse(QuickStartTime.Text, out TimeSpan st))
                    _pendingCreateEvent.StartTime = _pendingCreateEvent.StartTime.Date.Add(st);
                if (TimeSpan.TryParse(QuickEndTime.Text, out TimeSpan et))
                    _pendingCreateEvent.EndTime = _pendingCreateEvent.EndTime.Date.Add(et);

                DateTime localStart = _pendingCreateEvent.StartTime;

                // Set default UTC để chuẩn hóa DB
                _pendingCreateEvent.StartTime = _pendingCreateEvent.StartTime.ToUniversalTime();
                _pendingCreateEvent.EndTime = _pendingCreateEvent.EndTime.ToUniversalTime();

                _bll.Save(_pendingCreateEvent);
                
                // Lưu Tags
                long eventId = _pendingCreateEvent.IdEvent;
                if (eventId == 0) // Lấy ID mới nhất nếu vừa Insert
                {
                    using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString)) {
                        conn.Open();
                        using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT TOP 1 id_event FROM PERSONAL_EVENT WHERE id_acc = @uid ORDER BY id_event DESC", conn)) {
                            cmd.Parameters.AddWithValue("@uid", _pendingCreateEvent.IdAcc);
                            var obj = cmd.ExecuteScalar();
                            if (obj != null) eventId = Convert.ToInt64(obj);
                        }
                    }
                }
                var dal = new StudentReminderApp.DAL.EventDAL();
                var selectedTagIds = _quickAvailableTags.Where(t => t.IsSelected).Select(t => t.IdTag).ToList();
                dal.SaveTagIdsForEvent(eventId, selectedTagIds);

                QuickCreatePopup.IsOpen = false;
                _current = localStart.Date; // Tự động nhảy lịch
                Render();
            }
        }

        private void MoreOptions_Click(object sender, RoutedEventArgs e)
        {
            QuickCreatePopup.IsOpen = false;
            if (_pendingCreateEvent != null)
            {
                _pendingCreateEvent.Title = QuickTitle.Text;
                
                if (QuickEventType.SelectedItem is ComboBoxItem selectedType)
                {
                    _pendingCreateEvent.EventType = selectedType.Tag.ToString();
                    if (_pendingCreateEvent.EventType == "REMINDER") _pendingCreateEvent.ColorCategory = "#3F51B5";
                    else if (_pendingCreateEvent.EventType == "ACADEMIC") _pendingCreateEvent.ColorCategory = "#D93025";
                    else _pendingCreateEvent.ColorCategory = "#1A73E8";
                }

                if (TimeSpan.TryParse(QuickStartTime.Text, out TimeSpan st))
                    _pendingCreateEvent.StartTime = _pendingCreateEvent.StartTime.Date.Add(st);
                if (TimeSpan.TryParse(QuickEndTime.Text, out TimeSpan et))
                    _pendingCreateEvent.EndTime = _pendingCreateEvent.EndTime.Date.Add(et);

                var selectedTags = _quickAvailableTags.Where(t => t.IsSelected).Select(t => t.IdTag).ToList();
                var dlg = new EventDialog(_pendingCreateEvent, selectedTags) { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() == true) 
                {
                    _current = _pendingCreateEvent.StartTime.Kind == DateTimeKind.Utc ? _pendingCreateEvent.StartTime.ToLocalTime().Date : _pendingCreateEvent.StartTime.Date;
                    Render();
                }
            }
        }

        private void CloseQuickCreate_Click(object sender, RoutedEventArgs e)
        {
            QuickCreatePopup.IsOpen = false;
        }

        private void BtnCreateEvent_Click(object sender, RoutedEventArgs e)
        {
            OpenNewEvent(_current.Date, (UIElement)sender); // Đặt mặc định là ngày/tháng đang xem thay vì luôn là ngày hôm nay
        }

        private void PopupOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (QuickCreatePopup != null) QuickCreatePopup.IsOpen = false;
            if (QuickEventPopup != null) QuickEventPopup.IsOpen = false;
            if (SearchResultsPopup != null) SearchResultsPopup.IsOpen = false;
        }

        private void QuickPopup_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (sender is System.Windows.Controls.Primitives.Popup popup) popup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void QuickEventPopup_Closed(object sender, EventArgs e)
        {
            if (PopupOverlay != null) PopupOverlay.Visibility = Visibility.Collapsed;
        }

        private void QuickCreatePopup_Closed(object sender, EventArgs e)
        {
            if (PopupOverlay != null) PopupOverlay.Visibility = Visibility.Collapsed;
            if (GhostEventBorder != null) GhostEventBorder.Visibility = Visibility.Collapsed;
            if (WeekGhostEventBorder != null) WeekGhostEventBorder.Visibility = Visibility.Collapsed;
        }

        private void BtnEventDetail_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CalendarItem ci) OpenEventDialog(ci); }

        private void LoadTags()
        {
            if (SessionManager.CurrentAccount == null) return;
            
            PersonalTagsPanel.Children.Clear();
            AcademicTagsPanel.Children.Clear();
            ReminderTagsPanel.Children.Clear();
            
            var dal = new StudentReminderApp.DAL.EventDAL();
            var tags = dal.GetTags(SessionManager.CurrentAccount.IdAcc);
            
            foreach (var t in tags)
            {
                if (t.TagType == "PERSONAL") CreateNewTag(PersonalTagsPanel, "PERSONAL", t.TagName, t.IdTag);
                else if (t.TagType == "ACADEMIC") CreateNewTag(AcademicTagsPanel, "ACADEMIC", t.TagName, t.IdTag);
                else if (t.TagType == "REMINDER") CreateNewTag(ReminderTagsPanel, "REMINDER", t.TagName, t.IdTag);
            }
        }

        private void BtnTogglePersonal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) {
                bool isCol = PersonalTagsPanel.Visibility == Visibility.Visible;
                PersonalTagsPanel.Visibility = isCol ? Visibility.Collapsed : Visibility.Visible;
                btn.Content = isCol ? "\xE76C" : "\xE70D";
            }
        }

        private void BtnToggleAcademic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) {
                bool isCol = AcademicTagsPanel.Visibility == Visibility.Visible;
                AcademicTagsPanel.Visibility = isCol ? Visibility.Collapsed : Visibility.Visible;
                btn.Content = isCol ? "\xE76C" : "\xE70D";
            }
        }

        private void BtnToggleReminder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) {
                bool isCol = ReminderTagsPanel.Visibility == Visibility.Visible;
                ReminderTagsPanel.Visibility = isCol ? Visibility.Collapsed : Visibility.Visible;
                btn.Content = isCol ? "\xE76C" : "\xE70D";
            }
        }

        private void BtnAddPersonalTag_Click(object sender, RoutedEventArgs e) => CreateNewTag(PersonalTagsPanel, "PERSONAL");
        private void BtnAddAcademicTag_Click(object sender, RoutedEventArgs e) => CreateNewTag(AcademicTagsPanel, "ACADEMIC");
        private void BtnAddReminderTag_Click(object sender, RoutedEventArgs e) => CreateNewTag(ReminderTagsPanel, "REMINDER");

        private void CreateNewTag(StackPanel parentPanel, string tagType, string tagName = "Tag mới", long tagId = 0)
        {
            var dal = new StudentReminderApp.DAL.EventDAL();
            if (tagId == 0)
            {
                tagId = dal.InsertTag(new EventTag { IdAcc = SessionManager.CurrentAccount.IdAcc, TagType = tagType, TagName = tagName });
            }

            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4), Background = Brushes.Transparent };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var contentText = new TextBlock { Text = tagName, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 140 };
            var chk = new CheckBox { Content = contentText, IsChecked = true, Foreground = new SolidColorBrush(Color.FromRgb(60, 64, 67)), FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
            chk.Tag = tagId;
            chk.Checked += Filter_Changed;
            chk.Unchecked += Filter_Changed;
            
            var btnEdit = new Button { Content = "\xE70F", FontFamily = new FontFamily("Segoe MDL2 Assets"), Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.Gray, Cursor = Cursors.Hand, Visibility = Visibility.Collapsed, Width = 22, ToolTip = "Sửa", FocusVisualStyle = null };
            var btnDel = new Button { Content = "\xE74D", FontFamily = new FontFamily("Segoe MDL2 Assets"), Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.Gray, Cursor = Cursors.Hand, Visibility = Visibility.Collapsed, Width = 22, ToolTip = "Xóa", FocusVisualStyle = null };

            // Chỉ hiện nút Sửa và Xóa khi hover
            grid.MouseEnter += (s, e) => { btnEdit.Visibility = Visibility.Visible; btnDel.Visibility = Visibility.Visible; };
            grid.MouseLeave += (s, e) => { btnEdit.Visibility = Visibility.Collapsed; btnDel.Visibility = Visibility.Collapsed; };

            // Logic Xóa
            btnDel.Click += (s, e) => {
                dal.DeleteTag(tagId);
                parentPanel.Children.Remove(grid); 
                Filter_Changed(null, null);
            };
            
            // Logic Sửa (Inline Edit)
            btnEdit.Click += (s, e) => {
                var editBox = new TextBox { Text = ((TextBlock)chk.Content).Text, VerticalAlignment = VerticalAlignment.Center, FontSize = 13, Padding = new Thickness(2,0,2,0) };
                Grid.SetColumn(editBox, 0);
                grid.Children.Remove(chk);
                grid.Children.Add(editBox);
                editBox.Focus();
                editBox.SelectAll();
                
                editBox.LostFocus += (s2, e2) => {
                    string newName = string.IsNullOrWhiteSpace(editBox.Text) ? "Tag mới" : editBox.Text;
                    ((TextBlock)chk.Content).Text = newName;
                    dal.UpdateTag(tagId, newName);
                    grid.Children.Remove(editBox);
                    grid.Children.Add(chk);
                };
                editBox.KeyDown += (s2, e2) => {
                    if (e2.Key == Key.Enter) editBox.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
                };
            };

            Grid.SetColumn(chk, 0);
            Grid.SetColumn(btnEdit, 1);
            Grid.SetColumn(btnDel, 2);

            grid.Children.Add(chk);
            grid.Children.Add(btnEdit);
            grid.Children.Add(btnDel);

            parentPanel.Children.Add(grid);
        }

        #endregion
    }
}