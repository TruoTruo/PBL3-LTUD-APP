namespace LTUDAPI.Models
{
    public class User
    {
        public string HoTen { get; set; } = string.Empty;
        public long IdAcc { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? Sdt { get; set; }
        public string? Email { get; set; }
    }
}