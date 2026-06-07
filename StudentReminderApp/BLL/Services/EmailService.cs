using System;
using System.Net;
using System.Net.Mail;

namespace StudentReminderApp.Services
{
    public class EmailService
    {
        // Vui lòng thay thế bằng App Password thực tế của Gmail
        private const string SenderEmail = "pbl3.student.reminder@gmail.com";
        private const string SenderAppPassword = "zihf qowp zxcv asdf"; // Thay thế App Password vào đây

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
                    Credentials = new NetworkCredential(SenderEmail, SenderAppPassword)
                };
                using var message = new MailMessage(SenderEmail, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                smtp.Send(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi gửi email: " + ex.Message);
            }
        }
    }
}
