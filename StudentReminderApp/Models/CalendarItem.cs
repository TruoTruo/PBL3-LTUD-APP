using System;
using System.Windows.Media;

namespace StudentReminderApp.Models
{
    public class CalendarItem
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; } 
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string EventType { get; set; } // PERSONAL, ACADEMIC, DEADLINE
        public object OriginalEvent { get; set; }

        public Brush BackgroundColor
        {
            get
            {
                var converter = new BrushConverter();
                return EventType switch
                {
                    "DEADLINE" => (Brush)converter.ConvertFromString("#EF4444"), // Đỏ
                    "ACADEMIC" => (Brush)converter.ConvertFromString("#2563EB"), // Xanh dương
                    _          => (Brush)converter.ConvertFromString("#10B981")  // Xanh lá (Cá nhân)
                };
            }
        }
    }
}