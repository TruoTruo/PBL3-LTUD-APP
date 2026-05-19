namespace StudentReminderApp.Models
{
    public class Account
    {
        public long IdAcc { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public long IdRole { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        // Thêm thuộc tính này vào file Models/Account.cs
        public bool IsVerified { get; set; } = false;

        public bool IsAdmin => RoleName == "Admin";
    }
}
