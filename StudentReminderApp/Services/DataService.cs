using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            string appBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase ?? baseDir;
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? baseDir;
            string currentDir = Environment.CurrentDirectory;

            var startPaths = new[] { baseDir, appBase, assemblyDir, currentDir }
                              .Where(p => !string.IsNullOrWhiteSpace(p))
                              .Select(Path.GetFullPath)
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToList();

            var searchPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in startPaths)
            {
                searchPaths.Add(root);
                searchPaths.Add(Path.Combine(root, "data"));
                searchPaths.Add(Path.Combine(root, "Views", "Pages"));
                searchPaths.Add(Path.Combine(root, "..", "Views", "Pages"));
                searchPaths.Add(Path.Combine(root, "..", "..", "Views", "Pages"));
                searchPaths.Add(Path.Combine(root, "..", "..", "..", "Views", "Pages"));
                searchPaths.Add(Path.Combine(root, "..", "..", "..", "..", "Views", "Pages"));

                string current = root;
                for (int i = 0; i < 6; i++)
                {
                    current = Path.GetFullPath(Path.Combine(current, ".."));
                    searchPaths.Add(Path.Combine(current, "Views", "Pages"));
                    searchPaths.Add(Path.Combine(current, "data"));
                }
            }

            var files = new Dictionary<string, string>
            {
                { "dacthu", "kctdt_dacthu.json" },
                { "nhat", "kctdt_nhat.json" },
                { "ai", "kctdt_trituenhantao.json" }
            };

            foreach (var kvp in files)
            {
                foreach (var path in searchPaths)
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

            if (_programs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[DataService] Không tìm thấy file JSON nào trong các đường dẫn:");
                foreach (var path in searchPaths.Distinct())
                {
                    System.Diagnostics.Debug.WriteLine($"  {Path.GetFullPath(path)}");
                }
            }

            // Chỉ đánh dấu là đã load nếu thực sự lấy được dữ liệu
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
        public double TotalCredits { get; set; }
        [JsonProperty("required_credits")]
        public double RequiredCredits { get; set; }
        [JsonProperty("elective_credits")]
        public double ElectiveCredits { get; set; }
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
        [JsonProperty("symbol")]
        public string? Symbol { get; set; }
        [JsonProperty("credits")]
        public double Credits { get; set; }
        [JsonProperty("optional")]
        public string? Optional { get; set; }
        [JsonProperty("ht_da")]
        public string? HtDa { get; set; }
        [JsonProperty("tq_da")]
        public string? TqDa { get; set; }
        [JsonProperty("relation")]
        public string? Relation { get; set; }
        [JsonProperty("prerequisite")]
        public string? Prerequisite { get; set; }
        [JsonProperty("corequisite")]
        public string? Corequisite { get; set; }
        [JsonProperty("lecturer")]
        public string? Lecturer { get; set; }
        [JsonProperty("weeks")]
        public string? Weeks { get; set; }
        [JsonProperty("session")]
        public string? Session { get; set; }
        [JsonProperty("status")]
        public string? Status { get; set; }
    }
}