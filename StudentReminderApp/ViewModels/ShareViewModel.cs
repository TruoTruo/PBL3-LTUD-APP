using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;
using StudentReminderApp.BLL;

namespace StudentReminderApp.ViewModels
{
    public class ShareViewModel : BaseViewModel
    {
        private readonly ForumBLL _forumBLL = new ForumBLL();
        private Post _originalPost;

        public string CurrentUserName => SessionManager.CurrentUser?.HoTen ?? "Người dùng";
        public string CurrentUserAvatar => "/Resources/Images/user.png";

        public ObservableCollection<string> PrivacyOptions { get; } = new ObservableCollection<string> { "🌎 Công khai", "🔒 Chỉ mình tôi" };

        private string _selectedPrivacyMode = "🌎 Công khai";
        public string SelectedPrivacyMode
        {
            get => _selectedPrivacyMode;
            set { _selectedPrivacyMode = value; OnPropertyChanged(); }
        }

        private string _userComment = "";
        public string UserComment
        {
            get => _userComment;
            set { _userComment = value; OnPropertyChanged(); }
        }

        public ICommand ShareNowCommand { get; }

        public ShareViewModel(Post post)
        {
            _originalPost = post;
            _selectedPrivacyMode = PrivacyOptions[0];

            ShareNowCommand = new RelayCommand((obj) =>
            {
                if (obj is Window window)
                {
                    ExecuteShareNow(window);
                }
                else
                {
                    MessageBox.Show("Lỗi: Nút bấm thiếu CommandParameter trong XAML!");
                }
            });
        }

        private void ExecuteShareNow(Window window)
        {
            try
            {
                if (_originalPost == null) { MessageBox.Show("Không tìm thấy bài viết gốc!"); return; }
                if (SessionManager.CurrentUser == null) { MessageBox.Show("Vui lòng đăng nhập lại!"); return; }

                bool isPublic = SelectedPrivacyMode.Contains("Công khai");

                long postId = _originalPost.IdPost;
                long userId = SessionManager.CurrentUser.IdAcc;

                bool success = _forumBLL.SharePost(postId, userId, UserComment, isPublic);

                if (success)
                {
                    MessageBox.Show("✅ Chia sẻ bài viết thành công!");
                    window.DialogResult = true; 
                    window.Close(); 
                }
                else
                {
                    MessageBox.Show("❌ Lỗi: Không thể lưu vào Database. Hãy kiểm tra Store Procedure!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message);
            }
        }
    }
}