using System;

namespace StudentReminderApp.Models
{
    public class User
    {
        public long      IdAcc    { get; set; }
        public string?   HoTen    { get; set; }   // nullable → hết CS8618
        public string?   Email    { get; set; }
        public string?   Sdt      { get; set; }
        public DateTime? NgaySinh { get; set; }
        public long?     IdLop    { get; set; }
        public string?   TenLop   { get; set; }
    }
}