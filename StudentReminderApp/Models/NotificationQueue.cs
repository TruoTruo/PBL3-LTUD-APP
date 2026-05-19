using System;

namespace StudentReminderApp.Models
{
    public class NotificationQueue
    {
        public long IdQueue { get; set; }
        public long IdAcc { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string Status { get; set; }
        public long? IdBuoiHoc { get; set; }
        public long? IdEvent { get; set; }
    }
}