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
                    bitmap.UriSource = new Uri(FilePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; 
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage = bitmap;
                }
                catch
                {
                    PreviewImage = null;
                }
            }
            else
            {
                PreviewImage = null;
            }
        }
    }

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

        public ObservableCollection<FileAttachment> SelectedFiles { get; set; } = new ObservableCollection<FileAttachment>();

        public ICommand RemoveFileCommand { get; }
        public ICommand PostCommand { get; }
        public ICommand DeletePostCommand { get; set; }
        public ICommand LikeCommand { get; }
        public ICommand OpenShareCommand { get; }
        public ICommand ViewFileCommand { get; }

        public ForumViewModel()
        {
            Posts = new ObservableCollection<Post>();
            SelectedFiles.CollectionChanged += (s, e) => CommandManager.InvalidateRequerySuggested();

            ViewFileCommand = new RelayCommand((obj) =>
            {
                if (obj is FileAttachment file && !string.IsNullOrEmpty(file.FilePath))
                {
                    try 
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.FilePath) 
                        { 
                            UseShellExecute = true 
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể mở file: " + ex.Message);
                    }
                }
            });

            RemoveFileCommand = new RelayCommand((obj) =>
            {
                if (obj is FileAttachment file) SelectedFiles.Remove(file);
            });

            PostCommand = new RelayCommand(
                async (obj) => await ExecutePost(obj),
                (obj) => CanExecutePost()
            );

            DeletePostCommand = new RelayCommand(ExecuteDeletePostWrapper);
            LikeCommand = new RelayCommand(async (obj) => await ExecuteLike(obj));

            OpenShareCommand = new RelayCommand((obj) =>
            {
                if (obj is Post p)
                {
                    _sharingPost = p;
                    NewContent = "Đã chia sẻ một bài viết";
                }
            });

            _ = LoadDataAsync();
        }

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
                        var sortedPosts = data.OrderByDescending(x => x.CreatedAt).ToList();
                        foreach (var p in sortedPosts)
                        {
                            Posts.Add(p);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi nạp dữ liệu: " + ex.Message);
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

                List<string> filePaths = SelectedFiles
                                         .Where(f => !string.IsNullOrEmpty(f.FilePath))
                                         .Select(f => f.FilePath!)
                                         .ToList();

                bool success = await Task.Run(() =>
                    _forumBLL.CreatePost(
                        currentUserId, 
                        "Thảo luận", 
                        NewContent, 
                        IsAnonymous, 
                        filePaths, 
                        originalId, 
                        SelectedColor
                    )
                );

                if (success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        NewContent = string.Empty;
                        IsAnonymous = false;
                        SelectedColor = "Transparent";
                        SelectedFiles.Clear();
                        _sharingPost = null;
                        CloseAction?.Invoke();
                    });

                    await LoadDataAsync();
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
            finally 
            { 
                IsBusy = false; 
            }
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
                if (_forumBLL.RemovePost(post.IdPost)) 
                {
                    Posts.Remove(post);
                }
                else 
                {
                    MessageBox.Show("Không thể xóa bài viết này.");
                }
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Lỗi khi xóa bài viết: " + ex.Message); 
            }
        }
    }
}