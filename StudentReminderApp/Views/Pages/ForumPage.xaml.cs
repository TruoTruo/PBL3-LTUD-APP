using System;
using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.Views.Dialogs;
using StudentReminderApp.ViewModels;

namespace StudentReminderApp.Views.Pages
{
    public partial class ForumPage : UserControl
    {
        public ForumPage()
        {
            InitializeComponent();
        }

        private void OpenCreatePost_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Tạo mới cửa sổ con
                var popup = new CreatePostDialog();

                // 1. Chia sẻ DataContext để dùng chung ViewModel (giúp tự load lại bài viết)
                if (this.DataContext != null)
                {
                    popup.DataContext = this.DataContext;
                }

                // 2. Thiết lập cửa sổ cha cho Popup (Quan trọng để không bị thoát ẩn)
                var parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    popup.Owner = parentWindow;
                }

                // 3. Hiển thị Popup dưới dạng Dialog (Cửa sổ con)
                popup.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở cửa sổ tạo bài viết: " + ex.Message, "Lỗi Hệ Thống");
            }
        }
        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            // Ép kiểu sender về Button
            Button btn = sender as Button;

            // Nếu nút có chứa ContextMenu, chúng ta sẽ ép nó mở ra bằng chuột trái
            if (btn != null && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }
    }
}