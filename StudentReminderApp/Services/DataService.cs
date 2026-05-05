using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

#pragma warning disable CS8618 // Ẩn cảnh báo Nullable để output Build sạch sẽ nhất

namespace StudentReminderApp.Services
{
    public static class DataService
    {
        private static Dictionary<string, ProgramData> _programs = new Dictionary<string, ProgramData>();
        private static bool _isLoaded = false;

        public static void Initialize()
        {
            if (_isLoaded) return;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] possiblePaths = {
                Path.Combine(baseDir, "data"),
                Path.Combine(baseDir, "..", "..", "..", "data"),
                Path.Combine(baseDir, "..", "..", "..", "Views", "Pages") // Fix: Đường dẫn lùi về thư mục gốc chính xác
            };

            var files = new Dictionary<string, string>
            {
                { "dacthu", "kctdt_dacthu.json" },
                { "nhat", "kctdt_nhat.json" },
                { "ai", "kctdt_trituenhantao.json" }
            };

            foreach (var kvp in files)
            {
                foreach (var path in possiblePaths)
                {
                    string fullPath = Path.Combine(path, kvp.Value);
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(fullPath);
                            var data = JsonConvert.DeserializeObject<ProgramData>(json);
                            if (data != null) _programs[kvp.Key] = data;
                            break;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DataService] Lỗi parse JSON file {kvp.Value}: {ex.Message}");
                        }
                    }
                }
            }

            // Fix: Chỉ đánh dấu là đã load nếu thực sự lấy được dữ liệu
            if (_programs.Count > 0)
            {
                _isLoaded = true;
            }
        }

        public static ProgramData? GetProgramData(string programId)
        {
            if (!_isLoaded) Initialize();
            return _programs.TryGetValue(programId, out var data) ? data : null;
        }

        public static List<CourseData> GetCoursesBySemester(string programId, int semester)
        {
            var program = GetProgramData(programId);
            var semData = program?.Semesters?.FirstOrDefault(s => s != null && s.Semester == semester);
            return semData?.Courses ?? new List<CourseData>();
        }
    }

    public class ProgramData
    {
        [JsonProperty("program_info")]
        public ProgramInfo? ProgramInfo { get; set; }
        [JsonProperty("semesters")]
        public List<SemesterData>? Semesters { get; set; }
    }

    public class ProgramInfo
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("major")]
        public string? Major { get; set; }
        [JsonProperty("total_credits")]
        public int TotalCredits { get; set; }
        [JsonProperty("required_credits")]
        public int RequiredCredits { get; set; }
        [JsonProperty("elective_credits")]
        public int ElectiveCredits { get; set; }
        [JsonProperty("duration")]
        public string? Duration { get; set; }
    }

    public class SemesterData
    {
        [JsonProperty("semester")]
        public int Semester { get; set; }
        [JsonProperty("courses")]
        public List<CourseData>? Courses { get; set; }
    }

    public class CourseData
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("credits")]
        public double Credits { get; set; }
        [JsonProperty("type")]
        public string? Type { get; set; }
        [JsonProperty("lecturer")]
        public string? Lecturer { get; set; }
        [JsonProperty("weeks")]
        public string? Weeks { get; set; }
        [JsonProperty("session")]
        public string? Session { get; set; }
    }
}