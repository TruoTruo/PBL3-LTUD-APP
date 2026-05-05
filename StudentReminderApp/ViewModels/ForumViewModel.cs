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
using StudentReminderApp.Views.Dialogs;

namespace StudentReminderApp.ViewModels
{
    // -------------------------------------------------------
    // FileAttachment — giữ nguyên từ bản cũ
    // -------------------------------------------------------
    public class FileAttachment : BaseViewModel
    {
        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
                UpdatePreviewImage();
            }
        }

        public string? FileName { get; set; }

        private BitmapImage? _previewImage;
        public BitmapImage? PreviewImage
        {
            get => _previewImage;
            set { _previewImage = value; OnPropertyChanged(); }
        }

        public string FileExtension => !string.IsNullOrEmpty(FilePath)
            ? Path.GetExtension(FilePath).ToUpper().Replace(".", "")
            : "";

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
                    bitmap.UriSource   = new Uri(FilePath, UriKind.Absolute);
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

    // -------------------------------------------------------
    // ForumViewModel — phiên bản đầy đủ với phân quyền Admin
    // -------------------------------------------------------
    public class ForumViewModel : BaseViewModel
    {
        private readonly ForumBLL _forumBLL = new ForumBLL();

        // -------------------------------------------------------
        // PHÂN QUYỀN ADMIN
        // -------------------------------------------------------

        /// <summary>
        /// True nếu người đang đăng nhập là Admin.
        /// Bind vào Visibility của tab "Duyệt bài" và nút Xóa toàn bộ.
        /// </summary>
        public bool IsAdmin => SessionManager.IsAdmin;

        // -------------------------------------------------------
        // DANH SÁCH BÀI VIẾT
        // -------------------------------------------------------

        /// <summary>Danh sách bài đã duyệt — hiển thị ở tab Diễn đàn.</summary>
        public ObservableCollection<Post> Posts { get; set; } = new ObservableCollection<Post>();

        /// <summary>Danh sách bài CHỜ DUYỆT — chỉ Admin thấy.</summary>
        public ObservableCollection<Post> PendingPosts { get; set; } = new ObservableCollection<Post>();

        // -------------------------------------------------------
        // TRẠNG THÁI LOADING
        // -------------------------------------------------------
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _isLoadingPending;
        public bool IsLoadingPending
        {
            get => _isLoadingPending;
            set { _isLoadingPending = value; OnPropertyChanged(); }
        }

        // -------------------------------------------------------
        // TẠO BÀI VIẾT MỚI
        // -------------------------------------------------------
        public Action? CloseAction { get; set; }

        private Post? _sharingPost;

        private string _newContent = string.Empty;
        public string NewContent
        {
            get => _newContent;
            set
            {
                _newContent = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
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

        // -------------------------------------------------------
        // COMMANDS — đăng bài, duyệt bài, từ chối, xóa
        // -------------------------------------------------------

        public ICommand PostCommand          { get; }
        public ICommand DeletePostCommand    { get; }
        public ICommand LikeCommand          { get; }
        public ICommand OpenShareCommand     { get; }
        public ICommand ViewFileCommand      { get; }
        public ICommand RemoveFileCommand    { get; }

        // ---- ADMIN COMMANDS ----
        /// <summary>Admin duyệt bài (approval_status → 1).</summary>
        public ICommand ApprovePostCommand  { get; }

        /// <summary>Admin từ chối bài (approval_status → 2) — hỏi lý do.</summary>
        public ICommand RejectPostCommand   { get; }

        /// <summary>Admin xóa bài bất kỳ.</summary>
        public ICommand AdminDeleteCommand  { get; }

        // -------------------------------------------------------
        // CONSTRUCTOR
        // -------------------------------------------------------
        public ForumViewModel()
        {
            SelectedFiles.CollectionChanged += (s, e) => CommandManager.InvalidateRequerySuggested();

            // --- View file ---
            ViewFileCommand = new RelayCommand(obj =>
            {
                if (obj is FileAttachment file && !string.IsNullOrEmpty(file.FilePath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.FilePath)
                        { UseShellExecute = true });
                    }
                    catch (Exception ex) { MessageBox.Show("Không thể mở file: " + ex.Message); }
                }
            });

            // --- Xóa file khỏi danh sách chọn ---
            RemoveFileCommand = new RelayCommand(obj =>
            {
                if (obj is FileAttachment file) SelectedFiles.Remove(file);
            });

            // --- Đăng bài (async) ---
            PostCommand = new RelayCommand(
                async obj => await ExecutePost(obj),
                obj => CanExecutePost()
            );

            // --- User xóa bài của mình ---
            DeletePostCommand = new RelayCommand(ExecuteDeletePostWrapper);

            // --- Like ---
            LikeCommand = new RelayCommand(async obj => await ExecuteLike(obj));

            // --- Chia sẻ ---
            OpenShareCommand = new RelayCommand(obj =>
            {
                if (obj is Post p)
                {
                    _sharingPost = p;
                    NewContent   = "Đã chia sẻ một bài viết";
                }
            });

            // =====================================================
            // ADMIN COMMANDS
            // =====================================================

            // Admin duyệt bài
            ApprovePostCommand = new RelayCommand(
                obj => ExecuteApprovePost(obj as Post),
                obj => IsAdmin && obj is Post
            );

            // Admin từ chối bài (yêu cầu nhập lý do)
            RejectPostCommand = new RelayCommand(
                obj => ExecuteRejectPost(obj as Post),
                obj => IsAdmin && obj is Post
            );

            // Admin xóa bài bất kỳ
            AdminDeleteCommand = new RelayCommand(
                obj => ExecuteAdminDelete(obj as Post),
                obj => IsAdmin && obj is Post
            );

            // Load dữ liệu khởi tạo
            _ = LoadDataAsync();
            if (IsAdmin)
                _ = LoadPendingPostsAsync();
        }

        // -------------------------------------------------------
        // LOAD DỮ LIỆU
        // -------------------------------------------------------

        public async Task LoadDataAsync()
        {
            try
            {
                var data = await Task.Run(() => _forumBLL.GetApprovedPosts());
                if (data == null) return;

                // Ghép bài share với bài gốc
                var postDict = data.ToDictionary(p => p.IdPost, p => p);
                foreach (var item in data)
                {
                    if (item.IdOriginalPost.HasValue &&
                        postDict.TryGetValue(item.IdOriginalPost.Value, out var origin))
                    {
                        item.OriginalPost = origin;
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Posts.Clear();
                    foreach (var p in data.OrderByDescending(x => x.CreatedAt))
                        Posts.Add(p);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi LoadDataAsync: " + ex.Message);
            }
        }

        /// <summary>Tải danh sách bài chờ duyệt — Admin only.</summary>
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
                    if (data != null)
                        foreach (var p in data) PendingPosts.Add(p);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi LoadPendingPostsAsync: " + ex.Message);
            }
            finally
            {
                IsLoadingPending = false;
            }
        }

        // -------------------------------------------------------
        // ĐĂNG BÀI
        // -------------------------------------------------------

        private bool CanExecutePost()
        {
            if (IsBusy) return false;
            return !string.IsNullOrWhiteSpace(NewContent) || SelectedFiles.Count > 0;
        }

        public void AddFileToList(string path)
        {
            if (!SelectedFiles.Any(f => f.FilePath == path))
            {
                SelectedFiles.Add(new FileAttachment
                {
                    FilePath = path,
                    FileName = Path.GetFileName(path)
                });
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task ExecutePost(object? parameter)
        {
            if (SessionManager.CurrentUser == null) return;

            IsBusy = true;
            try
            {
                long  currentUserId = SessionManager.CurrentUser.IdAcc;
                long? originalId    = _sharingPost?.IdPost;

                List<string> filePaths = SelectedFiles
                    .Where(f => !string.IsNullOrEmpty(f.FilePath))
                    .Select(f => f.FilePath!)
                    .ToList();

                bool success = await Task.Run(() =>
                    _forumBLL.CreatePost(currentUserId, "Thảo luận", NewContent,
                                         IsAnonymous, filePaths, originalId, SelectedColor));

                if (success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        NewContent    = string.Empty;
                        IsAnonymous   = false;
                        SelectedColor = "Transparent";
                        SelectedFiles.Clear();
                        _sharingPost  = null;
                        CloseAction?.Invoke();
                    });

                    await LoadDataAsync();
                    // Nếu Admin, cũng làm mới danh sách chờ duyệt
                    if (IsAdmin) await LoadPendingPostsAsync();
                }
                else
                {
                    MessageBox.Show("Đăng bài không thành công. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi đăng bài: " + ex.Message);
            }
            finally { IsBusy = false; }
        }

        // -------------------------------------------------------
        // LIKE
        // -------------------------------------------------------

        private async Task ExecuteLike(object? parameter)
        {
            if (parameter is Post post && SessionManager.CurrentUser != null)
            {
                long userId = SessionManager.CurrentUser.IdAcc;
                bool success = await Task.Run(() => _forumBLL.ToggleLike(userId, post.IdPost));
                if (success)
                {
                    // Trigger TRG_UpdateLikeCount đã cập nhật DB — ta chỉ cần đổi UI
                    post.IsLiked = !post.IsLiked;
                    post.Likes  += post.IsLiked ? 1 : -1;
                }
            }
        }

        // -------------------------------------------------------
        // XÓA BÀI (USER — chỉ xóa bài của mình)
        // -------------------------------------------------------

        private void ExecuteDeletePostWrapper(object? parameter)
        {
            if (parameter is not Post post) return;

            // Kiểm tra quyền sở hữu (User thường không thể xóa bài người khác)
            if (!post.IsMyPost && !IsAdmin)
            {
                MessageBox.Show("Bạn không có quyền xóa bài viết này!", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Bạn có chắc chắn muốn xóa bài viết này không?",
                                         "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) ExecuteDeletePost(post);
        }

        private void ExecuteDeletePost(Post post)
        {
            try
            {
                if (_forumBLL.RemovePost(post.IdPost))
                    Posts.Remove(post);
                else
                    MessageBox.Show("Không thể xóa bài viết này.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa bài viết: " + ex.Message);
            }
        }

        // -------------------------------------------------------
        // ADMIN: DUYỆT BÀI
        // -------------------------------------------------------

        private void ExecuteApprovePost(Post? post)
        {
            if (post == null || !IsAdmin) return;

            bool success = _forumBLL.UpdatePostStatus(post.IdPost, PostStatus.Approved);

            if (success)
            {
                // Cập nhật thuộc tính → OnPropertyChanged kéo UI cập nhật ngay
                post.ApprovalStatus = PostStatus.Approved;

                // Chuyển bài từ PendingPosts sang Posts
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PendingPosts.Remove(post);
                    Posts.Insert(0, post); // Thêm lên đầu danh sách diễn đàn
                });

                MessageBox.Show($"✅ Đã duyệt bài viết: \"{post.Title}\"",
                                "Duyệt bài thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("❌ Không thể duyệt bài. Vui lòng thử lại!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------------------------------------
        // ADMIN: TỪ CHỐI BÀI
        // -------------------------------------------------------

        private void ExecuteRejectPost(Post? post)
        {
            if (post == null || !IsAdmin) return;

            // Hỏi lý do từ chối qua InputBox tuỳ chỉnh
            // (Nếu dự án chưa có InputDialog, dùng MessageBox với lý do mặc định)
            string reason = ShowRejectReasonDialog(post.Title) ?? "Vi phạm nội quy diễn đàn.";

            if (string.IsNullOrWhiteSpace(reason))
            {
                // Người dùng nhấn Cancel
                return;
            }

            bool success = _forumBLL.UpdatePostStatus(post.IdPost, PostStatus.Rejected, reason);

            if (success)
            {
                post.ApprovalStatus = PostStatus.Rejected;
                post.RejectedReason = reason;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    PendingPosts.Remove(post);
                });

                MessageBox.Show($"🚫 Đã từ chối bài viết.\nLý do: {reason}",
                                "Từ chối bài", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("❌ Không thể từ chối bài. Vui lòng thử lại!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Dialog nhập lý do từ chối.
        /// Trả về null nếu Cancel, trả về chuỗi (có thể rỗng) nếu OK.
        /// </summary>
        private string? ShowRejectReasonDialog(string postTitle)
        {
            // Dùng Window nội tuyến đơn giản — bạn có thể thay bằng Dialog riêng
            var dialog = new RejectReasonDialog(postTitle);
            bool? result = dialog.ShowDialog();
            return result == true ? dialog.Reason : null;
        }

        // -------------------------------------------------------
        // ADMIN: XÓA BÀI BẤT KỲ
        // -------------------------------------------------------

        private void ExecuteAdminDelete(Post? post)
        {
            if (post == null || !IsAdmin) return;

            var confirm = MessageBox.Show(
                $"⚠️ Bạn đang dùng quyền Admin để xóa bài viết:\n\"{post.Title}\"\nHành động này không thể hoàn tác!",
                "Xác nhận xóa (Admin)", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            bool success = _forumBLL.AdminDeletePost(post.IdPost);

            if (success)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Xóa khỏi cả 2 danh sách
                    Posts.Remove(post);
                    PendingPosts.Remove(post);
                });

                MessageBox.Show("✅ Đã xóa bài viết thành công.", "Hoàn tất",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("❌ Không thể xóa bài. Vui lòng thử lại!", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
