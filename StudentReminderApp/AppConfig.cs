using System;
namespace StudentReminderApp
{
    public static class AppConfig
    {
        public static string ConnectionString =>
            "Server=localhost\\SQLEXPRESS01;Database=PBL3;Trusted_Connection=True;" +
            "TrustServerCertificate=True;MultipleActiveResultSets=True;";

        // CẤU HÌNH GỬI EMAIL OTP
        // Ưu tiên đọc từ biến môi trường (Environment Variables) để bảo mật.
        // Nếu bạn chạy ở máy cá nhân (Local), hãy đổi "YOUR_EMAIL_HERE" và "YOUR_APP_PASSWORD_HERE" thành thông tin của bạn.
        // LƯU Ý: KHÔNG PUSH MẬT KHẨU THẬT LÊN GITHUB!
        public static string SenderEmail => Environment.GetEnvironmentVariable("STUDENT_APP_EMAIL") ?? "YOUR_EMAIL_HERE"; 
        
        public static string SenderAppPassword => Environment.GetEnvironmentVariable("STUDENT_APP_PWD") ?? "YOUR_APP_PASSWORD_HERE"; 

        // CẤU HÌNH ĐƯỜNG DẪN THƯ MỤC RENDER
        // Thay đổi đường dẫn này khi bạn chuyển code sang máy khác
        public static string RenderFolderPath => @"D:\IT\HỌC\PBL3\PBL3-LTUD-APP\StudentReminderApp\RENDER";
        public static string OrganizationJsonPath => System.IO.Path.Combine(RenderFolderPath, "Profile", "Organization.json");
        public static string TimeWeekStartJsonPath => System.IO.Path.Combine(RenderFolderPath, "TimeWeekStart.json");
        public static string TkbHK2JsonPath => System.IO.Path.Combine(RenderFolderPath, "HK2_2025.json");
        public static string TimePeriodJsonPath => System.IO.Path.Combine(RenderFolderPath, "TimePeriod.json");
    }
}
