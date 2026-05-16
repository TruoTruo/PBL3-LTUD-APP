using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StudentReminderApp.BLL;
using StudentReminderApp.DAL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;
using StudentReminderApp.ViewModels;

namespace StudentReminderApp.Views.Pages
{
    public partial class ProfilePage : Page
    {
        private readonly UserDAL    _userDal = new UserDAL();
        private readonly StudentBLL _stuBll  = new StudentBLL();
        private readonly AccountBLL    _authBll = new AccountBLL();

        private List<ClassItem> _classList = new();

        public ProfilePage()
        {
            InitializeComponent();
            Loaded += (s, e) => { LoadClasses(); LoadProfile(); };
        }

        // ── Load danh sách lớp vào ComboBox ──────────────────────
        private void LoadClasses()
        {
            var rawList = _stuBll.GetAllClasses();
            _classList.Clear();
            _classList.Add(new ClassItem { IdLop = null, TenLop = "— Chưa phân lớp —" });
            foreach (var (id, ten) in rawList)
                _classList.Add(new ClassItem { IdLop = id, TenLop = ten });

            CmbClass.ItemsSource       = _classList;
            CmbClass.DisplayMemberPath = "TenLop";
            CmbClass.SelectedValuePath = "IdLop";
        }

        // ── Load profile ──────────────────────────────────────────
        private void LoadProfile()
        {
            var acc = SessionManager.CurrentAccount;
            if (acc == null) return;

            // ── FIX PHẦN 2: Reload User từ DB kèm TenLop ──────────
            // Không chỉ dùng SessionManager.CurrentUser vì có thể
            // thiếu IdLop/TenLop nếu query lúc login không có JOIN.
            var user = _authBll.GetUserWithClass(acc.IdAcc)
                       ?? SessionManager.CurrentUser;

            if (user == null) return;

            // Cập nhật lại session để các màn hình khác cũng dùng đúng
            SessionManager.SetSession(acc, user);

            // ── Điền dữ liệu UI ───────────────────────────────────
            TxtAvatar.Text          = user.HoTen?.Length > 0 ? user.HoTen[0].ToString().ToUpper() : "?";
            TxtFullName.Text        = user.HoTen;
            TxtUsernameDisplay.Text = "@" + acc.Username;
            TxtMssvDisplay.Text     = acc.Username ?? "";
            TxtRole.Text            = acc.RoleName == "Admin" ? "Quản trị viên" : "Sinh viên";

            TxtEditName.Text        = user.HoTen;
            TxtEditEmail.Text       = user.Email;
            TxtEditSdt.Text         = user.Sdt;
            TxtEditMssv.Text        = acc.Username ?? "";
            DpNgaySinh.SelectedDate = user.NgaySinh;

            // ── Chọn lớp trong ComboBox ───────────────────────────
            // user.IdLop đã được load từ DB cùng LEFT JOIN → không còn null nữa
            SelectClass(user.IdLop);
        }

        private void SelectClass(long? idLop)
        {
            if (idLop.HasValue)
            {
                foreach (var item in _classList)
                {
                    if (item.IdLop == idLop) { CmbClass.SelectedItem = item; return; }
                }
            }
            CmbClass.SelectedIndex = 0;
        }

        // ── Lưu profile + lớp ────────────────────────────────────
        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            var acc  = SessionManager.CurrentAccount;
            var user = SessionManager.CurrentUser;
            if (user == null || acc == null) return;

            if (string.IsNullOrWhiteSpace(TxtEditName.Text))
            { ShowMsg(TxtProfileMsg, "Họ tên không được để trống.", false); return; }

            user.HoTen    = TxtEditName.Text.Trim();
            user.Email    = TxtEditEmail.Text.Trim();
            user.Sdt      = TxtEditSdt.Text.Trim();
            user.NgaySinh = DpNgaySinh.SelectedDate;

            long? newIdLop = (CmbClass.SelectedItem is ClassItem sel && sel.IdLop.HasValue)
                ? sel.IdLop : null;
            user.IdLop = newIdLop;

            _userDal.Update(user);
            _stuBll.UpdateStudentClass(acc.IdAcc, newIdLop);

