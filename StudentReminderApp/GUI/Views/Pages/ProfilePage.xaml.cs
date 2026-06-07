using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        private readonly DanhMucDAL _danhMucDal = new DanhMucDAL();

        private List<ClassItem> _classList = new();

        public ProfilePage()
        {
            InitializeComponent();
            Loaded += (s, e) => { LoadClasses(); LoadDanhMuc(); LoadProfile(); };
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

        private void LoadDanhMuc()
        {
            // Trường
            CmbTruong.Items.Clear();
            CmbTruong.Items.Add(new ComboBoxItem { Content = "— Chọn trường —" });
            foreach (var item in _danhMucDal.GetByCategory("TRUONG"))
                CmbTruong.Items.Add(new ComboBoxItem { Content = item.Value });
            
            // Khoa
            CmbKhoa.Items.Clear();
            CmbKhoa.Items.Add(new ComboBoxItem { Content = "— Chọn khoa —" });
            foreach (var item in _danhMucDal.GetByCategory("KHOA"))
                CmbKhoa.Items.Add(new ComboBoxItem { Content = item.Value });

            // Ngành học
            CmbNganhHoc.Items.Clear();
            CmbNganhHoc.Items.Add(new ComboBoxItem { Content = "— Chọn ngành học —" });
            foreach (var item in _danhMucDal.GetByCategory("NGANH"))
                CmbNganhHoc.Items.Add(new ComboBoxItem { Content = item.Value });

            // Nhóm
            CmbNhom.Items.Clear();
            CmbNhom.Items.Add(new ComboBoxItem { Content = "— Chọn nhóm —" });
            foreach (var item in _danhMucDal.GetByCategory("NHOM"))
                CmbNhom.Items.Add(new ComboBoxItem { Content = item.Value });
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
            // Load Profile Header
            TxtHeaderName.Text = user.HoTen ?? "Họ tên chưa cập nhật";
            TxtHeaderUsername.Text = "@" + (acc.Username ?? "");
            TxtBadgeMssv.Text = "MSSV: " + (acc.Username ?? "");
            
            TxtBadgeRole.Text = (acc.RoleName == "Admin" || acc.IdRole == 1) ? "Quản trị viên" : "Sinh viên";
            
            if (!string.IsNullOrWhiteSpace(user.HoTen))
            {
                var parts = user.HoTen.Trim().Split(' ');
                if (parts.Length > 0)
                {
                    string lastName = parts[parts.Length - 1];
                    if (!string.IsNullOrEmpty(lastName))
                        TxtHeaderAvatar.Text = lastName[0].ToString().ToUpper();
                }
            }
            
            if (!string.IsNullOrEmpty(user.AvatarUrl) && System.IO.File.Exists(user.AvatarUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.UriSource = new Uri(user.AvatarUrl, UriKind.Absolute);
                    bitmap.EndInit();
                    ImgAvatar.Source = bitmap;
                    ImgAvatar.Visibility = Visibility.Visible;
                    TxtHeaderAvatar.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    ImgAvatar.Visibility = Visibility.Collapsed;
                    TxtHeaderAvatar.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ImgAvatar.Visibility = Visibility.Collapsed;
                TxtHeaderAvatar.Visibility = Visibility.Visible;
            }

            TxtEditHoTen.Text       = user.HoTen;
            TxtEditEmail.Text       = user.Email;
            TxtEditSdt.Text         = user.Sdt;
            TxtEditMssv.Text        = acc.Username ?? "";
            DpNgaySinh.SelectedDate = user.NgaySinh;
            // Populate Quê Quán
            var provinces = new List<string> {
                "— Chọn tỉnh/thành phố —",
                "An Giang", "Bà Rịa - Vũng Tàu", "Bắc Giang", "Bắc Kạn", "Bạc Liêu", "Bắc Ninh", "Bến Tre", "Bình Định", "Bình Dương", "Bình Phước", "Bình Thuận", "Cà Mau", "Cần Thơ", "Cao Bằng", "Đà Nẵng", "Đắk Lắk", "Đắk Nông", "Điện Biên", "Đồng Nai", "Đồng Tháp", "Gia Lai", "Hà Giang", "Hà Nam", "Hà Nội", "Hà Tĩnh", "Hải Dương", "Hải Phòng", "Hậu Giang", "Hòa Bình", "Hưng Yên", "Khánh Hòa", "Kiên Giang", "Kon Tum", "Lai Châu", "Lâm Đồng", "Lạng Sơn", "Lào Cai", "Long An", "Nam Định", "Nghệ An", "Ninh Bình", "Ninh Thuận", "Phú Thọ", "Phú Yên", "Quảng Bình", "Quảng Nam", "Quảng Ngãi", "Quảng Ninh", "Quảng Trị", "Sóc Trăng", "Sơn La", "Tây Ninh", "Thái Bình", "Thái Nguyên", "Thanh Hóa", "Thừa Thiên Huế", "Tiền Giang", "TP Hồ Chí Minh", "Trà Vinh", "Tuyên Quang", "Vĩnh Long", "Vĩnh Phúc", "Yên Bái"
            };
            if (CmbQueQuan.Items.Count == 0)
            {
                foreach (var p in provinces) CmbQueQuan.Items.Add(new ComboBoxItem { Content = p });
            }
            
            CmbQueQuan.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(user.QueQuan))
            {
                foreach (ComboBoxItem item in CmbQueQuan.Items)
                {
                    if (item.Content.ToString() == user.QueQuan)
                    {
                        CmbQueQuan.SelectedItem = item;
                        break;
                    }
                }
            }

            CmbTruong.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(user.TruongHoc))
            {
                foreach (ComboBoxItem item in CmbTruong.Items)
                {
                    if (item.Content.ToString() == user.TruongHoc)
                    {
                        CmbTruong.SelectedItem = item;
                        break;
                    }
                }
            }

            CmbNganhHoc.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(user.NganhHoc))
            {
                foreach (ComboBoxItem item in CmbNganhHoc.Items)
                {
                    if (item.Content.ToString() == user.NganhHoc)
                    {
                        CmbNganhHoc.SelectedItem = item;
                        break;
                    }
                }
            }

            CmbKhoa.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(user.Khoa))
            {
                foreach (ComboBoxItem item in CmbKhoa.Items)
                {
                    if (item.Content.ToString() == user.Khoa)
                    {
                        CmbKhoa.SelectedItem = item;
                        break;
                    }
                }
            }

            CmbNhom.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(user.Nhom))
            {
                foreach (ComboBoxItem item in CmbNhom.Items)
                {
                    if (item.Content.ToString() == user.Nhom)
                    {
                        CmbNhom.SelectedItem = item;
                        break;
                    }
                }
            }

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

            if (string.IsNullOrWhiteSpace(TxtEditHoTen.Text))
            { ShowMsg(TxtProfileMsg, "Họ tên không được để trống.", false); return; }

            string newMssv = TxtEditMssv.Text.Trim();
            if (string.IsNullOrWhiteSpace(newMssv))
            { ShowMsg(TxtProfileMsg, "MSSV không được để trống.", false); return; }

            if (newMssv != acc.Username)
            {
                try
                {
                    UpdateUsername(acc.IdAcc, newMssv);
                    acc.Username = newMssv;
                }
                catch
                {
                    ShowMsg(TxtProfileMsg, "MSSV này đã tồn tại hoặc không hợp lệ.", false);
                    return;
                }
            }

            user.HoTen    = TxtEditHoTen.Text.Trim();
            user.Email    = TxtEditEmail.Text.Trim();
            user.Sdt      = TxtEditSdt.Text.Trim();
            user.NgaySinh = DpNgaySinh.SelectedDate;
            if (CmbQueQuan.SelectedIndex > 0)
                user.QueQuan = ((ComboBoxItem)CmbQueQuan.SelectedItem).Content.ToString();
            else
                user.QueQuan = null;

            if (CmbTruong.SelectedIndex > 0)
                user.TruongHoc = ((ComboBoxItem)CmbTruong.SelectedItem).Content.ToString();
            else
                user.TruongHoc = null;

            if (CmbNganhHoc.SelectedIndex > 0)
                user.NganhHoc = ((ComboBoxItem)CmbNganhHoc.SelectedItem).Content.ToString();
            else
                user.NganhHoc = null;

            if (CmbKhoa.SelectedIndex > 0)
                user.Khoa = ((ComboBoxItem)CmbKhoa.SelectedItem).Content.ToString();
            else
                user.Khoa = null;

            if (CmbNhom.SelectedIndex > 0)
                user.Nhom = ((ComboBoxItem)CmbNhom.SelectedItem).Content.ToString();
            else
                user.Nhom = null;

            long? newIdLop = (CmbClass.SelectedItem is ClassItem sel && sel.IdLop.HasValue)
                ? sel.IdLop : null;
            user.IdLop = newIdLop;

            _userDal.Update(user);
            _stuBll.UpdateStudentClass(acc.IdAcc, newIdLop);

            // Reload từ DB để lấy TenLop mới nhất → cập nhật session
            var refreshed = _authBll.GetUserWithClass(acc.IdAcc);
            if (refreshed != null) SessionManager.SetSession(acc, refreshed);
            else                   SessionManager.SetSession(acc, user);

            TxtHeaderName.Text = user.HoTen;
            TxtHeaderAvatar.Text   = user.HoTen.Length > 0 ? user.HoTen[0].ToString().ToUpper() : "?";
            ShowMsg(TxtProfileMsg, "✓ Lưu thành công!", true);
        }

        private void CmbTruong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTruong.SelectedIndex > 0)
            {
                CmbKhoa.IsEnabled = true;
            }
            else
            {
                CmbKhoa.IsEnabled = false;
                CmbKhoa.SelectedIndex = 0;
            }
        }

        private void CmbKhoa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbKhoa.SelectedIndex > 0)
            {
                CmbNganhHoc.IsEnabled = true;
                CmbClass.IsEnabled = true;
            }
            else
            {
                CmbNganhHoc.IsEnabled = false;
                CmbNganhHoc.SelectedIndex = 0;
                CmbClass.IsEnabled = false;
                CmbClass.SelectedIndex = 0;
            }
        }

        private void CmbNganhHoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbNganhHoc.SelectedIndex > 0)
            {
                CmbClass.IsEnabled = true;
            }
            else
            {
                CmbClass.IsEnabled = false;
                CmbClass.SelectedIndex = 0;
            }
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

        private static void UpdateUsername(long idAcc, string username)
        {
            const string sql = "UPDATE ACCOUNT SET username=@u, updated_at=GETDATE() WHERE id_acc=@id";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u",  username);
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
        private void BtnTabInfo_Click(object sender, RoutedEventArgs e)
        {
            ContainerInfo.Visibility = Visibility.Visible;
            ContainerReminder.Visibility = Visibility.Collapsed;
            ContainerPassword.Visibility = Visibility.Collapsed;

            BtnTabInfo.Style = (Style)FindResource("TabBtnActive");
            BtnTabReminder.Style = (Style)FindResource("TabBtnInactive");
            BtnTabPassword.Style = (Style)FindResource("TabBtnInactive");
        }

        private void BtnTabReminder_Click(object sender, RoutedEventArgs e)
        {
            ContainerInfo.Visibility = Visibility.Collapsed;
            ContainerReminder.Visibility = Visibility.Visible;
            ContainerPassword.Visibility = Visibility.Collapsed;

            BtnTabInfo.Style = (Style)FindResource("TabBtnInactive");
            BtnTabReminder.Style = (Style)FindResource("TabBtnActive");
            BtnTabPassword.Style = (Style)FindResource("TabBtnInactive");
        }

        private void BtnTabPassword_Click(object sender, RoutedEventArgs e)
        {
            ContainerInfo.Visibility = Visibility.Collapsed;
            ContainerReminder.Visibility = Visibility.Collapsed;
            ContainerPassword.Visibility = Visibility.Visible;

            BtnTabInfo.Style = (Style)FindResource("TabBtnInactive");
            BtnTabReminder.Style = (Style)FindResource("TabBtnInactive");
            BtnTabPassword.Style = (Style)FindResource("TabBtnActive");
        }

        private void BtnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Chọn ảnh đại diện"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var user = SessionManager.CurrentUser;
                    if (user == null) return;

                    var cropper = new Dialogs.ImageCropperDialog(openFileDialog.FileName);
                    if (cropper.ShowDialog() == true)
                    {
                        user.AvatarUrl = cropper.CroppedImagePath;
                        new UserDAL().Update(user);

                        LoadProfile();
                        ShowMsg(TxtProfileMsg, "Cập nhật ảnh đại diện thành công", true);
                    }
                }
                catch (Exception ex)
                {
                    ShowMsg(TxtProfileMsg, "Lỗi: " + ex.Message, false);
                }
            }
        }

        private void BtnDeleteAvatar_Click(object sender, RoutedEventArgs e)
        {
            var user = SessionManager.CurrentUser;
            if (user == null || string.IsNullOrEmpty(user.AvatarUrl)) return;

            try
            {
                user.AvatarUrl = null;
                new UserDAL().Update(user);

                LoadProfile();
                ShowMsg(TxtProfileMsg, "Đã xóa ảnh đại diện", true);
            }
            catch (Exception ex)
            {
                ShowMsg(TxtProfileMsg, "Lỗi: " + ex.Message, false);
            }
        }
    }
}