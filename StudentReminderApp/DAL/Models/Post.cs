using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using StudentReminderApp.Helpers;
using StudentReminderApp.ViewModels;

namespace StudentReminderApp.Models
{
    /// <summary>
    /// Hằng số trạng thái duyệt bài — dùng chung toàn dự án.
    /// </summary>
    public static class PostStatus
    {
        public const int Pending = 0; // Chờ duyệt
        public const int Approved = 1; // Đã duyệt
        public const int Rejected = 2; // Bị từ chối
    }

    public class Post : BaseViewModel
    {
        public long IdPost { get; set; }
        public long IdAcc { get; set; }
        public long? IdOriginalPost { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsAnonymous { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? RejectedReason { get; set; }

        // -------------------------------------------------------
        // TRẠNG THÁI DUYỆT BÀI — REACTIVE (gọi OnPropertyChanged)
        // -------------------------------------------------------
        private int _approvalStatus = PostStatus.Approved;
        /// <summary>0=Chờ duyệt | 1=Đã duyệt | 2=Từ chối</summary>
        public int ApprovalStatus
        {
            get => _approvalStatus;
            set
            {
                if (_approvalStatus == value) return;
                _approvalStatus = value;
                OnPropertyChanged();
                // Kéo theo các computed props phụ thuộc vào ApprovalStatus
                OnPropertyChanged(nameof(StatusBadge));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(IsPending));
                OnPropertyChanged(nameof(IsApproved));
                OnPropertyChanged(nameof(IsRejected));
            }
        }

        // -------------------------------------------------------
        // COMPUTED: derived từ ApprovalStatus — dùng trong XAML
        // -------------------------------------------------------
        public bool IsPending => ApprovalStatus == PostStatus.Pending;
        public bool IsApproved => ApprovalStatus == PostStatus.Approved;
        public bool IsRejected => ApprovalStatus == PostStatus.Rejected;

        public string StatusBadge => ApprovalStatus switch
        {
            PostStatus.Pending => "⏳ Chờ duyệt",
            PostStatus.Approved => "✅ Đã duyệt",
            PostStatus.Rejected => "❌ Từ chối",
            _ => "Không rõ"
        };

        public string StatusColor => ApprovalStatus switch
        {
            PostStatus.Pending => "#FF9800",
            PostStatus.Approved => "#4CAF50",
            PostStatus.Rejected => "#F44336",
            _ => "#9E9E9E"
        };

        // -------------------------------------------------------
        // LIKES / COMMENTS / SHARES — REACTIVE
        // -------------------------------------------------------
        private int _likes;
        public int Likes
        {
            get => _likes;
            set { _likes = value; OnPropertyChanged(); }
        }

        private bool _isLiked;
        public bool IsLiked
        {
            get => _isLiked;
            set { _isLiked = value; OnPropertyChanged(); }
        }

        private int _commentCount;
        public int CommentCount
        {
            get => _commentCount;
            set { _commentCount = value; OnPropertyChanged(); }
        }

        private int _shareCount;
        public int ShareCount
        {
            get => _shareCount;
            set { _shareCount = value; OnPropertyChanged(); }
        }

        // -------------------------------------------------------
        // FILES / IMAGES
        // -------------------------------------------------------
        private List<string> _filePaths = new List<string>();
        public List<string> FilePaths
        {
            get => _filePaths ?? new List<string>();
            set
            {
                _filePaths = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImagePaths));
            }
        }

        public List<string> ImagePaths
        {
            get
            {
                if (FilePaths == null) return new List<string>();
                string[] imgExtensions = { ".jpg", ".png", ".jpeg", ".bmp", ".gif" };
                return FilePaths.Where(path =>
                    !string.IsNullOrEmpty(path) &&
                    imgExtensions.Contains(Path.GetExtension(path).ToLower()))
                    .ToList();
            }
        }

        // -------------------------------------------------------
        // GIAO DIỆN / MÀU SẮC
        // -------------------------------------------------------
        private string _backgroundColor = "Transparent";
        public string BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsColored));
                OnPropertyChanged(nameof(PostForeground));
            }
        }

        public bool IsColored => !string.IsNullOrEmpty(BackgroundColor) &&
                                 BackgroundColor != "Transparent" &&
                                 BackgroundColor.ToLower() != "#ffffffff";

        public string PostForeground => IsColored ? "White" : "#050505";
        public bool IsShared => IdOriginalPost.HasValue && IdOriginalPost.Value > 0;

        private Post? _originalPost;
        public Post? OriginalPost
        {
            get => _originalPost;
            set
            {
                _originalPost = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsShared));
            }
        }

        private bool _isPublic = true;
        public bool IsPublic
        {
            get => _isPublic;
            set { _isPublic = value; OnPropertyChanged(); OnPropertyChanged(nameof(PrivacyIcon)); }
        }

        public string PrivacyIcon => IsPublic ? "🌎" : "🔒";

        // -------------------------------------------------------
        // QUYỀN XÓA BÀI — tích hợp phân quyền Admin
        // -------------------------------------------------------
        /// <summary>
        /// True nếu người đăng nhập là Admin — Admin thấy nút Xóa trên mọi bài.
        /// True nếu bài viết thuộc về người đang đăng nhập.
        /// Binding trực tiếp trong XAML: Visibility="{Binding CanDelete, Converter=...}"
        /// </summary>
        public bool CanDelete
        {
            get
            {
                if (SessionManager.CurrentAccount == null) return false;
                if (SessionManager.IsAdmin) return true;
                return IdAcc == SessionManager.CurrentAccount.IdAcc;
            }
        }

        /// <summary>Bài của người đang đăng nhập (dùng để hiện menu tuỳ chọn).</summary>
        public bool IsMyPost => SessionManager.CurrentUser != null && IdAcc == SessionManager.CurrentUser.IdAcc;

        public string DisplayName => IsAnonymous
            ? "Người dùng ẩn danh"
            : (string.IsNullOrWhiteSpace(AuthorName) ? "Thành viên" : AuthorName);

        public string DisplayAvatar
        {
            get
            {
                string defaultImg = "pack://application:,,,/Resources/Images/user.png";
                if (IsAnonymous || string.IsNullOrWhiteSpace(AuthorAvatar)) return defaultImg;
                return AuthorAvatar;
            }
        }

        public string TimeAgo
        {
            get
            {
                TimeSpan span = DateTime.Now - CreatedAt;
                if (span.TotalMinutes < 1) return "Vừa xong";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
                return CreatedAt.ToString("dd/MM/yyyy");
            }
        }
    }
}
