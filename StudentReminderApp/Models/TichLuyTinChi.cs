// Models/TichLuyTinChi.cs
namespace StudentReminderApp.Models
{
    public class TichLuyTinChi
    {
        public long IdTichLuy { get; set; }
        public long IdAcc { get; set; }
        public long IdMonHoc { get; set; }
        public int HocKy { get; set; }
        public string NamHoc { get; set; }
        public decimal? DiemSo { get; set; }
        public string DiemChu { get; set; }
        public bool IsPassed { get; set; }

        // Navigation properties
        public MonHoc MonHoc { get; set; }
    }

    public class BangDiem
    {
        public string TenMonHoc { get; set; }
        public decimal SoTinChi { get; set; }
        public decimal? DiemSo { get; set; }
        public string DiemChu { get; set; }
        public string XepLoai { get; set; }
        public int HocKy { get; set; }
        public string NamHoc { get; set; }
    }
}