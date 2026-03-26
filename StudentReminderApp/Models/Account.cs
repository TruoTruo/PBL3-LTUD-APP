namespace StudentReminderApp.Models
{
    public class Account
    {
        public long   IdAcc        { get; set; }
        public string Username     { get; set; }
        public string PasswordHash { get; set; }
        public long   IdRole       { get; set; }
        public string Status       { get; set; }
        public string RoleName     { get; set; }
    }
}
