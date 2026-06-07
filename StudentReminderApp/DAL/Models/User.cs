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
        public string?   NganhHoc { get; set; }
        public string?   TruongHoc { get; set; }
        public string?   Nhom { get; set; }
        public string?   QueQuan { get; set; }
        public string?   AvatarUrl { get; set; }
        public string?   Khoa { get; set; }
    }
}