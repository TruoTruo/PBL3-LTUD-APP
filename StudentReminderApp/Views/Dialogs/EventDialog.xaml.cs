using System;
using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Dialogs
{
    public partial class EventDialog : Window
    {
        private readonly PersonalEvent _event;
        private readonly EventBLL      _bll = new EventBLL();

        public EventDialog(PersonalEvent ev = null)
        {
            InitializeComponent();
            _event = ev ?? new PersonalEvent
            {
                IdAcc     = SessionManager.CurrentAccount.IdAcc,
                StartTime = DateTime.Now,
                EndTime   = DateTime.Now.AddHours(1),
                EventType = "PERSONAL"
            };

            TxtDialogTitle.Text  = _event.IdEvent == 0 ? "Thêm sự kiện" : "Chỉnh sửa sự kiện";
            TxtEventTitle.Text   = _event.Title;
            TxtLocation.Text     = _event.Location;
            TxtDesc.Text         = _event.Description;
            DpStart.SelectedDate = _event.StartTime.Date;
            DpEnd.SelectedDate   = _event.EndTime.Date;

            // Set combo selection
            foreach (ComboBoxItem item in CmbType.Items)
                if (item.Tag?.ToString() == _event.EventType)
                    CmbType.SelectedItem = item;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TxtErr.Visibility  = Visibility.Collapsed;
            _event.Title       = TxtEventTitle.Text;
            _event.Location    = TxtLocation.Text;
            _event.Description = TxtDesc.Text;
            _event.StartTime   = DpStart.SelectedDate?.Date.AddHours(8) ?? DateTime.Now;
            _event.EndTime     = DpEnd.SelectedDate?.Date.AddHours(17)  ?? DateTime.Now.AddHours(1);
            _event.EventType   = ((ComboBoxItem)CmbType.SelectedItem)?.Tag?.ToString() ?? "PERSONAL";

            var (ok, msg) = _bll.Save(_event);
            if (!ok) { TxtErr.Text = "⚠ " + msg; TxtErr.Visibility = Visibility.Visible; return; }
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