            // Reload từ DB để lấy TenLop mới nhất → cập nhật session
            var refreshed = _authBll.GetUserWithClass(acc.IdAcc);
            if (refreshed != null) SessionManager.SetSession(acc, refreshed);
            else                   SessionManager.SetSession(acc, user);

            TxtFullName.Text = user.HoTen;
            TxtAvatar.Text   = user.HoTen[0].ToString().ToUpper();
            ShowMsg(TxtProfileMsg, "✓ Lưu thành công!", true);
        }

        // ── Đổi mật khẩu ─────────────────────────────────────────
        private void BtnChangePwd_Click(object sender, RoutedEventArgs e)
        {
            var acc = SessionManager.CurrentAccount;
            if (acc == null) return;

            if (string.IsNullOrWhiteSpace(PwdCurrent.Password) ||
                string.IsNullOrWhiteSpace(PwdNew.Password))
            { ShowMsg(TxtPwdMsg, "Vui lòng điền đầy đủ.", false); return; }

            if (!BCrypt.Net.BCrypt.Verify(PwdCurrent.Password, acc.PasswordHash))
            { ShowMsg(TxtPwdMsg, "Mật khẩu hiện tại không đúng.", false); return; }

            if (PwdNew.Password.Length < 8)
            { ShowMsg(TxtPwdMsg, "Mật khẩu mới tối thiểu 8 ký tự.", false); return; }

            if (PwdNew.Password != PwdConfirm.Password)
            { ShowMsg(TxtPwdMsg, "Xác nhận mật khẩu không khớp.", false); return; }

            string hash = BCrypt.Net.BCrypt.HashPassword(PwdNew.Password);
            UpdatePasswordHash(acc.IdAcc, hash);
            acc.PasswordHash = hash;
            PwdCurrent.Password = PwdNew.Password = PwdConfirm.Password = "";
            ShowMsg(TxtPwdMsg, "✓ Đổi mật khẩu thành công!", true);
        }

        // ── Cài đặt nhắc nhở ─────────────────────────────────────
        private void BtnSaveReminder_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtMinsBefore.Text, out int mins) || mins < 1)
            { ShowMsg(TxtReminderMsg, "Số phút không hợp lệ.", false); return; }

            SaveReminderConfig(SessionManager.CurrentAccount.IdAcc, mins,
                ChkEnabled.IsChecked == true,
                CmbChannel.SelectedIndex == 0 ? "PUSH" : "EMAIL");
            ShowMsg(TxtReminderMsg, "✓ Đã lưu cài đặt!", true);
        }

        // ── Helpers ───────────────────────────────────────────────
        private static void ShowMsg(TextBlock tb, string msg, bool success)
        {
            tb.Text       = msg;
            tb.Foreground = success
                ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                : new SolidColorBrush(Color.FromRgb(239, 68, 68));
            tb.Visibility = Visibility.Visible;
        }

        private static void UpdatePasswordHash(long idAcc, string hash)
        {
            const string sql = "UPDATE ACCOUNT SET password_hash=@h, updated_at=GETDATE() WHERE id_acc=@id";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@h",  hash);
            cmd.Parameters.AddWithValue("@id", idAcc);
            cmd.ExecuteNonQuery();
        }

        private static void SaveReminderConfig(long idAcc, int mins, bool isEnabled, string channel)
        {
            const string sql = @"
                IF EXISTS (SELECT 1 FROM REMINDER_CONFIG WHERE id_acc=@acc)
                    UPDATE REMINDER_CONFIG SET mins_before=@m,is_enabled=@en,channel=@ch WHERE id_acc=@acc
                ELSE
                    INSERT INTO REMINDER_CONFIG(id_acc,mins_before,is_enabled,channel) VALUES(@acc,@m,@en,@ch)";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@acc", idAcc);
            cmd.Parameters.AddWithValue("@m",   mins);
            cmd.Parameters.AddWithValue("@en",  isEnabled ? 1 : 0);
            cmd.Parameters.AddWithValue("@ch",  channel);
            cmd.ExecuteNonQuery();
        }
    }
}