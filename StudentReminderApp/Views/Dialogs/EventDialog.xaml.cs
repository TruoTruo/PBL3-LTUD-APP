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
            DpStart.SelectedDate = _event.StartTime.Date;
            DpEnd.SelectedDate   = _event.EndTime.Date;
            
            TxtStartTime.Text    = _event.StartTime.ToString("HH:mm");
            TxtEndTime.Text      = _event.EndTime.ToString("HH:mm");

            // Chọn loại sự kiện trong ComboBox
            foreach (ComboBoxItem item in CmbType.Items)
            {
                if (item.Tag?.ToString() == _event.EventType)
                {
                    CmbType.SelectedItem = item;
                    break;
                }
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
            if (!TimeSpan.TryParse(TxtStartTime.Text, out TimeSpan startTime) || 
                !TimeSpan.TryParse(TxtEndTime.Text, out TimeSpan endTime))
            {
                ShowError("Giờ sai định dạng HH:mm (VD: 08:30)");
                return;
            }

            // 3. Gộp ngày và giờ
            DateTime start = (DpStart.SelectedDate ?? DateTime.Today).Date.Add(startTime);
            DateTime end   = (DpEnd.SelectedDate ?? start.Date).Date.Add(endTime);

            if (end <= start)
            {
                ShowError("Giờ kết thúc phải sau giờ bắt đầu");
                return;
            }

            // 4. Cập nhật object
            _event.Title       = TxtEventTitle.Text;
            _event.Location    = TxtLocation.Text;
            _event.Description = TxtDesc.Text;
            _event.StartTime   = start;
            _event.EndTime     = end;
            _event.EventType   = ((ComboBoxItem)CmbType.SelectedItem)?.Tag?.ToString() ?? "PERSONAL";

            // 5. Lưu (BLL sẽ tự check: nếu Id > 0 thì UPDATE, Id = 0 thì INSERT)
            var (ok, msg) = _bll.Save(_event);
            if (!ok) 
            { 
                ShowError(msg); 
                return; 
            }

            this.DialogResult = true; 
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
    }
}