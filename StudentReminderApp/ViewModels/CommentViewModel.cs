using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using StudentReminderApp.Models;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.ViewModels
{
    public class CommentViewModel : BaseViewModel
    {
        private readonly ForumBLL _forumBLL = new ForumBLL();
        private long _postId;

        // Đối tượng bài viết hiện tại để hiển thị phần nội dung gốc 
        private Post _currentPost;
        public Post CurrentPost
        {
            get => _currentPost;
            set { _currentPost = value; OnPropertyChanged(); }
        }

        // Danh sách bình luận để hiển thị lên UI
        public ObservableCollection<Comment> Comments { get; set; }

        // Nội dung người dùng đang nhập
        private string _commentText = string.Empty;
        public string CommentText
        {
            get => _commentText;
            set { _commentText = value; OnPropertyChanged(); }
        }

        public ICommand SendCommentCommand { get; }

        // Constructor nhận nguyên object Post để lấy đủ thông tin (Avatar, Nội dung, Ảnh bài viết)
        public CommentViewModel(Post post)
        {
            if (post == null) return;

            CurrentPost = post;
            _postId = post.IdPost;
            
            Comments = new ObservableCollection<Comment>();
            SendCommentCommand = new RelayCommand(ExecuteSendComment);
            
            LoadComments();
        }

        // Hàm tải danh sách bình luận từ Database
        private void LoadComments()
        {
            try
            {
                var data = _forumBLL.GetComments(_postId);
                Comments.Clear();
                if (data != null)
                {
                    foreach (var c in data)
                    {
                        Comments.Add(c);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi LoadComments: " + ex.Message);
            }
        }

        private void ExecuteSendComment(object obj)
        {
            if (SessionManager.CurrentUser == null || string.IsNullOrWhiteSpace(CommentText)) 
                return;

            bool success = _forumBLL.PostComment(_postId, SessionManager.CurrentUser.IdAcc, CommentText.Trim());
            
            if (success)
            {
                CommentText = string.Empty; 
                LoadComments();           
            }
        }
    }
}