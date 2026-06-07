using System;

namespace StudentReminderApp.Models
{
    public class UserLogModel
    {
        public long IdLog { get; set; }
        public string Action { get; set; }
        public DateTime Time { get; set; }
        public long IdAcc { get; set; }
        public string UserName { get; set; }
    }
}
