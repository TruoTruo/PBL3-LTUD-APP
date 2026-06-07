using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Dialogs
{
    public partial class UserDetailsDialog : Window
    {
        public UserDetailsDialog(UserManagementDto user)
        {
            InitializeComponent();
            LoadUserData(user);
        }

        private void LoadUserData(UserManagementDto user)
        {
            TxtName.Text = string.IsNullOrEmpty(user.HoTen) ? "Chưa cập nhật" : user.HoTen;
            TxtRole.Text = user.RoleName;

            TxtUsername.Text = user.Username;
            TxtEmail.Text = string.IsNullOrEmpty(user.Email) ? "Trống" : user.Email;
            TxtSdt.Text = string.IsNullOrEmpty(user.Sdt) ? "Trống" : user.Sdt;
            TxtDob.Text = user.NgaySinh.HasValue ? user.NgaySinh.Value.ToString("dd/MM/yyyy") : "Trống";
            TxtTruong.Text = string.IsNullOrEmpty(user.TruongHoc) ? "Trống" : user.TruongHoc;
            TxtNganh.Text = string.IsNullOrEmpty(user.NganhHoc) ? "Trống" : user.NganhHoc;
            TxtLop.Text = string.IsNullOrEmpty(user.TenLop) ? "Trống" : user.TenLop;
            TxtNhom.Text = string.IsNullOrEmpty(user.Nhom) ? "Trống" : user.Nhom;
            TxtQueQuan.Text = string.IsNullOrEmpty(user.QueQuan) ? "Trống" : user.QueQuan;

            // Load Avatar
            if (!string.IsNullOrEmpty(user.AvatarUrl) && File.Exists(user.AvatarUrl))
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
                    TxtInitials.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    SetDefaultAvatar(user.HoTen);
                }
            }
            else
            {
                SetDefaultAvatar(user.HoTen);
            }
        }

        private void SetDefaultAvatar(string name)
        {
            ImgAvatar.Visibility = Visibility.Collapsed;
            TxtInitials.Visibility = Visibility.Visible;
            TxtInitials.Text = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpper();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
