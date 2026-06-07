using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using StudentReminderApp.BLL;
using StudentReminderApp.DAL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Pages
{
    public partial class ManagementPage : Page
    {
        private List<UserManagementDto> _allUsers = new();
        private readonly AccountDAL _accountDal = new();
        private readonly StudentBLL _stuBll = new();
        private readonly DanhMucDAL _danhMucDal = new();
        private ICollectionView _collectionView;
        private List<DanhMucItem> _danhMucList = new();

        public ManagementPage()
        {
            InitializeComponent();
            Loaded += (s, e) => {
                LoadFilters();
                LoadData();
                LoadDanhMuc();
            };
        }

        private void LoadFilters()
        {
            var rawList = _stuBll.GetAllClasses();
            CboFilterLop.Items.Clear();
            CboFilterLop.Items.Add(new ComboBoxItem { Content = "Tất cả lớp", IsSelected = true });
            foreach (var (id, ten) in rawList)
            {
                CboFilterLop.Items.Add(new ComboBoxItem { Content = ten });
            }
        }

        private void LoadData()
        {
            _allUsers = _accountDal.GetAllUsersManagement();
            _collectionView = CollectionViewSource.GetDefaultView(_allUsers);
            _collectionView.Filter = UserFilter;
            DgUsers.ItemsSource = _collectionView;
        }

        private bool UserFilter(object item)
        {
            if (item is not UserManagementDto user) return false;

            // Xử lý dòng trống (không có Username)
            if (string.IsNullOrEmpty(user.Username)) return false;

            string keyword = TxtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(keyword))
            {
                bool matchName = user.HoTen != null && user.HoTen.ToLower().Contains(keyword);
                bool matchMssv = user.Username != null && user.Username.ToLower().Contains(keyword);
                if (!matchName && !matchMssv) return false;
            }

            if (CboFilterTruong != null && CboFilterTruong.SelectedIndex > 0)
            {
                string truong = ((ComboBoxItem)CboFilterTruong.SelectedItem).Content.ToString();
                if (user.TruongHoc != truong) return false;
            }

            if (CboFilterKhoa != null && CboFilterKhoa.SelectedIndex > 0)
            {
                string khoa = ((ComboBoxItem)CboFilterKhoa.SelectedItem).Content.ToString();
                if (user.Khoa != khoa) return false;
            }

            if (CboFilterLop != null && CboFilterLop.SelectedIndex > 0)
            {
                string lop = ((ComboBoxItem)CboFilterLop.SelectedItem).Content.ToString();
                if (user.TenLop != lop) return false;
            }

            return true;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtSearchPlaceholder != null)
                TxtSearchPlaceholder.Visibility = string.IsNullOrEmpty(TxtSearch.Text) ? Visibility.Visible : Visibility.Hidden;
            
            _collectionView?.Refresh();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _collectionView?.Refresh();
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserManagementDto user)
            {
                var dialog = new Dialogs.UserDetailsDialog(user);
                dialog.ShowDialog();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserManagementDto user)
            {
                if (user.IdAcc == SessionManager.CurrentAccount?.IdAcc)
                {
                    MessageBox.Show("Bạn không thể xóa chính mình!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa tài khoản {user.Username} - {user.HoTen}?\nHành động này không thể hoàn tác.", 
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _accountDal.DeleteAccount(user.IdAcc);
                        MessageBox.Show("Xóa tài khoản thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Có lỗi xảy ra khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // ── QUẢN LÝ DANH MỤC LÕI (Cột JSON) ─────────────────────────
        private OrganizationData _orgData = new();
        private readonly string _orgJsonPath = AppConfig.OrganizationJsonPath;

        private void LoadDanhMuc()
        {
            if (System.IO.File.Exists(_orgJsonPath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(_orgJsonPath);
                    _orgData = Newtonsoft.Json.JsonConvert.DeserializeObject<OrganizationData>(json) ?? new OrganizationData();
                }
                catch { _orgData = new OrganizationData(); }
            }
            else
            {
                _orgData = new OrganizationData();
            }

            LstTruong.ItemsSource = _orgData.Truongs;
            LstTruong.Items.Refresh();
        }

        private void BtnSaveOrg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(_orgData, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(_orgJsonPath, json);
                MessageBox.Show("Đã lưu thay đổi vào Organization.json thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu JSON: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Trường ──
        private void LstTruong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstTruong.SelectedItem is TruongData t)
            {
                LstKhoa.ItemsSource = t.Khoas;
                LstKhoa.Items.Refresh();
                LstNhom.ItemsSource = t.Nhoms;
                LstNhom.Items.Refresh();
            }
            else
            {
                LstKhoa.ItemsSource = null;
                LstNhom.ItemsSource = null;
            }
            LstNganh.ItemsSource = null;
            LstLop.ItemsSource = null;
        }

        private void BtnAddTruong_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtNewTruong.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;
            if (_orgData.Truongs.Exists(x => x.Name == name)) return;
            _orgData.Truongs.Add(new TruongData { Name = name });
            LstTruong.Items.Refresh();
            TxtNewTruong.Clear();
        }

        private void BtnDelTruong_Click(object sender, RoutedEventArgs e)
        {
            if (LstTruong.SelectedItem is TruongData t)
            {
                _orgData.Truongs.Remove(t);
                LstTruong.Items.Refresh();
            }
        }

        // ── Khoa ──
        private void LstKhoa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstKhoa.SelectedItem is KhoaData k)
            {
                LstNganh.ItemsSource = k.Nganhs;
                LstNganh.Items.Refresh();
            }
            else
            {
                LstNganh.ItemsSource = null;
            }
            LstLop.ItemsSource = null;
        }

        private void BtnAddKhoa_Click(object sender, RoutedEventArgs e)
        {
            if (LstTruong.SelectedItem is not TruongData t) return;
            string name = TxtNewKhoa.Text.Trim();
            if (string.IsNullOrEmpty(name) || t.Khoas.Exists(x => x.Name == name)) return;
            t.Khoas.Add(new KhoaData { Name = name });
            LstKhoa.Items.Refresh();
            TxtNewKhoa.Clear();
        }

        private void BtnDelKhoa_Click(object sender, RoutedEventArgs e)
        {
            if (LstTruong.SelectedItem is TruongData t && LstKhoa.SelectedItem is KhoaData k)
            {
                t.Khoas.Remove(k);
                LstKhoa.Items.Refresh();
            }
        }

        // ── Ngành ──
        private void LstNganh_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstNganh.SelectedItem is NganhData n)
            {
                LstLop.ItemsSource = n.Lops;
                LstLop.Items.Refresh();
            }
            else
            {
                LstLop.ItemsSource = null;
            }
        }

        private void BtnAddNganh_Click(object sender, RoutedEventArgs e)
        {
            if (LstKhoa.SelectedItem is not KhoaData k) return;
            string name = TxtNewNganh.Text.Trim();
            if (string.IsNullOrEmpty(name) || k.Nganhs.Exists(x => x.Name == name)) return;
            k.Nganhs.Add(new NganhData { Name = name });
            LstNganh.Items.Refresh();
            TxtNewNganh.Clear();
        }

        private void BtnDelNganh_Click(object sender, RoutedEventArgs e)
        {
            if (LstKhoa.SelectedItem is KhoaData k && LstNganh.SelectedItem is NganhData n)
            {
                k.Nganhs.Remove(n);
                LstNganh.Items.Refresh();
            }
        }

        // ── Lớp ──
        private void LstLop_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void BtnAddLop_Click(object sender, RoutedEventArgs e)
        {
            if (LstNganh.SelectedItem is not NganhData n) return;
            string name = TxtNewLop.Text.Trim();
            if (string.IsNullOrEmpty(name) || n.Lops.Exists(x => x.Name == name)) return;
            n.Lops.Add(new LopData { Name = name });
            LstLop.Items.Refresh();
            TxtNewLop.Clear();
        }

        private void BtnDelLop_Click(object sender, RoutedEventArgs e)
        {
            if (LstNganh.SelectedItem is NganhData n && LstLop.SelectedItem is LopData l)
            {
                n.Lops.Remove(l);
                LstLop.Items.Refresh();
            }
        }

        // ── Nhóm ──
        private void BtnAddNhom_Click(object sender, RoutedEventArgs e)
        {
            if (LstTruong.SelectedItem is not TruongData t) return;
            string name = TxtNewNhom.Text.Trim();
            if (string.IsNullOrEmpty(name) || t.Nhoms.Contains(name)) return;
            t.Nhoms.Add(name);
            LstNhom.Items.Refresh();
            TxtNewNhom.Clear();

            // Cập nhật tự động vào file JSON
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(_orgData, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(_orgJsonPath, json);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi khi lưu JSON: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }

            // Cập nhật tự động vào Database (Category = NHOM)
            try
            {
                _danhMucDal.AddDanhMucChung("NHOM", name);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi khi thêm vào Database: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnDelNhom_Click(object sender, RoutedEventArgs e)
        {
            if (LstTruong.SelectedItem is TruongData t && LstNhom.SelectedItem is string nhom)
            {
                t.Nhoms.Remove(nhom);
                LstNhom.Items.Refresh();

                // Cập nhật tự động xóa khỏi file JSON
                try
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(_orgData, Newtonsoft.Json.Formatting.Indented);
                    System.IO.File.WriteAllText(_orgJsonPath, json);
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi khi lưu JSON: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }

                // Cập nhật tự động xóa khỏi Database
                try
                {
                    using var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString);
                    conn.Open();
                    using var cmd = new System.Data.SqlClient.SqlCommand("DELETE FROM DANH_MUC_CHUNG WHERE Category = 'NHOM' AND Value = @val", conn);
                    cmd.Parameters.AddWithValue("@val", nhom);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi khi xóa khỏi Database: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }
    }
}
