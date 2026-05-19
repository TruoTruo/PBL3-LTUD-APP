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

        public CalendarPage()
        {
            InitializeComponent();
            
            // Khởi tạo danh sách 24 múi giờ cho trục bên trái (00:00 -> 23:00)
            var hours = new List<object>();
            for (int i = 0; i < 24; i++) hours.Add(new { HourText = $"{i:D2}:00" });
            HourLinesControl.ItemsSource = hours;

            // Render lần đầu khi Page được load
            Loaded += (s, e) => Render();
        }

        // Hàm điều phối chính
        private void Render()
        {
            if (TxtMonthYear == null) return;
            TxtMonthYear.Text = _current.ToString("MMMM yyyy");

            if (RbMonthView.IsChecked == true)
            {
                HeaderDateInfo.Visibility = Visibility.Collapsed;
                MonthViewGrid.Visibility = Visibility.Visible;
                ScheduleViewScroll.Visibility = Visibility.Collapsed;
                RenderMonthView();
            }
            else
            {
                HeaderDateInfo.Visibility = Visibility.Visible;
                MonthViewGrid.Visibility = Visibility.Collapsed;
                ScheduleViewScroll.Visibility = Visibility.Visible;
                RenderTimeGridView();
            }
        }

        #region LOGIC XEM THEO THÁNG (UniformGrid)

        private void RenderMonthView()
        {
            CalendarGrid.Children.Clear();
            var events = _bll.GetCalendarItemsForMonth(SessionManager.CurrentAccount.IdAcc, _current.Year, _current.Month);

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
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = isToday ? new SolidColorBrush(Color.FromRgb(239, 246, 255)) : Brushes.White,
                Padding = new Thickness(6), Cursor = Cursors.Hand
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock {
                Text = date.Day.ToString(), FontSize = 13, Margin = new Thickness(0, 0, 0, 4),
                Foreground = isToday ? new SolidColorBrush(Color.FromRgb(37, 99, 235)) : 
                             isOtherMonth ? new SolidColorBrush(Color.FromRgb(148, 163, 184)) : Brushes.Black
            });

            foreach (var ev in events.Take(3)) panel.Children.Add(CreateEventChip(ev));

            if (events.Count > 3)
            {
                var more = new TextBlock { Text = $"+{events.Count - 3} khác", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Brushes.Gray };
                more.MouseUp += (s, e) => {
                    e.Handled = true;
                    ShowPopupAllEvents(date, events, (UIElement)s);
                };
                panel.Children.Add(more);
            }

            cell.Child = panel;
            cell.MouseUp += (s, e) => { if (!e.Handled) OpenNewEvent(date); };
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
            TxtCurrentDayNumber.Foreground = _current.Date == DateTime.Today ? 
                new SolidColorBrush(Color.FromRgb(26, 115, 232)) : new SolidColorBrush(Color.FromRgb(60, 64, 67));

            // 2. Làm sạch và chuẩn bị Canvas
            EventsCanvas.Children.Clear();
            EventsCanvas.Background = Brushes.Transparent; // Quan trọng để bắt chuột

            // 3. Lấy dữ liệu sự kiện
            var events = _bll.GetCalendarItemsForMonth(SessionManager.CurrentAccount.IdAcc, _current.Year, _current.Month)
                            .Where(e => e.StartTime.Date == _current.Date)
                            .OrderBy(e => e.StartTime)
                            .ToList();

            if (events.Count == 0) { UpdateNowLine(); return; }

            // 4. Chia cột cho sự kiện trùng giờ
            List<List<CalendarItem>> groups = new List<List<CalendarItem>>();
            foreach (var ev in events)
            {
                bool placed = false;
                foreach (var group in groups)
                {
                    if (ev.StartTime < group.Max(x => x.EndTime)) { group.Add(ev); placed = true; break; }
                }
                if (!placed) groups.Add(new List<CalendarItem> { ev });
            }

            double canvasWidth = EventsCanvas.ActualWidth > 0 ? EventsCanvas.ActualWidth : 500;

            foreach (var group in groups)
            {
                double widthPerEvent = (canvasWidth - 10) / group.Count;
                for (int i = 0; i < group.Count; i++)
                {
                    var ev = group[i];
                    double startY = (ev.StartTime.Hour * 60) + ev.StartTime.Minute;
                    double endY = (ev.EndTime.Hour * 60) + ev.EndTime.Minute;
                    double height = Math.Max(25, endY - startY);

                    // TẠO THẺ CHIP
                    var chip = new Border {
                        Background = ev.BackgroundColor,
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 4, 8, 4),
                        Width = widthPerEvent - 4,
                        Height = height,
                        Tag = ev, // Gắn object vào Tag
                        Cursor = Cursors.Hand,
                        BorderBrush = Brushes.White,
                        BorderThickness = new Thickness(1),
                        IsHitTestVisible = true
                    };

                    var stp = new StackPanel { IsHitTestVisible = false };
                    stp.Children.Add(new TextBlock { Text = ev.Title, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis });
                    stp.Children.Add(new TextBlock { Text = ev.StartTime.ToString("HH:mm"), Foreground = Brushes.White, FontSize = 10, Opacity = 0.8 });
                    chip.Child = stp;

                    // ĐĂNG KÝ SỰ KIỆN CLICK (Dùng duy nhất tên hàm chuẩn bên dưới)
                    chip.PreviewMouseLeftButtonUp += (s, e) => {
                        e.Handled = true; 
                        if ((s as Border)?.Tag is CalendarItem cItem) {
                            OpenEventDialog(cItem); 
                        }
                    };

                    Canvas.SetTop(chip, startY);
                    Canvas.SetLeft(chip, i * widthPerEvent + 5);
                    EventsCanvas.Children.Add(chip);
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

        #region EVENT HANDLERS & HELPERS

        private Border CreateEventChip(CalendarItem ev)
        {
            var chip = new Border { Background = ev.BackgroundColor, CornerRadius = new CornerRadius(4), Padding = new Thickness(4, 1, 4, 1), Margin = new Thickness(0, 0, 0, 1) };
            chip.Child = new TextBlock { Text = ev.Title, FontSize = 10, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis };
            chip.MouseUp += (s, e) => { e.Handled = true; OpenEventDialog(ev); };
            return chip;
        }

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
            else _current = _current.AddMonths(-1);
            Render();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (RbScheduleView.IsChecked == true) _current = _current.AddDays(1);
            else _current = _current.AddMonths(1);
            Render();
        }

        private void BtnToday_Click(object sender, RoutedEventArgs e) { _current = DateTime.Today; Render(); }

        private void BtnMonthSelector_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = new ContextMenu();
            for (int i = 1; i <= 12; i++) {
                MenuItem mi = new MenuItem { Header = "Tháng " + i, Tag = i };
                mi.Click += (s, ex) => { _current = new DateTime(_current.Year, (int)((MenuItem)s).Tag, 1); Render(); };
                menu.Items.Add(mi);
            }
            BtnMonthSelector.ContextMenu = menu; BtnMonthSelector.ContextMenu.IsOpen = true;
        }

        private void SwitchView_Click(object sender, RoutedEventArgs e) => Render();

        private void OpenNewEvent(DateTime date)
        {
            var p = new PersonalEvent { 
                IdAcc = SessionManager.CurrentAccount.IdAcc, 
                StartTime = date.Date.AddHours(DateTime.Now.Hour), 
                EndTime = date.Date.AddHours(DateTime.Now.Hour + 1), 
                EventType = "PERSONAL" 
            };
            var dlg = new EventDialog(p) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) Render();
        }

        private void BtnEventDetail_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CalendarItem ci) OpenEventDialog(ci); }

        #endregion
    }
}