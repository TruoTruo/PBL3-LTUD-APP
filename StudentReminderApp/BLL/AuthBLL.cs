using StudentReminderApp.DAL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.BLL
{
    public class AuthBLL
    {
        private readonly AccountDAL _accDal = new AccountDAL();
        private readonly UserDAL _usrDal = new UserDAL();

        public (bool ok, string msg, Account acc, User user) Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "Vui lòng nhập đầy đủ thông tin.", null, null);

            var acc = _accDal.GetByUsername(username.Trim());
            if (acc == null)
                return (false, "Tài khoản không tồn tại.", null, null);

            // Kiểm tra trạng thái tài khoản (có thể là Status hoặc IsActive tùy theo DB)
            if (acc.Status == "Banned" || !acc.IsActive)
                return (false, "Tài khoản bị khóa.", null, null);

            if (!BCrypt.Net.BCrypt.Verify(password, acc.PasswordHash))
                return (false, "Mật khẩu không chính xác.", null, null);

            var user = _usrDal.GetById(acc.IdAcc);
            return (true, "OK", acc, user);
        }

        public (bool ok, string msg) Register(string username, string password,
                                               string confirm, string hoTen,
                                               string email, string sdt)
        {
            // Validate các trường bắt buộc
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(hoTen))
                return (false, "Vui lòng điền đầy đủ các trường bắt buộc (*).");

            // Validate username
            if (username.Trim().Length < 6)
                return (false, "Tên đăng nhập tối thiểu 6 ký tự.");

            // Validate password
            if (password.Length < 8)
                return (false, "Mật khẩu tối thiểu 8 ký tự.");

            if (password != confirm)
                return (false, "Xác nhận mật khẩu không khớp.");

            // Kiểm tra username đã tồn tại
            if (_accDal.UsernameExists(username.Trim()))
                return (false, "Tên đăng nhập đã tồn tại.");

            // Tạo tài khoản mới
            _accDal.CreateWithProfile(
                username.Trim(),
                BCrypt.Net.BCrypt.HashPassword(password),
                hoTen.Trim(),
                email ?? "",
                sdt ?? "");

            return (true, "Đăng ký thành công!");
        }

        // Đăng xuất
        public void Logout()
        {
            SessionManager.ClearSession();
        }

        // Kiểm tra trạng thái đăng nhập
        public bool IsLoggedIn()
        {
            return SessionManager.CurrentAccount != null;
        }

        // Lấy thông tin người dùng hiện tại
        public User GetCurrentUser()
        {
            return SessionManager.CurrentUser;
        }
    }
}