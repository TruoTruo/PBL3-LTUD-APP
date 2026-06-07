using System;

namespace StudentReminderApp.Models
{
    public class UserManagementDto
    {
        public long IdAcc { get; set; }
        public string Username { get; set; }
        public string RoleName { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string Sdt { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string NganhHoc { get; set; }
        public string TruongHoc { get; set; }
        public string Khoa { get; set; }
        public string TenLop { get; set; }
        public string Nhom { get; set; }
        public string QueQuan { get; set; }
        public string AvatarUrl { get; set; }
    }
}
