using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using System.Linq;
using StudentReminderApp.Models;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.ViewModels
{
    public class CommentViewModel : BaseViewModel
    {
        private readonly ForumBLL _forumBLL = new ForumBLL();
        private long _postId;

        private Post? _currentPost;
        public Post? CurrentPost
        {
            get => _currentPost;
            set { _currentPost = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Comment> Comments { get; set; }

        private string _commentText = string.Empty;
        public string CommentText
        {
            get => _commentText;
            set { _commentText = value; OnPropertyChanged(); }
        }

        public ICommand SendCommentCommand { get; }
        public ICommand DeleteCommentCommand { get; }

        public CommentViewModel(Post post)
        {
            Comments = new ObservableCollection<Comment>();

            SendCommentCommand = new RelayCommand(ExecuteSendComment);
            DeleteCommentCommand = new RelayCommand(ExecuteDeleteComment);

            if (post == null) return;
            CurrentPost = post;
            _postId = post.IdPost;

            LoadComments();
        }

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

        private void ExecuteSendComment(object? obj)
        {
            if (SessionManager.CurrentUser == null || string.IsNullOrWhiteSpace(CommentText))
                return;

            bool success = _forumBLL.PostComment(_postId, SessionManager.CurrentUser.IdAcc, CommentText.Trim());

            if (success)
            {
                CommentText = string.Empty;
                LoadComments();

                if (CurrentPost != null)
                {
                    CurrentPost.CommentCount++;
                }
            }
        }

        private void ExecuteDeleteComment(object? obj)
        {
            var comment = obj as Comment;

            if (comment != null)
            {
                bool isCommentOwner = SessionManager.CurrentUser != null && comment.IdAcc == SessionManager.CurrentUser.IdAcc;
                bool isPostOwner = SessionManager.CurrentUser != null && CurrentPost != null && CurrentPost.IdAcc == SessionManager.CurrentUser.IdAcc;

                if (!isCommentOwner && !isPostOwner)
                {
                    MessageBox.Show("Bạn không có quyền xóa bình luận này!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show("Bạn có chắc chắn muốn xóa bình luận này không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    bool success = _forumBLL.DeleteComment(comment.IdComment);

                    if (success)
                    {
                        Comments.Remove(comment);
                        if (CurrentPost != null && CurrentPost.CommentCount > 0)
                        {
                            CurrentPost.CommentCount--;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Không thể xóa bình luận. Vui lòng thử lại sau!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Lỗi: CommandParameter bị null!");
            }
        }
    }
}
