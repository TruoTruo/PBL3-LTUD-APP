using System;
using StudentReminderApp.ViewModels;
using StudentReminderApp.Helpers; // Thêm cái này để lấy Session người dùng

namespace StudentReminderApp.Models
{
    public class Comment : BaseViewModel
    {
        private long _idComment;
        public long IdComment 
        { 
            get => _idComment; 
            set { _idComment = value; OnPropertyChanged(); } 
        }

        public long IdAcc { get; set; }
        public long IdPost { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        private string _authorName;
        public string AuthorName
        {
            get => _authorName;
            set { _authorName = value; OnPropertyChanged(); }
        }

        // --- THÊM LOGIC KIỂM TRA QUYỀN XÓA ---
        // Thuộc tính này giúp UI (XAML) biết có nên hiện nút xóa hay không
        public bool IsMyComment
        {
            get
            {
                if (SessionManager.CurrentUser == null) return false;
                return IdAcc == SessionManager.CurrentUser.IdAcc;
            }
        }

        public string TimeDisplay
        {
            get
            {
                TimeSpan span = DateTime.Now - CreatedAt;
                if (span.TotalDays > 1) return CreatedAt.ToString("dd/MM/yyyy HH:mm");
                if (span.TotalHours > 1) return $"{(int)span.TotalHours} giờ trước";
                if (span.TotalMinutes > 1) return $"{(int)span.TotalMinutes} phút trước";
                return "Vừa xong";
            }
        }
    }
}