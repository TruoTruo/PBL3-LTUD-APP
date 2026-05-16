using System;
using System.Text.RegularExpressions;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;
using StudentReminderApp.Services;

namespace StudentReminderApp.BLL
{
    public partial class AccountBLL
    {
        private readonly AccountDAL _dal = new AccountDAL();

        // ════════════════════════════════════════════════════════════
        // ĐĂNG NHẬP (ĐÃ FIX ÉP KIỂU CHỐNG LỖI NULLABILITY CS8619)
        // ════════════════════════════════════════════════════════════
        // Thêm dấu ? vào Account? và User? ở kiểu trả về để chấp nhận giá trị null khi login lỗi
        public Tuple<bool, string, Account?, User?> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Tuple.Create<bool, string, Account?, User?>(false, "Vui lòng nhập đầy đủ tài khoản và mật khẩu.", null, null);

            Account acc = _dal.GetAccountByUsername(username.Trim());
            if (acc == null)
                return Tuple.Create<bool, string, Account?, User?>(false, "Tài khoản hoặc mật khẩu không chính xác.", null, null);

            if (acc.Status == "Banned")
                return Tuple.Create<bool, string, Account?, User?>(false, "Tài khoản đã bị khóa. Vui lòng liên hệ Admin.", null, null);

            bool pwdOk = BCrypt.Net.BCrypt.Verify(password, acc.PasswordHash);
            if (!pwdOk)
                return Tuple.Create<bool, string, Account?, User?>(false, "Tài khoản hoặc mật khẩu không chính xác.", null, null);

            // Load user kèm TenLop ngay lúc login — fix lỗi Phần 2
            User usr = _dal.GetUserWithClass(acc.IdAcc);
            if (usr == null)
                return Tuple.Create<bool, string, Account?, User?>(false, "Không tìm thấy thông tin người dùng.", null, null);

            return Tuple.Create<bool, string, Account?, User?>(true, "Đăng nhập thành công!", acc, usr);
        }

        // ════════════════════════════════════════════════════════════
        // ĐĂNG KÝ
        // ════════════════════════════════════════════════════════════
        public Tuple<bool, string> Register(
            string mssv, string password, string confirmPassword,
            string hoTen, string email, string sdt)
        {
            // Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(mssv)     ||
                string.IsNullOrWhiteSpace(password)  ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                string.IsNullOrWhiteSpace(hoTen)     ||
                string.IsNullOrWhiteSpace(email))
                return Tuple.Create(false, "Vui lòng nhập đầy đủ các thông tin bắt buộc.");

            // Kiểm tra định dạng MSSV: đúng 9 chữ số, bắt đầu bằng 102
            if (!Regex.IsMatch(mssv, @"^102\d{6}$"))
                return Tuple.Create(false, "MSSV phải có đúng 9 chữ số và bắt đầu bằng '102'.");

            // Kiểm tra mật khẩu
            if (password != confirmPassword)
                return Tuple.Create(false, "Mật khẩu xác nhận không khớp.");
            if (password.Length < 6)
                return Tuple.Create(false, "Mật khẩu phải có tối thiểu 6 ký tự.");

            // Kiểm tra trùng MSSV
            if (_dal.GetAccountByUsername(mssv.Trim()) != null)
                return Tuple.Create(false, "MSSV này đã được đăng ký tài khoản.");

            // Hash mật khẩu và lưu
            string hash  = BCrypt.Net.BCrypt.HashPassword(password);
            long   roleId = 2; // Student

            bool ok = _dal.InsertNewStudentAccount(
                mssv.Trim(), hash, roleId,
                hoTen.Trim(), email.Trim(), sdt.Trim());

