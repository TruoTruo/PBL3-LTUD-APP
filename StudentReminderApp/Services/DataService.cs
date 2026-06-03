using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

                string current = root;
                for (int i = 0; i < 6; i++)
                {
                    current = Path.GetFullPath(Path.Combine(current, ".."));
                    searchPaths.Add(current);
                    searchPaths.Add(Path.Combine(current, "data"));
                }
            }

            // Danh sách các file CSV theo từng chuyên ngành
            var files = new Dictionary<string, string>
            {
                { "nhat", "CNTTNHATCN.csv" },
                { "dacthu", "CNTTDACTHUCN.csv" },
                { "ai", "CNTTAICN.csv" }
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
                            var data = ParseCsv(fullPath, kvp.Key);
                            if (data != null) 
                            {
                                _programs[kvp.Key] = data;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DataService] Lỗi parse CSV file {kvp.Value}: {ex.Message}");
                        }
                    }
                }
            }

            if (_programs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[DataService] Không tìm thấy file CSV nào trong các đường dẫn:");
                foreach (var path in searchPaths.Distinct())
                {
                    System.Diagnostics.Debug.WriteLine($"  {Path.GetFullPath(path)}");
                }
            }
            else
            {
                _isLoaded = true;
            }
        }

        private static ProgramData? ParseCsv(string filePath, string programId)
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length <= 2) return null;

            var programData = new ProgramData
            {
                ProgramInfo = new ProgramInfo { Id = programId, Name = Path.GetFileNameWithoutExtension(filePath) },
                Semesters = new List<SemesterData>()
            };

            CourseData? currentCourse = null;

            // Dòng 0 và 1 là header, bắt đầu đọc từ dòng 2
            for (int i = 2; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');

                // Nếu cột TT (parts[0]) có dữ liệu -> Bắt đầu môn học mới
                if (!string.IsNullOrWhiteSpace(parts[0]))
                {
                    if (int.TryParse(parts[1], out int semesterNum))
                    {
                        var semester = programData.Semesters.FirstOrDefault(s => s.Semester == semesterNum);
                        if (semester == null)
                        {
                            semester = new SemesterData { Semester = semesterNum, Courses = new List<CourseData>() };
                            programData.Semesters.Add(semester);
                        }

                        currentCourse = new CourseData
                        {
                            Id = parts.Length > 4 ? parts[4].Trim() : "",
                            Name = parts.Length > 2 ? parts[2].Trim() : "",
                            Symbol = parts.Length > 3 ? parts[3].Trim() : "",
                            Credits = parts.Length > 5 && double.TryParse(parts[5], out double c) ? c : 0,
                            Optional = parts.Length > 6 ? parts[6].Trim() : "",
                            HtDa = parts.Length > 7 ? parts[7].Trim() : "",
                            TqDa = parts.Length > 8 ? parts[8].Trim() : "",
                            Relation = parts.Length > 9 ? parts[9].Trim() : "",
                            Corequisite = parts.Length > 10 ? parts[10].Trim() : "",
                            Prerequisite = parts.Length > 11 ? parts[11].Trim() : "",
                        };
                        semester.Courses!.Add(currentCourse);
                    }
                }
                // Nếu cột TT trống -> Dòng tiếp nối các môn tiên quyết/song hành của môn học hiện tại
                else if (currentCourse != null)
                {
                    if (parts.Length > 7 && !string.IsNullOrWhiteSpace(parts[7]) && (currentCourse.HtDa == null || !currentCourse.HtDa.Contains(parts[7].Trim())))
                        currentCourse.HtDa += (string.IsNullOrEmpty(currentCourse.HtDa) ? "" : " ") + parts[7].Trim();
                    if (parts.Length > 8 && !string.IsNullOrWhiteSpace(parts[8]) && (currentCourse.TqDa == null || !currentCourse.TqDa.Contains(parts[8].Trim())))
                        currentCourse.TqDa += (string.IsNullOrEmpty(currentCourse.TqDa) ? "" : " ") + parts[8].Trim();
                    
                    if (parts.Length > 9 && !string.IsNullOrWhiteSpace(parts[9]))
                        currentCourse.Relation += (string.IsNullOrEmpty(currentCourse.Relation) ? "" : "\n") + parts[9].Trim();
                    
                    if (parts.Length > 10 && !string.IsNullOrWhiteSpace(parts[10]))
                        currentCourse.Corequisite += (string.IsNullOrEmpty(currentCourse.Corequisite) ? "" : "\n") + parts[10].Trim();
                    
                    if (parts.Length > 11 && !string.IsNullOrWhiteSpace(parts[11]))
                        currentCourse.Prerequisite += (string.IsNullOrEmpty(currentCourse.Prerequisite) ? "" : "\n") + parts[11].Trim();
                }
            }

            return programData;
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
        public ProgramInfo? ProgramInfo { get; set; }
        public List<SemesterData>? Semesters { get; set; }
    }

    public class ProgramInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Major { get; set; }
        public double TotalCredits { get; set; }
        public double RequiredCredits { get; set; }
        public double ElectiveCredits { get; set; }
        public string? Duration { get; set; }
    }

    public class SemesterData
    {
        public int Semester { get; set; }
        public List<CourseData>? Courses { get; set; }
    }

    public class CourseData
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }
        public double Credits { get; set; }
        public string? Optional { get; set; }
        public string? HtDa { get; set; }
        public string? TqDa { get; set; }
        public string? Relation { get; set; }
        public string? Prerequisite { get; set; }
        public string? Corequisite { get; set; }
        public string? Lecturer { get; set; }
        public string? Weeks { get; set; }
        public string? Session { get; set; }
        public string? Status { get; set; }
    }
}