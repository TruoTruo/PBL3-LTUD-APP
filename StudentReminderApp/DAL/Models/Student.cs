using System;
using StudentReminderApp.ViewModels;

namespace StudentReminderApp.Models
{
    public class StudentModel : BaseViewModel
    {
        public long     IdAcc    { get; set; }
        public string   Mssv     { get; set; } = string.Empty;
        public string   HoTen    { get; set; } = string.Empty;
        public string   Email    { get; set; } = string.Empty;
        public string   Sdt      { get; set; } = string.Empty;
        public string   NienKhoa { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // ── LỚP ──────────────────────────────────────────────────
        private string _tenLop = string.Empty;
        public string TenLop
        {
            get => _tenLop;
            set { _tenLop = value; OnPropertyChanged(); }
        }

        private long? _idLop;
        public long? IdLop
        {
            get => _idLop;
            set { _idLop = value; OnPropertyChanged(); }
        }

        // ── STATUS ───────────────────────────────────────────────
        private string _status = "Active";
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBanned));
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(LockUntilDisplay));
            }
        }

        // ── LOCK_UNTIL ────────────────────────────────────────────
        private DateTime? _lockUntil;
        public DateTime? LockUntil
        {
            get => _lockUntil;
            set
            {
                _lockUntil = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LockUntilDisplay));
            }
        }

        // ── IS_VERIFIED ───────────────────────────────────────────
        private bool _isVerified;
        public bool IsVerified
        {
            get => _isVerified;
            set
            {
                _isVerified = value;
                OnPropertyChanged();
                // Notify tất cả computed phụ thuộc
                OnPropertyChanged(nameof(VerifiedIcon));
                OnPropertyChanged(nameof(VerifiedDisplay));
                OnPropertyChanged(nameof(VerifiedColor));
                OnPropertyChanged(nameof(VerifiedBg));
                OnPropertyChanged(nameof(CanVerify));
            }
        }

        // ── COMPUTED ──────────────────────────────────────────────
        public bool   IsBanned        => Status == "Banned";
        public bool   CanVerify       => !IsVerified;
        public string StatusDisplay   => IsBanned ? "Bị khóa" : "Hoạt động";
        public string StatusColor     => IsBanned ? "#F44336" : "#4CAF50";
        public string VerifiedDisplay => IsVerified ? "Đã xác thực" : "Chưa xác thực";

        /// <summary>
        /// Dùng ký tự Unicode thay vì emoji để DataGrid render đúng và reactive.
        /// ✓ (U+2713) màu xanh khi verified, ✗ (U+2717) màu đỏ khi chưa.
        /// </summary>
        public string VerifiedIcon  => IsVerified ? "✓" : "✗";
        public string VerifiedColor => IsVerified ? "#16A34A" : "#DC2626";
        public string VerifiedBg    => IsVerified ? "#DCFCE7" : "#FEE2E2";

        public string LockUntilDisplay
        {
            get
            {
                if (!IsBanned) return string.Empty;
                return _lockUntil.HasValue
                    ? $"đến {_lockUntil:dd/MM/yyyy HH:mm}"
                    : "Vĩnh viễn";
            }
        }
    }
}
