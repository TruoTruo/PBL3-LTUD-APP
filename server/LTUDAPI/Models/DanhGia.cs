namespace LTUDAPI.Models
{
    public class DanhGia
    {
        public long IdDanhGia { get; set; }
        public long IdAcc { get; set; }
        public long IdGiangVien { get; set; }
        public int SoSao { get; set; }
        public string? NoiDung { get; set; }
        public bool IsAnonymous { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}