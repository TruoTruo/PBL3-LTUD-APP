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

        // ── QUẢN LÝ DANH MỤC LÕI ───────────────────────────────────────
        private string _selectedJsonFilePath = "";

        private void LoadDanhMuc()
        {
            if (CboCategory != null && CboCategory.SelectedItem is ComboBoxItem selected && selected.Tag != null)
            {
                string tag = selected.Tag.ToString();
                if (tag == "LOP")
                {
                    _danhMucList = _danhMucDal.GetAllClasses();
                    if (ColNienKhoa != null) ColNienKhoa.Width = GridLength.Auto;
                    if (TxtNienKhoa != null) TxtNienKhoa.Visibility = Visibility.Visible;
                    if (TxtNienKhoaPlaceholder != null) TxtNienKhoaPlaceholder.Visibility = Visibility.Visible;
                    if (SpJsonFile != null) SpJsonFile.Visibility = Visibility.Collapsed;
                    if (DgDanhMuc != null && DgDanhMuc.Columns.Count > 2) DgDanhMuc.Columns[2].Header = "Niên khóa (Lớp)";
                }
                else if (tag == "NGANH")
                {
                    _danhMucList = _danhMucDal.GetByCategory(tag);
                    foreach (var item in _danhMucList)
                    {
                        item.NienKhoa = $@"RENDER\{item.Value}.json";
                    }
                    if (ColNienKhoa != null) ColNienKhoa.Width = GridLength.Auto;
                    if (TxtNienKhoa != null) TxtNienKhoa.Visibility = Visibility.Collapsed;
                    if (TxtNienKhoaPlaceholder != null) TxtNienKhoaPlaceholder.Visibility = Visibility.Collapsed;
                    if (SpJsonFile != null) SpJsonFile.Visibility = Visibility.Visible;
                    if (DgDanhMuc != null && DgDanhMuc.Columns.Count > 2) DgDanhMuc.Columns[2].Header = "Đường dẫn File JSON";
                }
                else
                {
                    _danhMucList = _danhMucDal.GetByCategory(tag);
                    foreach (var item in _danhMucList) item.NienKhoa = "";
                    if (ColNienKhoa != null) ColNienKhoa.Width = new GridLength(0);
                    if (TxtNienKhoa != null) TxtNienKhoa.Visibility = Visibility.Collapsed;
                    if (TxtNienKhoaPlaceholder != null) TxtNienKhoaPlaceholder.Visibility = Visibility.Collapsed;
                    if (SpJsonFile != null) SpJsonFile.Visibility = Visibility.Collapsed;
                    if (DgDanhMuc != null && DgDanhMuc.Columns.Count > 2) DgDanhMuc.Columns[2].Header = "";
                }
                if (DgDanhMuc != null) DgDanhMuc.ItemsSource = _danhMucList;
            }
        }

        private void ClearJsonFile()
        {
            _selectedJsonFilePath = "";
            if (TxtJsonFile != null) TxtJsonFile.Text = "";
            if (TxtJsonFilePlaceholder != null) TxtJsonFilePlaceholder.Visibility = Visibility.Visible;
        }

        private void CboCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDanhMuc();
            if (TxtDanhMucValue != null) TxtDanhMucValue.Text = "";
            if (TxtNienKhoa != null) TxtNienKhoa.Text = "";
            ClearJsonFile();
        }

        private void DgDanhMuc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgDanhMuc.SelectedItem is DanhMucItem item)
            {
                TxtDanhMucValue.Text = item.Value;
                TxtNienKhoa.Text = item.NienKhoa;
                
                if (TxtDanhMucValuePlaceholder != null)
                    TxtDanhMucValuePlaceholder.Visibility = Visibility.Hidden;
                if (TxtNienKhoaPlaceholder != null)
                    TxtNienKhoaPlaceholder.Visibility = string.IsNullOrEmpty(item.NienKhoa) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void TxtDanhMucValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtDanhMucValuePlaceholder != null)
                TxtDanhMucValuePlaceholder.Visibility = string.IsNullOrEmpty(TxtDanhMucValue.Text) ? Visibility.Visible : Visibility.Hidden;
        }

        private void TxtNienKhoa_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtNienKhoaPlaceholder != null)
                TxtNienKhoaPlaceholder.Visibility = string.IsNullOrEmpty(TxtNienKhoa.Text) ? Visibility.Visible : Visibility.Hidden;
        }

        private void BtnAddDanhMuc_Click(object sender, RoutedEventArgs e)
        {
            string rawValue = TxtDanhMucValue.Text.Trim();
            if (string.IsNullOrEmpty(rawValue))
            {
                MessageBox.Show("Vui lòng nhập giá trị! (Có thể nhập nhiều giá trị cách nhau bằng dấu phẩy)", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tag = ((ComboBoxItem)CboCategory.SelectedItem).Tag.ToString();

            if (tag == "NGANH" && string.IsNullOrEmpty(_selectedJsonFilePath))
            {
                MessageBox.Show("Vui lòng đính kèm file JSON (Đây là điều kiện bắt buộc đối với Ngành Học)!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var values = rawValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int count = 0;
                foreach (var v in values)
                {
                    string value = v.Trim();
                    if (string.IsNullOrEmpty(value)) continue;

                    if (tag == "LOP")
                    {
                        _danhMucDal.AddClass(value, TxtNienKhoa.Text.Trim());
                    }
                    else
                    {
                        _danhMucDal.AddDanhMucChung(tag, value);
                        if (tag == "NGANH" && !string.IsNullOrEmpty(_selectedJsonFilePath) && System.IO.File.Exists(_selectedJsonFilePath))
                        {
                            string targetDir = FindRenderFolder();
                            if (!string.IsNullOrEmpty(targetDir))
                            {
                                try {
                                    string targetFile = System.IO.Path.Combine(targetDir, value + ".json");
                                    System.IO.File.Copy(_selectedJsonFilePath, targetFile, true);
                                } catch { }
                            }
                        }
                    }
                    count++;
                }

                if (count > 0)
                {
                    MessageBox.Show($"Đã thêm thành công {count} mục!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    TxtDanhMucValue.Text = "";
                    TxtNienKhoa.Text = "";
                    ClearJsonFile();
                    LoadDanhMuc();
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập giá trị hợp lệ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FindRenderFolder()
        {
            string current = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                string path = System.IO.Path.Combine(current, "RENDER");
                if (System.IO.Directory.Exists(path)) return path;
                current = System.IO.Path.GetFullPath(System.IO.Path.Combine(current, ".."));
            }
            return "";
        }

        private void BtnSelectJson_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files|*.json|All Files|*.*",
                Title = "Chọn file dữ liệu môn học (.json)"
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedJsonFilePath = dialog.FileName;
                TxtJsonFile.Text = System.IO.Path.GetFileName(dialog.FileName);
                if (TxtJsonFilePlaceholder != null) TxtJsonFilePlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnUpdateDanhMuc_Click(object sender, RoutedEventArgs e)
        {
            if (DgDanhMuc.SelectedItem is not DanhMucItem item)
            {
                MessageBox.Show("Vui lòng chọn một mục để cập nhật!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string value = TxtDanhMucValue.Text.Trim();
            if (string.IsNullOrEmpty(value))
            {
                MessageBox.Show("Vui lòng nhập giá trị!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tag = ((ComboBoxItem)CboCategory.SelectedItem).Tag.ToString();
            try
            {
                if (tag == "LOP")
                {
                    _danhMucDal.UpdateClass(item.Id, value, TxtNienKhoa.Text.Trim());
                }
                else
                {
                    _danhMucDal.UpdateDanhMucChung(item.Id, value);
                }
                MessageBox.Show("Cập nhật thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDanhMuc();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteDanhMuc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DanhMucItem item)
            {
                var result = MessageBox.Show($"Bạn có chắc muốn xóa '{item.Value}' không?\nLưu ý: Thao tác này có thể ảnh hưởng đến các dữ liệu đang sử dụng danh mục này.", 
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    string tag = ((ComboBoxItem)CboCategory.SelectedItem).Tag.ToString();
                    try
                    {
                        if (tag == "LOP")
                            _danhMucDal.DeleteClass(item.Id);
                        else
                            _danhMucDal.DeleteDanhMucChung(item.Id);

                        MessageBox.Show("Xóa thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadDanhMuc();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
