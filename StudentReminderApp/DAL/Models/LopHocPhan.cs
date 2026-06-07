namespace StudentReminderApp.Models
{
    public class LopHocPhan
    {
        public long IdLopHp { get; set; }
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public int SoTinChi { get; set; }
        public string TenGiangVien { get; set; }
        public string TenPhong { get; set; }
        public int ThuTrongTuan { get; set; }
        public int TietBatDau { get; set; }
        public int TietKetThuc { get; set; }
        public string TrangThaiText { get; set; }
        public bool DaDangKy { get; set; }

        public string MaMonHocDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(MaMonHoc) && MaMonHoc.Length >= 2)
                {
                    return $"10224.{MaMonHoc.Substring(MaMonHoc.Length - 2)}";
                }
                return MaMonHoc;
            }
        }
    }
}