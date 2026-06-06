using System;

namespace StudentReminderApp.Models
{
    public class PersonalEvent
    {
        public long IdEvent { get; set; }
        public long IdAcc { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? RecurrenceRule { get; set; }
        public string ColorCategory { get; set; } = string.Empty;
        public bool IsAllDay { get; set; }

        // Thuộc tính quan trọng nhất để sửa lỗi build:
        public bool IsCompleted { get; set; }
        public string? GroupId { get; set; }
    }
}
