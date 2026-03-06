using System;
using System.ComponentModel.DataAnnotations;

namespace LTUDAPI.Models
{
    public class UserLog
    {
        [Key]
        public long IdLog { get; set; }
        public long IdAcc { get; set; }
        public string? HanhDong { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
        public string? IpAddress { get; set; }
    }
}