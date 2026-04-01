using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        private Post? _sharingPost; 

        private string _newContent = string.Empty;
        public string NewContent
        {
            get => _newContent;
            set { _newContent = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

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
        public ICommand LikeCommand { get; }
        public ICommand OpenShareCommand { get; } 

        public ForumViewModel()
        {
            Posts = new ObservableCollection<Post>();
            
            PostCommand = new RelayCommand(
                async (obj) => await ExecutePost(obj),
                (obj) => !string.IsNullOrWhiteSpace(NewContent) && !IsBusy
            );

            DeletePostCommand = new RelayCommand(ExecuteDeletePostWrapper);
            LikeCommand = new RelayCommand(async (obj) => await ExecuteLike(obj));
            
            OpenShareCommand = new RelayCommand((obj) => {
                if (obj is Post p) {
                    _sharingPost = p;
                    NewContent = "Đã chia sẻ một bài viết"; 
                }
            });

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            try 
            {
                var data = await Task.Run(() => _forumBLL.GetAllPosts());
                
                if (data != null)
                {
                    var postDict = data.ToDictionary(p => p.IdPost, p => p);

                    foreach (var item in data)
                    {
                        if (item.IdOriginalPost.HasValue && postDict.ContainsKey(item.IdOriginalPost.Value))
                        {
                            item.OriginalPost = postDict[item.IdOriginalPost.Value];
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Posts.Clear();
                        foreach (var p in data.OrderByDescending(x => x.CreatedAt))
                        {
                            Posts.Add(p);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi nạp dữ liệu Diễn đàn: " + ex.Message);
            }
        }

        private async Task ExecutePost(object? parameter)
        {
            if (SessionManager.CurrentUser == null) return;
            IsBusy = true;
            try
            {
                long currentUserId = SessionManager.CurrentUser.IdAcc;
                long? originalId = _sharingPost?.IdPost; 
                bool success = await Task.Run(() =>
                    _forumBLL.CreatePost(currentUserId, "Thảo luận", NewContent, IsAnonymous, originalId)
                );

                if (success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        NewContent = string.Empty;
                        IsAnonymous = false;
                        _sharingPost = null; 
                        CloseAction?.Invoke();
                    });
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi đăng bài: " + ex.Message);
            }
            finally { IsBusy = false; }
        }

        private async Task ExecuteLike(object? parameter)
        {
            if (parameter is Post post && SessionManager.CurrentUser != null)
            {
                long currentUserId = SessionManager.CurrentUser.IdAcc;
                bool success = await Task.Run(() => _forumBLL.ToggleLike(currentUserId, post.IdPost));
                if (success)
                {
                    post.IsLiked = !post.IsLiked;
                    if (post.IsLiked) post.Likes++;
                    else post.Likes--;
                }
            }
        }

        private void ExecuteDeletePostWrapper(object? parameter)
        {
            if (parameter is Post post)
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn xóa bài viết này không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) ExecuteDeletePost(post);
            }
        }

        private void ExecuteDeletePost(Post post)
        {
            try
            {
                if (_forumBLL.RemovePost(post.IdPost)) Posts.Remove(post);
                else MessageBox.Show("Không thể xóa bài viết này.");
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }
    }
}