namespace StudentReminderApp
{
    public static class AppConfig
    {
        public static string ConnectionString =>
            "Server=localhost\\SQLEXPRESS01;Database=PBL3;Trusted_Connection=True;" +
            "TrustServerCertificate=True;MultipleActiveResultSets=True;";

        // CẤU HÌNH GỬI EMAIL OTP
        // 1. Điền Email của bạn vào đây.
        public static string SenderEmail => "nvttruonghtb@gmail.com"; 
        // 2. Điền App Password 16 ký tự của Gmail vào đây.
        public static string SenderAppPassword => "hilickcbbwpljyyd"; 
    }
}
