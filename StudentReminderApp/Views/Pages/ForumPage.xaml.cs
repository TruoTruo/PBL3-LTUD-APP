using System;
using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.Views.Dialogs;
using StudentReminderApp.ViewModels;
using StudentReminderApp.Models;
using System.Diagnostics;
using System.Windows.Documents;

namespace StudentReminderApp.Views.Pages
{
    public partial class ForumPage : UserControl
    {
        public ForumPage()
        {
            InitializeComponent();
        }

        /// Mở cửa sổ tạo bài viết mới
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

        /// Chức năng: Mở cửa sổ Bình luận
 
        private void CommentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Post post)
            {
                var diag = new CommentDialog(post);
                diag.Owner = Window.GetWindow(this);
                diag.ShowDialog();
            }
        }

        /// Chức năng: Chia sẻ bài viết
  
        private async void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var post = button?.CommandParameter as Post;

            if (post != null)
            {
                try
                {
                    var shareDialog = new ShareDialog(post);
                    shareDialog.Owner = Window.GetWindow(this);

                    bool? result = shareDialog.ShowDialog();

                    if (result == true)
                    {
                        if (this.DataContext is ForumViewModel vm)
                        {
                            await vm.LoadDataAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi chia sẻ: " + ex.Message);
                }
            }
        }

        /// Chức năng: Xử lý nút Option nếu có Context Menu

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }
    }
}