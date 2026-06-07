using System;
using System.Net;
using System.Net.Mail;

namespace StudentReminderApp.Services
{
    public class EmailService
    {
        public static void SendEmail(string toAddress, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toAddress)) return;

            try
            {
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(AppConfig.SenderEmail, AppConfig.SenderAppPassword)
                };
                using var message = new MailMessage(AppConfig.SenderEmail, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                smtp.Send(message);
            }
            catch (Exception ex)
            {
                try
                {
                    string logPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "StudentReminderApp_EmailLog.txt");
                    string errorDetails = $"[{DateTime.Now}] Gửi tới: {toAddress}\nLỗi: {ex.Message}\nStack Trace:\n{ex.StackTrace}\n\n";
                    System.IO.File.AppendAllText(logPath, errorDetails);
                }
                catch { /* Bỏ qua nếu không ghi được log */ }
            }
        }
    }
}
