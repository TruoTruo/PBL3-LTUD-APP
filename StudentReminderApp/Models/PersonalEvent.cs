using System;

namespace StudentReminderApp.Models
{
    public class PersonalEvent
    {
        public long     IdEvent        { get; set; }
        public long     IdAcc          { get; set; }
        public string   Title          { get; set; }
        public string   Description    { get; set; }
        public string   Location       { get; set; }
        public DateTime StartTime      { get; set; }
        public DateTime EndTime        { get; set; }
        public string   EventType      { get; set; }
        public string   RecurrenceRule { get; set; }

        // Bổ sung cho giao diện Calendar
        public bool     IsAllDay       { get; set; } // Xác định sự kiện cả ngày
        public string   ColorCategory  { get; set; } // Lưu mã màu (VD: "#FF0000")

        public string EventTypeIcon => EventType switch
        {
            "DEADLINE" => "⏰",
            "ACADEMIC" => "📚",
            _          => "📌"
        };
    }
}