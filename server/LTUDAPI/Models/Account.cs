using System;
using System.ComponentModel.DataAnnotations;

namespace LTUDAPI.Models
{
    public class Account
    {
        [Key]
        public long IdAcc { get; set; }

        public string Username { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public long? IdRole { get; set; }

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Quan hệ 1-1 với bảng User
        public virtual User? User { get; set; }
    }
}