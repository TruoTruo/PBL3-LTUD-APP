using System;
using System.Net;
using System.Net.Mail;

namespace StudentReminderApp.Services
{
    /// <summary>
    /// Tạo mã OTP và gửi email qua Gmail SMTP.
    ///
    /// CẤU HÌNH:
    ///   1. Bật "Xác minh 2 bước" cho Gmail gửi.
    ///   2. Vào myaccount.google.com → Security → App passwords → tạo mật khẩu 16 ký tự.
    ///   3. Điền SenderEmail và SenderAppPassword bên dưới.
    /// </summary>
    public static class OtpService
    {
        private const string SenderEmail       = "your_app@gmail.com";   // ← đổi thành email của bạn
        private const string SenderAppPassword = "xxxx xxxx xxxx xxxx";  // ← App Password 16 ký tự
        private const string SenderName        = "Student Reminder App";
        private const string SmtpHost          = "smtp.gmail.com";
        private const int    SmtpPort          = 587;

        // ── Tạo mã 6 chữ số ngẫu nhiên ──────────────────────────
        public static string GenerateOtp()
        {
            Random rng = new Random();
            return rng.Next(100_000, 999_999).ToString();
        }

        /// <summary>
        /// Gửi email OTP. Trả về Tuple(bool sent, string error).
        /// Dùng Tuple thay ValueTuple để tránh lỗi CS8130 ở .NET Framework.
        /// </summary>
        public static Tuple<bool, string> SendOtp(string toEmail, string otpCode)
        {
            try
            {
                MailMessage msg = new MailMessage();
                msg.From       = new MailAddress(SenderEmail, SenderName);
                msg.Subject    = "[Student Reminder] Mã OTP đặt lại mật khẩu";
                msg.IsBodyHtml = true;
                msg.Body       = BuildEmailBody(otpCode);
                msg.To.Add(toEmail);

                using (SmtpClient client = new SmtpClient(SmtpHost, SmtpPort))
                {
                    client.EnableSsl   = true;
                    client.Credentials = new NetworkCredential(SenderEmail, SenderAppPassword);
                    client.Send(msg);
                }

                return Tuple.Create(true, string.Empty);
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, ex.Message);
            }
        }

        private static string BuildEmailBody(string otpCode)
        {
            return string.Format(@"
<div style='font-family:Segoe UI,Arial,sans-serif;max-width:480px;margin:auto;
            border:1px solid #E5E7EB;border-radius:12px;overflow:hidden'>
  <div style='background:#2563EB;padding:24px 32px'>
    <h2 style='color:white;margin:0'>Student Reminder &amp; Advisor</h2>
  </div>
  <div style='padding:32px'>
    <p style='font-size:15px;color:#374151'>Bạn vừa yêu cầu đặt lại mật khẩu.</p>
    <p style='font-size:15px;color:#374151'>Mã OTP của bạn là:</p>
    <div style='background:#F3F4F6;border-radius:8px;padding:20px;text-align:center;
                font-size:36px;font-weight:bold;letter-spacing:10px;
                color:#1D4ED8;margin:16px 0'>{0}</div>
    <p style='font-size:13px;color:#6B7280'>
      Mã có hiệu lực trong <strong>5 phút</strong>.<br/>
      Không chia sẻ mã này với bất kỳ ai.
    </p>
  </div>
</div>", otpCode);
        }
    }
}