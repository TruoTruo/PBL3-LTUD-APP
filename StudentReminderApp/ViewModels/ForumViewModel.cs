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
using System.IO;
using System.Windows.Media.Imaging;

namespace StudentReminderApp.ViewModels
{
    public enum PostCategory
    {
        All,
        Hot,
        Student,
        Announcement
    }

    public class FileAttachment : BaseViewModel
    {
        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); UpdatePreviewImage(); }
        }
        public string? FileName { get; set; }

        private BitmapImage? _previewImage;
        public BitmapImage? PreviewImage
        {
            get => _previewImage;
            set { _previewImage = value; OnPropertyChanged(); }
        }

        public string FileExtension => !string.IsNullOrEmpty(FilePath)
            ? Path.GetExtension(FilePath).ToUpper().Replace(".", "") : "";

        public bool IsImage
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath)) return false;
                string ext = Path.GetExtension(FilePath).ToLower();
                return ext == ".jpg" || ext == ".png" || ext == ".jpeg" || ext == ".bmp";
            }
        }
        public bool IsNotImage => !IsImage;

        private void UpdatePreviewImage()
        {
            if (IsImage && File.Exists(FilePath))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(FilePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage = bitmap;
                }
                catch { PreviewImage = null; }
            }
            else { PreviewImage = null; }
        }
    }

    public class ForumViewModel : BaseViewModel
    {
        private readonly ForumBLL _forumBLL = new ForumBLL();

        // ── PHÂN QUYỀN ──────────────────────────────────────────
        public bool IsAdmin => SessionManager.IsAdmin;

        // ── CATEGORY FILTER ─────────────────────────────────────
        private PostCategory _selectedCategory = PostCategory.All;
        public PostCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value) return;
                _selectedCategory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAllSelected));
                OnPropertyChanged(nameof(IsHotSelected));
                OnPropertyChanged(nameof(IsStudentSelected));
                OnPropertyChanged(nameof(IsAnnouncementSelected));
                _ = LoadByCategory();
            }
        }

        public bool IsAllSelected => SelectedCategory == PostCategory.All;
        public bool IsHotSelected => SelectedCategory == PostCategory.Hot;
        public bool IsStudentSelected => SelectedCategory == PostCategory.Student;
        public bool IsAnnouncementSelected => SelectedCategory == PostCategory.Announcement;

        public ICommand SelectAllCommand { get; }
        public ICommand SelectHotCommand { get; }
        public ICommand SelectStudentCommand { get; }
        public ICommand SelectAnnouncementCommand { get; }

        // ── DANH SÁCH ───────────────────────────────────────────
        public ObservableCollection<Post> Posts { get; set; } = new ObservableCollection<Post>();
        public ObservableCollection<Post> PendingPosts { get; set; } = new ObservableCollection<Post>();

        // ── LOADING ─────────────────────────────────────────────
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        private bool _isLoadingPending;
        public bool IsLoadingPending
        {
            get => _isLoadingPending;
            set { _isLoadingPending = value; OnPropertyChanged(); }
        }

        // ── TẠO BÀI ─────────────────────────────────────────────
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

        private string _selectedColor = "Transparent";
        public string SelectedColor
        {
            get => _selectedColor;
            set { _selectedColor = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FileAttachment> SelectedFiles { get; set; }
            = new ObservableCollection<FileAttachment>();

        // ── COMMANDS ────────────────────────────────────────────
        public ICommand PostCommand { get; }
        public ICommand DeletePostCommand { get; }
        public ICommand LikeCommand { get; }
        public ICommand OpenShareCommand { get; }
        public ICommand ViewFileCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand ApprovePostCommand { get; }
        public ICommand RejectPostCommand { get; }
        public ICommand AdminDeleteCommand { get; }

        // ── CONSTRUCTOR ─────────────────────────────────────────
        public ForumViewModel()
        {
            SelectedFiles.CollectionChanged += (s, e) => CommandManager.InvalidateRequerySuggested();

            SelectAllCommand = new RelayCommand(_ => SelectedCategory = PostCategory.All);
            SelectHotCommand = new RelayCommand(_ => SelectedCategory = PostCategory.Hot);
            SelectStudentCommand = new RelayCommand(_ => SelectedCategory = PostCategory.Student);
            SelectAnnouncementCommand = new RelayCommand(_ => SelectedCategory = PostCategory.Announcement);

            ViewFileCommand = new RelayCommand(obj =>
            {
                if (obj is FileAttachment file && !string.IsNullOrEmpty(file.FilePath))
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.FilePath) { UseShellExecute = true }); }
                    catch (Exception ex) { MessageBox.Show("Không thể mở file: " + ex.Message); }
            });

            RemoveFileCommand = new RelayCommand(obj =>
            {
                if (obj is FileAttachment file) SelectedFiles.Remove(file);
            });

            PostCommand = new RelayCommand(
                async obj => await ExecutePost(obj),
                obj => CanExecutePost());

            DeletePostCommand = new RelayCommand(ExecuteDeletePostWrapper);
            LikeCommand = new RelayCommand(async obj => await ExecuteLike(obj));

            OpenShareCommand = new RelayCommand(obj =>
            {
                if (obj is Post p) { _sharingPost = p; NewContent = "Đã chia sẻ một bài viết"; }
            });

            ApprovePostCommand = new RelayCommand(
                obj => ExecuteApprovePost(obj as Post),
                obj => IsAdmin && obj is Post);

            RejectPostCommand = new RelayCommand(
                obj => ExecuteRejectPost(obj as Post),
                obj => IsAdmin && obj is Post);

            AdminDeleteCommand = new RelayCommand(
                obj => ExecuteAdminDelete(obj as Post),
                obj => IsAdmin && obj is Post);

            _ = LoadByCategory();
            if (IsAdmin) _ = LoadPendingPostsAsync();
        }

        // ── LOAD THEO DANH MỤC ──────────────────────────────────
        public async Task LoadByCategory()
        {
            IsBusy = true;
            try
            {
                List<Post>? data = SelectedCategory switch
                {
                    PostCategory.All => await Task.Run(() => _forumBLL.GetApprovedPosts()),
                    PostCategory.Hot => await Task.Run(() => _forumBLL.GetHotPosts()),
                    PostCategory.Student => await Task.Run(() => _forumBLL.GetStudentPosts()),
                    PostCategory.Announcement => await Task.Run(() => _forumBLL.GetAnnouncementPosts()),
                    _ => await Task.Run(() => _forumBLL.GetApprovedPosts())
                };

                if (data == null) return;

                var dict = data.ToDictionary(p => p.IdPost, p => p);
                foreach (var item in data)
                    if (item.IdOriginalPost.HasValue && dict.TryGetValue(item.IdOriginalPost.Value, out var origin))
                        item.OriginalPost = origin;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Posts.Clear();
                    // HOT: DB đã sắp xếp theo điểm; còn lại sắp theo ngày
                    var sorted = SelectedCategory == PostCategory.Hot
                        ? data
                        : data.OrderByDescending(x => x.CreatedAt).ToList();
                    foreach (var p in sorted) Posts.Add(p);
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadByCategory lỗi: " + ex.Message); }
            finally { IsBusy = false; }
        }

        public async Task LoadDataAsync() => await LoadByCategory();

        public async Task LoadPendingPostsAsync()
        {
            if (!IsAdmin) return;
            IsLoadingPending = true;
            try
            {
                var data = await Task.Run(() => _forumBLL.GetPendingPosts());
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PendingPosts.Clear();
                    if (data != null) foreach (var p in data) PendingPosts.Add(p);
                });
            }
            finally { IsLoadingPending = false; }
        }

        // ── ĐĂNG BÀI ────────────────────────────────────────────
        private bool CanExecutePost() =>
            !IsBusy && (!string.IsNullOrWhiteSpace(NewContent) || SelectedFiles.Count > 0);

        public void AddFileToList(string path)
        {
            if (!SelectedFiles.Any(f => f.FilePath == path))
            {
                SelectedFiles.Add(new FileAttachment { FilePath = path, FileName = Path.GetFileName(path) });
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task ExecutePost(object? parameter)
        {
            if (SessionManager.CurrentAccount == null) return;
            IsBusy = true;
            try
            {
                var filePaths = SelectedFiles.Where(f => !string.IsNullOrEmpty(f.FilePath))
                                             .Select(f => f.FilePath!).ToList();

                bool success = await Task.Run(() =>
                    _forumBLL.CreatePost(SessionManager.CurrentAccount.IdAcc, "Thảo luận",
                                         NewContent, IsAnonymous, filePaths,
                                         _sharingPost?.IdPost, SelectedColor));
                if (success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        NewContent = string.Empty; IsAnonymous = false;
                        SelectedColor = "Transparent"; SelectedFiles.Clear();
                        _sharingPost = null; CloseAction?.Invoke();
                    });

                    await LoadByCategory();
                    if (IsAdmin)
                    {
                        // Admin đăng → duyệt ngay → load lại feed bình thường
                        await LoadPendingPostsAsync();
                    }
                    else
                    {
                        // ✅ Student đăng → thông báo chờ duyệt, KHÔNG hiển thị lên feed
                        MessageBox.Show(
                            "✅ Bài viết đã được gửi!\n\nBài của bạn đang chờ Admin duyệt và sẽ hiển thị sau.",
                            "Đăng bài thành công",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else MessageBox.Show("Đăng bài không thành công. Vui lòng thử lại.");
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
            finally { IsBusy = false; }
        }
        // ── LIKE ────────────────────────────────────────────────
        private async Task ExecuteLike(object? parameter)
        {
            if (parameter is Post post && SessionManager.CurrentAccount != null)
            {
                bool success = await Task.Run(() =>
                    _forumBLL.ToggleLike(SessionManager.CurrentAccount.IdAcc, post.IdPost));
                if (success)
                {
                    // Trigger TRG_UpdateLikeCount xử lý DB; ta chỉ cập nhật UI
                    post.IsLiked = !post.IsLiked;
                    post.Likes += post.IsLiked ? 1 : -1;
                }
            }
        }

        // ── XÓA BÀI (User) ──────────────────────────────────────
        private void ExecuteDeletePostWrapper(object? parameter)
        {
            if (parameter is not Post post) return;
            if (!post.IsMyPost && !IsAdmin)
            {
                MessageBox.Show("Bạn không có quyền xóa bài này!", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var r = MessageBox.Show("Bạn có chắc muốn xóa bài viết này?",
                                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
            {
                if (_forumBLL.RemovePost(post.IdPost)) Posts.Remove(post);
                else MessageBox.Show("Không thể xóa bài viết.");
            }
        }

        // ── ADMIN: DUYỆT / TỪ CHỐI / XÓA ───────────────────────
        private void ExecuteApprovePost(Post? post)
        {
            if (post == null || !IsAdmin) return;
            if (_forumBLL.UpdatePostStatus(post.IdPost, PostStatus.Approved))
            {
                post.ApprovalStatus = PostStatus.Approved;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PendingPosts.Remove(post);
                    if (SelectedCategory == PostCategory.All || SelectedCategory == PostCategory.Student)
                        Posts.Insert(0, post);
                });
                MessageBox.Show($"✅ Đã duyệt bài: \"{post.Title}\"");
            }
            else MessageBox.Show("❌ Không thể duyệt bài.");
        }

        private void ExecuteRejectPost(Post? post)
        {
            if (post == null || !IsAdmin) return;
            string reason = ShowRejectReasonDialog(post.Title) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reason)) return;
            if (_forumBLL.UpdatePostStatus(post.IdPost, PostStatus.Rejected, reason))
            {
                post.ApprovalStatus = PostStatus.Rejected;
                Application.Current.Dispatcher.Invoke(() => PendingPosts.Remove(post));
                MessageBox.Show($"🚫 Đã từ chối.\nLý do: {reason}");
            }
            else MessageBox.Show("❌ Không thể từ chối bài.");
        }

        private string? ShowRejectReasonDialog(string postTitle)
        {
            var dialog = new StudentReminderApp.Views.Dialogs.RejectReasonDialog(postTitle);
            return dialog.ShowDialog() == true ? dialog.Reason : null;
        }

        private void ExecuteAdminDelete(Post? post)
        {
            if (post == null || !IsAdmin) return;
            var confirm = MessageBox.Show(
                $"⚠️ Xóa bài \"{post.Title}\"?\nKhông thể hoàn tác!",
                "Xác nhận (Admin)", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            if (_forumBLL.AdminDeletePost(post.IdPost))
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Posts.Remove(post);
                    PendingPosts.Remove(post);
                });
            else MessageBox.Show("❌ Không thể xóa bài.");
        }
    }
}
