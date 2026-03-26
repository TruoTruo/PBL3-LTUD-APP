using System;

namespace StudentReminderApp.Models
{
    public class User
    {
        public long      IdAcc    { get; set; }
        public string    HoTen    { get; set; }
        public string    Email    { get; set; }
        public string    Sdt      { get; set; }
        public DateTime? NgaySinh { get; set; }
    }
}
