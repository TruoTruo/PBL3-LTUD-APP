using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using StudentReminderApp.BLL;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;


namespace StudentReminderApp.ViewModels
{
    public class ForumViewModel : BaseViewModel
    {
        private readonly ForumBLL _forumBLL = new ForumBLL();
        public ObservableCollection<Post> Posts { get; set; }
        public Action? CloseAction { get; set; }

        private string _newContent = string.Empty;
        public string NewContent
        {
            get => _newContent;
            set { _newContent = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        // Thêm Property này để Binding với Toggle/Checkbox ẩn danh trên giao diện
        private bool _isAnonymous;
        public bool IsAnonymous
        {
            get => _isAnonymous;
            set { _isAnonymous = value; OnPropertyChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public ICommand PostCommand { get; }
        public ICommand DeletePostCommand { get; set; }

        public ForumViewModel()
        {
            Posts = new ObservableCollection<Post>();
            PostCommand = new RelayCommand(
                async (obj) => await ExecutePost(obj),
                (obj) => !string.IsNullOrWhiteSpace(NewContent) && !IsBusy
            );
            DeletePostCommand = new RelayCommand(ExecuteDeletePostWrapper);
            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            var data = await Task.Run(() => _forumBLL.GetAllPosts());
            Application.Current.Dispatcher.Invoke(() =>
            {
                Posts.Clear();
                if (data != null)
                {
                    foreach (var p in data) Posts.Add(p);
                }
            });
        }

        private async Task ExecutePost(object? parameter)
        {
            if (SessionManager.CurrentUser == null) return;
            IsBusy = true;

            try
            {
                // Lấy ID người dùng thật để hệ thống vẫn quản lý được
                long currentUserId = SessionManager.CurrentUser.IdAcc;

                // Gọi hàm CreatePost với ĐỦ 4 tham số (tham số cuối là IsAnonymous)
                bool success = await Task.Run(() =>
                    _forumBLL.CreatePost(currentUserId, "Thảo luận", NewContent, IsAnonymous)
                );

                if (success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        NewContent = string.Empty;
                        IsAnonymous = false; // Reset trạng thái sau khi đăng
                        CloseAction?.Invoke();
                    });
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteDeletePostWrapper(object? parameter)
        {
            // Kiểm tra ép kiểu an toàn
            if (parameter is Post post)
            {
                // Thêm hộp thoại xác nhận để tránh bấm nhầm
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa bài viết này không?",
                                           "Xác nhận xóa",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ExecuteDeletePost(post);
                }
            }
        }

        private void ExecuteDeletePost(Post post)
        {
            if (post == null) return;

            try
            {
                // Gọi BLL để xóa trong Database
                bool success = _forumBLL.RemovePost(post.IdPost);

                if (success)
                {
                    // Xóa khỏi ObservableCollection để giao diện cập nhật ngay lập tức
                    Posts.Remove(post);
                }
                else
                {
                    MessageBox.Show("Không thể xóa bài viết. Vui lòng thử lại sau.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message);
            }
        }

        // Hàm xóa chính của bạn (giữ nguyên hoặc sửa nhẹ)

    }
}