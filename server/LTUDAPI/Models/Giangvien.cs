namespace LTUDAPI.Models
{
    public class GiangVien
    {
        public long IdGiangVien { get; set; }
        public string TenGiangVien { get; set; } = null!;
        public string? Khoa { get; set; }
        public string? Email { get; set; }
    }
}