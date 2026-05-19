using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;
using StudentReminderApp.Views.Dialogs;

namespace StudentReminderApp.Views.Pages
{
    public partial class CalendarPage : Page
    {
        private DateTime   _current = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private readonly EventBLL _bll = new EventBLL();

        public CalendarPage() { InitializeComponent(); Loaded += (s, e) => Render(); }

        private void Render()
        {
            TxtMonthYear.Text = _current.ToString("MMMM yyyy");
            CalendarGrid.Children.Clear();

            var events    = _bll.GetByMonth(SessionManager.CurrentAccount.IdAcc,
                                            _current.Year, _current.Month);
            int startDow  = ((int)_current.DayOfWeek + 6) % 7;
            var gridStart = _current.AddDays(-startDow);

            for (int i = 0; i < 42; i++)
            {
                var date    = gridStart.AddDays(i);
                var dayEvts = events.Where(e => e.StartTime.Date == date.Date).ToList();
                CalendarGrid.Children.Add(MakeDayCell(date, dayEvts));
            }
        }

        private Border MakeDayCell(DateTime date, List<PersonalEvent> events)
        {
            bool isToday      = date.Date == DateTime.Today;
            bool isOtherMonth = date.Month != _current.Month;

            var cell = new Border
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background      = isToday
                    ? new SolidColorBrush(Color.FromRgb(239, 246, 255))
                    : Brushes.White,
                Padding = new Thickness(6),
                Cursor  = Cursors.Hand
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text       = date.Day.ToString(),
                FontSize   = 13,
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isToday
                    ? new SolidColorBrush(Color.FromRgb(37, 99, 235))
                    : isOtherMonth
                        ? new SolidColorBrush(Color.FromRgb(148, 163, 184))
                        : new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 0, 0, 4)
            });

            foreach (var ev in events.Take(3))
            {
                var chip = new Border
                {
                    Background   = GetEventBrush(ev.EventType),
                    CornerRadius = new CornerRadius(4),
                    Padding      = new Thickness(4, 2, 4, 2),
                    Margin       = new Thickness(0, 0, 0, 2),
                    Tag          = ev,
                    Cursor       = Cursors.Hand
                };
                chip.Child    = new TextBlock { Text = ev.Title, FontSize = 10,
                    Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis };
                chip.MouseUp += (s, e) => OpenEvent((PersonalEvent)((Border)s).Tag);
                panel.Children.Add(chip);
            }

            if (events.Count > 3)
                panel.Children.Add(new TextBlock
                {
                    Text = $"+{events.Count - 3} khác", FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
                });

            cell.Child   = panel;
            cell.MouseUp += (s, e) => { if (!(e.OriginalSource is Border b && b.Tag is PersonalEvent)) OpenNewEvent(date); };
            return cell;
        }

        private void OpenEvent(PersonalEvent ev)
        {
            var dlg = new EventDialog(ev) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) Render();
        }

        private void OpenNewEvent(DateTime date)
        {
            var ev = new PersonalEvent
            {
                IdAcc     = SessionManager.CurrentAccount.IdAcc,
                StartTime = date.Date.AddHours(8),
                EndTime   = date.Date.AddHours(9),
                EventType = "PERSONAL"
            };
            var dlg = new EventDialog(ev) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) Render();
        }

        private static SolidColorBrush GetEventBrush(string type) => type switch
        {
            "DEADLINE" => new SolidColorBrush(Color.FromRgb(239, 68,  68)),
            "ACADEMIC" => new SolidColorBrush(Color.FromRgb(37,  99, 235)),
            _          => new SolidColorBrush(Color.FromRgb(16, 185, 129))
        };

        private void BtnPrevMonth_Click(object sender, RoutedEventArgs e)
        { _current = _current.AddMonths(-1); Render(); }
        private void BtnNextMonth_Click(object sender, RoutedEventArgs e)
        { _current = _current.AddMonths(1); Render(); }
        private void BtnAdd_Click(object sender, RoutedEventArgs e) => OpenNewEvent(DateTime.Today);
    }
}
