using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;
using StudentReminderApp.Services;

namespace StudentReminderApp.Views.Dialogs
{
    public class CellItem
    {
        public string Text { get; set; } = string.Empty;
        public string TopText { get; set; } = string.Empty;
        public string BottomText { get; set; } = string.Empty;
        public Brush Background { get; set; } = Brushes.Transparent;
        public Brush BorderBrush { get; set; } = Brushes.Transparent;
        public Thickness BorderThickness { get; set; } = new Thickness(0);
        public Brush Foreground { get; set; } = Brushes.Black;
    }

    public class ScheduleOption
    {
        public string OptionId { get; set; } = string.Empty;
        public double Score { get; set; }
        public List<StudentReminderApp.Models.CourseClass> Classes { get; set; } = new List<StudentReminderApp.Models.CourseClass>();
        public List<CellItem> GridCells { get; set; } = new List<CellItem>();
    }

    public partial class ScheduleAdvisorWindow : Window
    {
        private List<StudentReminderApp.Views.Pages.AdvisorCourseSelectionItem> _targetCourses;
        private Dictionary<string, string> _preferredLecturers;
        private int _scheduleProfileIndex;
        public ScheduleOption SelectedOption { get; private set; }

        public ScheduleAdvisorWindow(List<StudentReminderApp.Views.Pages.AdvisorCourseSelectionItem> targetCourses, Dictionary<string, string> preferredLecturers, int scheduleProfileIndex)
        {
            InitializeComponent();
            _targetCourses = targetCourses;
            _preferredLecturers = preferredLecturers;
            _scheduleProfileIndex = scheduleProfileIndex;
            CourseListBox.ItemsSource = _targetCourses;
            RunAutoSchedule();
        }

        private void RunAutoSchedule()
        {
            var buckets = new List<List<StudentReminderApp.Models.CourseClass>>();
            try
            {
                string path = @"D:\IT\HỌC\PBL3\PBL3-LTUD-APP\RENDER\HK2_2025.json";
                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    string[] parts = json.Split(new[] { "\"id\":" }, StringSplitOptions.None);
                    var allParsedClasses = new List<StudentReminderApp.Models.CourseClass>();
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var idMatch = System.Text.RegularExpressions.Regex.Match(parts[i], @"^\s*""([^""]+)""");
                        var groupMatch = System.Text.RegularExpressions.Regex.Match(parts[i], @"""group""\s*:\s*""([^""]+)""");
                        if (idMatch.Success && groupMatch.Success)
                        {
                            var gvMatch = System.Text.RegularExpressions.Regex.Match(parts[i], @"""lecturer_name""\s*:\s*""([^""]+)""");
                            if (!gvMatch.Success) gvMatch = System.Text.RegularExpressions.Regex.Match(parts[i], @"""lecturer""\s*:\s*""([^""]+)""");

                            var c = new StudentReminderApp.Models.CourseClass();
                            c.ParseClassCode(idMatch.Groups[1].Value);
                            c.group = groupMatch.Groups[1].Value;
                            c.LecturerName = gvMatch.Success ? gvMatch.Groups[1].Value : "Unknown";
                            for (int d = 2; d <= 8; d++)
                            {
                                var dayMatch = System.Text.RegularExpressions.Regex.Match(parts[i], $@"""thu{d}""\s*:\s*""([^""]*)""");
                                if (dayMatch.Success && !string.IsNullOrWhiteSpace(dayMatch.Groups[1].Value))
                                {
                                    ParseSessionString(c, d, dayMatch.Groups[1].Value);
                                }
                            }
                            allParsedClasses.Add(c);
                        }
                    }
                    foreach (var item in _targetCourses)
                    {
                        var matching = allParsedClasses.Where(c => c.courseId == item.CourseId || c.classCode.StartsWith(item.CourseId)).ToList();
                        if (matching.Count > 0)
                        {
                            foreach(var m in matching) m.CourseName = item.CourseName;
                            buckets.Add(matching);
                        }
                    }
                }
            }
            catch { }