            return ok
                ? Tuple.Create(true,  "Đăng ký tài khoản sinh viên thành công!")
                : Tuple.Create(false, "Lỗi hệ thống khi tạo tài khoản. Vui lòng thử lại.");
        }

        // ════════════════════════════════════════════════════════════
        // LẤY USER KÈM LỚP (gọi sau Login nếu cần refresh)
        // ════════════════════════════════════════════════════════════
        public User GetUserWithClass(long idAcc)
            => _dal.GetUserWithClass(idAcc);

        // ════════════════════════════════════════════════════════════
        // QUÊN MẬT KHẨU — Bước 1: Gửi OTP qua Email
        // ════════════════════════════════════════════════════════════
        public Tuple<bool, string, long, string> SendOtp(string emailInput)
        {
            if (string.IsNullOrWhiteSpace(emailInput))
                return Tuple.Create(false, "Vui lòng nhập địa chỉ Email.", 0L, string.Empty);

            // Tìm tài khoản theo email
            Tuple<long, string> account = _dal.GetAccountByEmail(emailInput.Trim());
            if (account == null)
                return Tuple.Create(false, "Email này chưa được đăng ký trong hệ thống.",
                                    0L, string.Empty);

            long   idAcc = account.Item1;
            string email = account.Item2;

            // Tạo OTP
            string otp = OtpService.GenerateOtp();

            // Lưu OTP vào DB (hết hạn 5 phút)
            if (!_dal.SaveOtp(idAcc, otp))
                return Tuple.Create(false, "Lỗi hệ thống khi lưu OTP. Thử lại sau.",
                                    0L, string.Empty);

            // Hiển thị cửa sổ thông báo mô phỏng giao diện Hòm thư Gmail đến của Sinh viên
            System.Windows.MessageBox.Show(
                $"Chào bạn,\n" +
                $"Hệ thống nhận được yêu cầu cấp lại mật khẩu của bạn.\n\n" +
                $"Mã xác thực OTP của bạn là:  {otp}\n\n" +
                $"(Vui lòng ghi nhớ 6 số này để nhập vào bước tiếp theo trên phần mềm).", 
                "Ứng dụng vừa gửi 1 Email đến Gmail của bạn!", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information
            );

            // Đmax FIX LỖI CS0161: Bổ sung lệnh return kết quả thành công sau khi hiện bảng thông báo
            string masked = MaskEmail(email);
            return Tuple.Create(true, "Mã OTP đã được gửi mô phỏng thành công.", idAcc, masked);
        }

        // ════════════════════════════════════════════════════════════
        // QUÊN MẬT KHẨU — Bước 2: Xác nhận OTP
        // ════════════════════════════════════════════════════════════
        public Tuple<bool, string> ConfirmOtp(long idAcc, string otpInput)
        {
            if (string.IsNullOrWhiteSpace(otpInput) || otpInput.Length != 6)
                return Tuple.Create(false, "Mã OTP phải gồm đúng 6 chữ số.");

            return _dal.VerifyOtp(idAcc, otpInput)
                ? Tuple.Create(true,  "Xác thực OTP thành công.")
                : Tuple.Create(false, "Mã OTP không đúng hoặc đã hết hạn.");
        }

        // ════════════════════════════════════════════════════════════
        // QUÊN MẬT KHẨU — Bước 3: Đặt lại mật khẩu
        // ════════════════════════════════════════════════════════════
        public Tuple<bool, string> ResetPassword(long idAcc, string newPwd, string confirmPwd)
        {
            if (string.IsNullOrWhiteSpace(newPwd))
                return Tuple.Create(false, "Mật khẩu mới không được để trống.");
            if (newPwd.Length < 8)
                return Tuple.Create(false, "Mật khẩu mới tối thiểu 8 ký tự.");
            if (newPwd != confirmPwd)
                return Tuple.Create(false, "Xác nhận mật khẩu không khớp.");

            string hash = BCrypt.Net.BCrypt.HashPassword(newPwd);
            return _dal.ResetPassword(idAcc, hash)
                ? Tuple.Create(true,  "✓ Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.")
                : Tuple.Create(false, "Lỗi hệ thống. Thử lại sau.");
        }

        // ── Helper: che email (abc***@gmail.com) ─────────────────
        private static string MaskEmail(string email)
        {
            int at = email.IndexOf('@');
            if (at <= 1) return email;
            int visible = Math.Min(3, at);
            return email.Substring(0, visible)
                   + new string('*', at - visible)
                   + email.Substring(at);
        }
    }
}