using System;

namespace StudentReminderApp.Models
{
    public class LichChiTiet
    {
        public long     IdLich       { get; set; }
        public long     IdLopHp      { get; set; }
        public string   TenMonHoc    { get; set; }
        public string   TenGiangVien { get; set; }
        public string   TenPhong     { get; set; }
        public DateTime NgayHoc      { get; set; }
        public int      StartTime    { get; set; }
        public int      EndTime      { get; set; }
        public int      ThuTrongTuan { get; set; }
        public string   HinhThuc     { get; set; }
    }

    public class NotificationQueue
    {
        public long     IdQueue     { get; set; }
        public long     IdAcc       { get; set; }
        public string   Title       { get; set; }
        public string   Content     { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string   Status      { get; set; }
        public long?    IdBuoiHoc   { get; set; }
        public long?    IdEvent     { get; set; }
    }
}
