using System;
using StudentReminderApp.ViewModels;

namespace StudentReminderApp.Models
{
    public class Comment : BaseViewModel
    {
        public long IdComment { get; set; }
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