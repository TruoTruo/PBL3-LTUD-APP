namespace StudentReminderApp
{
    public static class AppConfig
    {
        public static string ConnectionString =>
            "Server=localhost;Database=PBL3;Trusted_Connection=True;" +
            "TrustServerCertificate=True;MultipleActiveResultSets=True;";
    }
}
