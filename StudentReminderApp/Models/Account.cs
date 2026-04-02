// Models/Account.cs
using System;

namespace StudentReminderApp.Models
{
    public class Account
    {
        public long IdAcc { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; internal set; }
    }

    public class User
    {
        public long IdAcc { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string Sdt { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string DiaChi { get; set; }
    }
}