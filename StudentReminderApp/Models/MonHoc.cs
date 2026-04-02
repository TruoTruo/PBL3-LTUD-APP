// Models/MonHoc.cs
using System.Collections.Generic;

namespace StudentReminderApp.Models
{
    public class MonHoc
    {
        public long IdMonHoc { get; set; }
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public decimal SoTinChi { get; set; }
        public int HocKyKhuyenNghi { get; set; }
        public string LoaiHp { get; set; }
        public bool IsActive { get; set; }
    }

    public class StudentSummary
    {
        public int TotalAccumulatedCredits { get; set; }
        public int RegisteredCreditsThisTerm { get; set; }
        public int MaxCreditsAllowed { get; set; } = 22;
        public int RemainingCredits => MaxCreditsAllowed - RegisteredCreditsThisTerm;
        public double GPA { get; set; }
        public string GPAFormatted { get; set; }
        public string GPALevel { get; set; }
        public double CompletionPercentage { get; set; }
    }

    public class HocPhanDaHoc
    {
        public long IdMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public decimal SoTinChi { get; set; }
        public double Diem { get; set; }
        public int HocKyHoanThanh { get; set; }
        public string NamHoc { get; set; }
    }

    public class PrerequisiteInfo
    {
        public List<long> TienQuyet { get; set; } = new List<long>();   // Phải học xong
        public List<long> HocTruoc { get; set; } = new List<long>();    // Có thể đang học
        public List<long> SongHanh { get; set; } = new List<long>();    // Học cùng kỳ
    }
}