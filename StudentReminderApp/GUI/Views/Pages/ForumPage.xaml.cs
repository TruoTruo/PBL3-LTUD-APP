using System;
using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.Views.Dialogs;
using StudentReminderApp.ViewModels;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Pages
{
    public partial class ForumPage : UserControl
    {
        public ForumPage()
        {
            InitializeComponent();
            this.Loaded += UserControl_Loaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CheckVerification();
        }

        private void CheckVerification()
        {
            var acc = StudentReminderApp.Helpers.SessionManager.CurrentAccount;
            if (acc == null) return;

            // Admin được xem trực tiếp
            if (acc.IdRole == 1)
            {
                MainGrid.Visibility = Visibility.Visible;
                VerificationOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            if (acc.IsVerified)
            {
                MainGrid.Visibility = Visibility.Visible;
                VerificationOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainGrid.Visibility = Visibility.Collapsed;
                VerificationOverlay.Visibility = Visibility.Visible;

                bool isPending = acc.Status == "PENDING_VERIFY";
                string idCardDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "idcards");
                if (!isPending)
                {
                    string[] exts = { ".png", ".jpg", ".jpeg" };
                    foreach (var ext in exts)
                    {
                        if (System.IO.File.Exists(System.IO.Path.Combine(idCardDir, $"idcard_{acc.IdAcc}{ext}")))
                        {
                            isPending = true;
                            break;
                        }
                    }
                }

                if (isPending)
                {
                    TxtVerificationStatus.Text = "Ảnh thẻ sinh viên của bạn đang chờ Quản trị viên duyệt. Vui lòng quay lại sau.";
                    BtnUploadIdCard.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TxtVerificationStatus.Text = "Để bảo vệ cộng đồng sinh viên an toàn, bạn cần tải lên ảnh thẻ sinh viên để Quản trị viên duyệt trước khi có thể đọc và tham gia diễn đàn.";
                    BtnUploadIdCard.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnUploadIdCard_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try {
                    string idCardDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "idcards");
                    if (!System.IO.Directory.Exists(idCardDir)) System.IO.Directory.CreateDirectory(idCardDir);
                    
                    string ext = System.IO.Path.GetExtension(openFileDialog.FileName);
                    string targetName = $"idcard_{StudentReminderApp.Helpers.SessionManager.CurrentAccount.IdAcc}{ext}";
                    string targetPath = System.IO.Path.Combine(idCardDir, targetName);
                    
                    System.IO.File.Copy(openFileDialog.FileName, targetPath, true);
                    
                    StudentReminderApp.Helpers.SessionManager.CurrentAccount.Status = "PENDING_VERIFY";
                    CheckVerification();

                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(targetPath, UriKind.Absolute);
                    bmp.EndInit();
                    ImgIdCardPreview.Source = bmp;
                    ImgIdCardPreview.Visibility = Visibility.Visible;

                    MessageBox.Show("Đã nộp ảnh thẻ sinh viên thành công! Vui lòng chờ Admin duyệt.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                } catch (Exception ex) {
                    MessageBox.Show("Lỗi khi tải ảnh: " + ex.Message);
                }
            }
        }

        // -------------------------------------------------------
        // Mở dialog tạo bài mới
        // -------------------------------------------------------
        private void OpenCreatePost_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var popup = new CreatePostDialog();

                if (this.DataContext is ForumViewModel vm)
                {
                    popup.DataContext = vm;
                    vm.CloseAction = () => popup.Close();
                }

                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null) popup.Owner = parentWindow;

                popup.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi mở cửa sổ đăng bài: " + ex.Message);
            }
        }

        // -------------------------------------------------------
        // Mở dialog bình luận
        // -------------------------------------------------------
        private void CommentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Post post)
            {
                var diag = new CommentDialog(post);
                diag.Owner = Window.GetWindow(this);
                diag.ShowDialog();
            }
        }

        // -------------------------------------------------------
        // Chia sẻ bài viết
        // -------------------------------------------------------
        private async void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var post   = button?.CommandParameter as Post;

            if (post != null)
            {
                try
                {
                    var shareDialog = new ShareDialog(post);
                    shareDialog.Owner = Window.GetWindow(this);

                    bool? result = shareDialog.ShowDialog();

                    if (result == true && this.DataContext is ForumViewModel vm)
                    {
                        await vm.LoadDataAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi chia sẻ: " + ex.Message);
                }
            }
        }

        // -------------------------------------------------------
        // Mở Context Menu (nút ...)
        // -------------------------------------------------------
        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn?.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        // -------------------------------------------------------
        // Admin: Làm mới danh sách bài chờ duyệt
        // -------------------------------------------------------
        private async void RefreshPending_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ForumViewModel vm)
            {
                await vm.LoadPendingPostsAsync();
            }
        }
    }
}
