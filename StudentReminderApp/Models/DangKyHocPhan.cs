// Models/DangKyHocPhan.cs
using System;

namespace StudentReminderApp.Models
{
    public class DangKyHocPhan
    {
        public long IdDk { get; set; }
        public long IdAcc { get; set; }
        public long IdLopHp { get; set; }
        public DateTime NgayDangKy { get; set; }
        public string TrangThai { get; set; }
        public decimal? DiemThi { get; set; }
        public string DiemChu { get; set; }

        // Navigation properties
        public LopHocPhan LopHocPhan { get; set; }
        public MonHoc MonHoc { get; set; }
    }

    public class DangKyChiTiet
    {
        public long IdDk { get; set; }
        public long IdLopHp { get; set; }
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public decimal SoTinChi { get; set; }
        public string TenGiangVien { get; set; }
        public string TenPhong { get; set; }
        public int HocKy { get; set; }
        public string NamHoc { get; set; }
        public string TrangThai { get; set; }
        public decimal? DiemThi { get; set; }
        public DateTime NgayDangKy { get; set; }
    }
}