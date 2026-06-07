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

        // CẤU HÌNH ĐƯỜNG DẪN THƯ MỤC RENDER
        // Thay đổi đường dẫn này khi bạn chuyển code sang máy khác
        public static string RenderFolderPath => @"D:\IT\HỌC\PBL3\PBL3-LTUD-APP\RENDER";
        public static string OrganizationJsonPath => System.IO.Path.Combine(RenderFolderPath, "Profile", "Organization.json");
        public static string TimeWeekStartJsonPath => System.IO.Path.Combine(RenderFolderPath, "TimeWeekStart.json");
        public static string TkbHK2JsonPath => System.IO.Path.Combine(RenderFolderPath, "HK2_2025.json");
        public static string TimePeriodJsonPath => System.IO.Path.Combine(RenderFolderPath, "TimePeriod.json");
    }
}
