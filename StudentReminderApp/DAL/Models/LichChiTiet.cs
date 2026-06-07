using System;

namespace StudentReminderApp.Models
{
    public class ScheduleDetail
    {
        public long     IdSchedule     { get; set; } 
        public long     IdSectionClass { get; set; } 
        public string   SubjectName    { get; set; } 
        public string   LecturerName   { get; set; } 
        public string   RoomName       { get; set; } 
        public DateTime StudyDate      { get; set; } 
        
        public int      StartPeriod    { get; set; } 
        public int      EndPeriod      { get; set; } 
        public int      DayOfWeek      { get; set; } 
        public string   StudyMethod    { get; set; } 

        // Hàm hỗ trợ convert "Tiết học" sang "Thời gian thực" để vẽ lên Lịch
        public TimeSpan RealStartTime => ConvertPeriodToTime(StartPeriod, isStart: true);
        public TimeSpan RealEndTime => ConvertPeriodToTime(EndPeriod, isStart: false);

        private TimeSpan ConvertPeriodToTime(int period, bool isStart)
        {
            return (period, isStart) switch
            {
                // Buổi sáng
                (1, true) => new TimeSpan(7, 0, 0),   (1, false) => new TimeSpan(7, 50, 0),
                (2, true) => new TimeSpan(8, 0, 0),   (2, false) => new TimeSpan(8, 50, 0),
                (3, true) => new TimeSpan(9, 0, 0),   (3, false) => new TimeSpan(9, 50, 0),
                (4, true) => new TimeSpan(10, 0, 0),  (4, false) => new TimeSpan(10, 50, 0),
                (5, true) => new TimeSpan(11, 0, 0),  (5, false) => new TimeSpan(11, 50, 0),
                
                // Buổi chiều
                (6, true) => new TimeSpan(12, 30, 0), (6, false) => new TimeSpan(13, 20, 0),
                (7, true) => new TimeSpan(13, 30, 0), (7, false) => new TimeSpan(14, 20, 0),
                (8, true) => new TimeSpan(14, 30, 0), (8, false) => new TimeSpan(15, 20, 0),
                (9, true) => new TimeSpan(15, 30, 0), (9, false) => new TimeSpan(16, 20, 0),
                (10, true) => new TimeSpan(16, 30, 0), (10, false) => new TimeSpan(17, 20, 0),
                
                // Buổi tối
                (11, true) => new TimeSpan(17, 30, 0), (11, false) => new TimeSpan(18, 15, 0),
                (12, true) => new TimeSpan(18, 15, 0), (12, false) => new TimeSpan(19, 0, 0),
                (13, true) => new TimeSpan(19, 10, 0), (13, false) => new TimeSpan(19, 55, 0),
                (14, true) => new TimeSpan(19, 55, 0), (14, false) => new TimeSpan(20, 40, 0),
                
                // Mặc định trả về 0h nếu có lỗi dữ liệu
                _ => new TimeSpan(0, 0, 0) 
            };
        }
    }
}