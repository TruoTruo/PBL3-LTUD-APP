namespace StudentReminderApp.Models
{
    public class LopHocPhan
    {
        public long   IdLopHp      { get; set; }
        public string MaLopHp      { get; set; }
        public long   IdMonHoc     { get; set; }
        public string TenMonHoc    { get; set; }
        public string MaMonHoc     { get; set; }
        public int    SoTinChi     { get; set; }
        public long   IdGiangVien  { get; set; }
        public string TenGiangVien { get; set; }
        public long   IdPhong      { get; set; }
        public string TenPhong     { get; set; }
        public int    HocKy        { get; set; }
        public string NamHoc       { get; set; }
        public bool   DaDangKy     { get; set; }
    }
}
