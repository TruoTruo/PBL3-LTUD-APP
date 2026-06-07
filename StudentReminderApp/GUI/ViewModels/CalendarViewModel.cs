using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using StudentReminderApp.BLL;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.ViewModels
{
    public class CalendarViewModel : BaseViewModel
    {
        private readonly EventBLL _eventBll;

        // Danh sách sự kiện để binding lên giao diện
        private ObservableCollection<CalendarItem> _events;
        public ObservableCollection<CalendarItem> Events
        {
            get => _events;
            set
            {
                _events = value;
                OnPropertyChanged(nameof(Events));
            }
        }

        // Ngày hiện tại đang xem trên lịch
        private DateTime _currentDate;
        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged(nameof(CurrentDate));
                LoadEvents(); // Tự động gọi lại dữ liệu khi đổi tháng
            }
        }

        // Các Command để chuyển tháng
        public ICommand NextMonthCommand { get; }
        public ICommand PrevMonthCommand { get; }

        public CalendarViewModel()
        {
            _eventBll = new EventBLL();
            Events = new ObservableCollection<CalendarItem>();
            
            // Khởi tạo các nút bấm chuyển tháng
            NextMonthCommand = new RelayCommand(_ => CurrentDate = CurrentDate.AddMonths(1));
            PrevMonthCommand = new RelayCommand(_ => CurrentDate = CurrentDate.AddMonths(-1));

            // Gán giá trị ban đầu (việc gán này sẽ tự động trigger hàm LoadEvents thông qua setter)
            CurrentDate = DateTime.Now;
        }
        

        public void LoadEvents()
        {
            // Kiểm tra an toàn xem có user đang đăng nhập không
            if (SessionManager.CurrentAccount == null) return;

            // Lấy ID tài khoản đang đăng nhập
            long currentUserId = SessionManager.CurrentAccount.IdAcc; 

            // Gọi xuống BLL để lấy dữ liệu
            var items = _eventBll.GetCalendarItemsForMonth(currentUserId, CurrentDate.Year, CurrentDate.Month);
            
            Events.Clear();
            foreach (var item in items)
            {
                Events.Add(item);
            }
        }
    }
}