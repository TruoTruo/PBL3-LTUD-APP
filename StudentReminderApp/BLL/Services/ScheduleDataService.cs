using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.Services
{
    public class ScheduleDataService
    {
        public List<List<CourseClass>> ParseScheduleCsv(string filePath)
        {
            var courses = new Dictionary<string, CourseClass>();
            var records = ParseCsvFile(filePath);

            for (int i = 1; i < records.Count; i++)
            {
                var parts = records[i];
                if (parts.Length <= 19) continue;

                string classCode = parts[3].Trim();
                if (string.IsNullOrWhiteSpace(classCode)) continue;

                if (!courses.ContainsKey(classCode))
                {
                    var newClass = new CourseClass();
                    newClass.ParseClassCode(classCode);
                    courses[classCode] = newClass;
                }

                var courseClass = courses[classCode];

                for (int dayCol = 14; dayCol <= 19; dayCol++)
                {
                    string dayData = parts[dayCol].Trim();
                    if (!string.IsNullOrWhiteSpace(dayData))
                    {
                        var dayParts = dayData.Split(',');
                        string lessonPart = dayParts[0].Trim();
                        string roomPart = dayParts.Length > 1 ? dayParts[1].Trim() : string.Empty;
                        
                        int dayOfWeek = dayCol - 12;
                        var session = new Session { dayOfWeek = dayOfWeek, lessons = new List<int>(), room = roomPart };

                        var lessonParts = lessonPart.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lessonParts.Length == 2)
                        {
                            if (int.TryParse(lessonParts[0], out int start) && int.TryParse(lessonParts[1], out int end))
                            {
                                for (int l = start; l <= end; l++) session.lessons.Add(l);
                            }
                        }
                        else
                        {
                            if (int.TryParse(lessonPart, out int l)) session.lessons.Add(l);
                        }

                        if (session.lessons.Count > 0)
                        {
                            courseClass.sessions.Add(session);
                        }
                    }
                }
            }

            return courses.Values.GroupBy(c => c.courseId).Select(g => g.ToList()).ToList();
        }

        private List<string[]> ParseCsvFile(string filePath)
        {
            var records = new List<string[]>();
            var currentRecord = new List<string>();
            var currentField = new System.Text.StringBuilder();
            bool inQuotes = false;

            string text = File.ReadAllText(filePath);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '\"')
                    {
                        currentField.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if ((c == '\r' || c == '\n') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();
                    if (currentRecord.Count > 1 || !string.IsNullOrWhiteSpace(currentRecord[0]))
                    {
                        records.Add(currentRecord.ToArray());
                    }
                    currentRecord.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            if (currentField.Length > 0 || currentRecord.Count > 0)
            {
                currentRecord.Add(currentField.ToString());
                records.Add(currentRecord.ToArray());
            }
            return records;
        }

        public List<List<CourseClass>> GetTopSchedulesFromCsv(string filePath)
        {
            var buckets = ParseScheduleCsv(filePath);
            var optimizer = new ScheduleOptimizerService();
            return optimizer.GetOptimizedScheduleOptions(buckets);
        }
    }
}