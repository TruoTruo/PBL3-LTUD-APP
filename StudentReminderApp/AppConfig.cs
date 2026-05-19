namespace StudentReminderApp
{
    public static class AppConfig
    {
        public static string ConnectionString =>
            "Server=localhost\\SQLEXPRESS01;Database=PBL3;Trusted_Connection=True;" +
            "TrustServerCertificate=True;MultipleActiveResultSets=True;";
    }
}