            var optimizer = new ScheduleOptimizerService();
            var results = optimizer.GetOptimizedScheduleOptions(buckets, _preferredLecturers, _scheduleProfileIndex);
            var options = new List<ScheduleOption>();
            for (int i = 0; i < results.Count; i++)
            {
                var opt = new ScheduleOption { OptionId = $"Phương án {i + 1}", Score = optimizer.CalculateFitness(results[i]) };
                opt.Classes = results[i];
                GenerateCells(opt);
                options.Add(opt);
            }
            ScheduleTabs.ItemsSource = options;
        }

        private void ParseSessionString(StudentReminderApp.Models.CourseClass c, int day, string scheduleStr)
        {
            var parts = scheduleStr.Split(',');
            string lessonPart = parts[0].Trim();
            string roomPart = parts.Length > 1 ? parts[1].Trim() : "";
            var session = new Session { dayOfWeek = day, lessons = new List<int>(), room = roomPart };
            var lessonParts = lessonPart.Split('-');
            if (lessonParts.Length == 2 && int.TryParse(lessonParts[0], out int start) && int.TryParse(lessonParts[1], out int end))
            {
                for (int l = start; l <= end; l++) session.lessons.Add(l);
            }
            else if (int.TryParse(lessonPart, out int single))
            {
                session.lessons.Add(single);
            }
            if (session.lessons.Count > 0) c.sessions.Add(session);
        }

        private void GenerateCells(ScheduleOption option)
        {
            var cells = new CellItem[88];
            var bc = new BrushConverter();
            for (int i = 0; i < 88; i++) cells[i] = new CellItem { Text = "", Background = Brushes.Transparent, BorderBrush = (Brush)bc.ConvertFrom("#E2E8F0"), BorderThickness = new Thickness(0, 0, 1, 1), Foreground = (Brush)bc.ConvertFrom("#1E293B") };
            cells[0].Text = "Tiết \\ Thứ"; cells[0].Background = (Brush)bc.ConvertFrom("#F8FAFC");
            for (int d = 2; d <= 7; d++) { cells[d - 1].Text = "Thứ " + d; cells[d - 1].Background = (Brush)bc.ConvertFrom("#F8FAFC"); }
            cells[7].Text = "CN"; cells[7].Background = (Brush)bc.ConvertFrom("#F8FAFC");
            string[] starts = { "07:00", "08:00", "09:00", "10:00", "11:00", "13:00", "14:00", "15:00", "16:00", "17:00" };
            string[] ends = { "07:50", "08:50", "09:50", "10:50", "11:50", "13:50", "14:50", "15:50", "16:50", "17:50" };
            for (int r = 1; r <= 10; r++) { 
                cells[r * 8].Text = "Tiết " + r; 
                cells[r * 8].TopText = starts[r - 1];
                cells[r * 8].BottomText = ends[r - 1];
                cells[r * 8].Background = (Brush)bc.ConvertFrom("#F8FAFC"); 
            }
            int colorIndex = 0;
            foreach (var c in option.Classes) {
                bool isTeal = (colorIndex % 2 == 1);
                colorIndex++;
                foreach (var s in c.sessions) {
                    int col = s.dayOfWeek - 1; 
                    foreach (var l in s.lessons) {
                        int row = l;
                        int index = row * 8 + col;
                        if (index >= 0 && index < 88) {
                            cells[index].Text = $"{c.CourseName}\n{c.classCode}\nP: {s.room}";
                            if (isTeal) {
                                cells[index].Background = (Brush)bc.ConvertFrom("#F0FDF4");
                                cells[index].BorderBrush = (Brush)bc.ConvertFrom("#10B981");
                                cells[index].Foreground = (Brush)bc.ConvertFrom("#064E3B");
                            } else {
                                cells[index].Background = (Brush)bc.ConvertFrom("#EFF6FF");
                                cells[index].BorderBrush = (Brush)bc.ConvertFrom("#3B82F6");
                                cells[index].Foreground = (Brush)bc.ConvertFrom("#1E3A8A");
                            }
                            cells[index].BorderThickness = new Thickness(4, 0, 0, 0);
                        }
                    }
                }
            }
            option.GridCells = cells.ToList();
        }

        private void ApplySchedule_Click(object sender, RoutedEventArgs e)
        {
            if (ScheduleTabs.SelectedItem is ScheduleOption opt)
            {
                SelectedOption = opt;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một Thời khóa biểu.", "Thông báo");
            }
        }
    }
}