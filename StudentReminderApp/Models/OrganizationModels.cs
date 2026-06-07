using System.Collections.Generic;

namespace StudentReminderApp.Models
{
    public class OrganizationData
    {
        public List<TruongData> Truongs { get; set; } = new();
    }

    public class TruongData
    {
        public string Name { get; set; }
        public List<KhoaData> Khoas { get; set; } = new();
        public List<string> Nhoms { get; set; } = new();
    }

    public class KhoaData
    {
        public string Name { get; set; }
        public List<NganhData> Nganhs { get; set; } = new();
    }

    public class NganhData
    {
        public string Name { get; set; }
        public List<LopData> Lops { get; set; } = new();
    }

    public class LopData
    {
        public string Name { get; set; }
        public List<string> Nhoms { get; set; } = new();
    }
}
