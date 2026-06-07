using StudentReminderApp.Models;

namespace StudentReminderApp.Helpers
{
    public static class SessionManager
    {
        public static Account CurrentAccount { get; private set; }
        public static User    CurrentUser    { get; private set; }

        public static void SetSession(Account acc, User user)
        {
            CurrentAccount = acc;
            CurrentUser    = user;
        }
        public static void Clear()
        {
            CurrentAccount = null;
            CurrentUser    = null;
        }
        public static bool IsLoggedIn => CurrentAccount != null;
        public static bool IsAdmin    => CurrentAccount?.RoleName == "Admin";
    }
}
