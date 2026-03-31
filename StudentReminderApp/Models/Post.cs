using System;
using StudentReminderApp.Helpers; // Đảm bảo có namespace này để dùng SessionManager

namespace StudentReminderApp.Models
{
    public class Post
    {
        public long IdPost { get; set; }
        public long IdAcc { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Likes { get; set; }
        public bool IsAnonymous { get; set; } 
        public string AuthorName { get; set; }
        public string AuthorAvatar { get; set; }

        // --- THÊM DÒNG NÀY ---
        // Kiểm tra xem ID người đăng bài có trùng với ID người đang đăng nhập không
        public bool IsMyPost => SessionManager.CurrentUser != null && IdAcc == SessionManager.CurrentUser.IdAcc;
        // ---------------------

        public string DisplayName 
        {
            get 
            {
                if (IsAnonymous) return "Người dùng ẩn danh";
                return string.IsNullOrWhiteSpace(AuthorName) ? "Thành viên" : AuthorName;
            }
        }

        public string DisplayAvatar 
        {
            get 
            {
                if (IsAnonymous) return "/Resources/Images/user.png";
                return string.IsNullOrWhiteSpace(AuthorAvatar) ? "/Resources/Images/user.png" : AuthorAvatar;
            }
        }

        public string TimeAgo
        {
            get
            {
                TimeSpan span = DateTime.Now - CreatedAt;
                if (span.TotalMinutes < 1) return "Vừa xong";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
                return CreatedAt.ToString("dd/MM/yyyy");
            }
        }
    }
}