using System.Collections.Generic;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.Models
{
    public class CourseClass
    {
        public string classCode { get; set; }
        public string courseId { get; set; }
        public string CourseName { get; set; }
        public string LecturerName { get; set; }
        public string group { get; set; }
        public string year { get; set; }
        public string semester { get; set; }
        public bool isOverriding { get; set; }
        public List<Session> sessions { get; set; } = new List<Session>();

        public void ParseClassCode(string fullCode)
        {
            if (string.IsNullOrEmpty(fullCode)) return;
            
            classCode = fullCode;
            int length = fullCode.Length;
            
            int groupLength = 2;
            if (length > 0 && char.IsLetter(fullCode[length - 1]))
            {
                groupLength = 3;
            }
            
            if (length >= groupLength)
            {
                group = fullCode.Substring(length - groupLength);
                length -= groupLength;
            }
            if (length >= 2)
            {
                year = fullCode.Substring(length - 2, 2);
                length -= 2;
            }
            if (length >= 4)
            {
                semester = fullCode.Substring(length - 4, 4);
                length -= 4;
            }
            if (length > 0)
            {
                courseId = fullCode.Substring(0, length);
            }
        }
    }
}