using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.Views.Pages
{
    public class AdvisorSession
    {
        public int DayOfWeek { get; set; }
        public List<int> Lessons { get; set; } = new List<int>();
    }

    public class AdvisorCourseClass
    {
        public string ClassCode { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public List<AdvisorSession> Schedule { get; set; } = new List<AdvisorSession>();
        public bool IsOverriding { get; set; }
    }

    public class AdvisorCellItem
    {
        public string Text { get; set; } = string.Empty;
        public Brush Background { get; set; } = Brushes.Transparent;
        public Brush BorderBrush { get; set; } = Brushes.Transparent;
        public Thickness BorderThickness { get; set; } = new Thickness(0);
        public Brush Foreground { get; set; } = Brushes.Black;
    }

    public class AdvisorScheduleOption
    {
        public string OptionId { get; set; } = string.Empty;
        public double Score { get; set; }
        public List<AdvisorCourseClass> Classes { get; set; } = new List<AdvisorCourseClass>();
        public List<AdvisorCellItem> GridCells { get; set; } = new List<AdvisorCellItem>();
    }

    public class AdvisorCourseSelectionItem : System.ComponentModel.INotifyPropertyChanged
    {
        public int SerialNumber { get; set; }
        public int HocKy { get; set; }
        
        private bool _isSelected = false;
        public bool IsSelected 
        { 
            get => _isSelected; 
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } } 
        }
        
        private bool _canRegister = true;
        public bool CanRegister 
        { 
            get => _canRegister; 
            set { if (_canRegister != value) { _canRegister = value; OnPropertyChanged(nameof(CanRegister)); } } 
        }
        
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public double SoTC { get; set; }
        
        private string _registration = "Có thể đăng ký";
        public string Registration 
        { 
            get => _registration; 
            set { if (_registration != value) { _registration = value; OnPropertyChanged(nameof(Registration)); } } 
        }
        
        private string _registrationColor = "#059669";
        public string RegistrationColor 
        { 
            get => _registrationColor; 
            set { if (_registrationColor != value) { _registrationColor = value; OnPropertyChanged(nameof(RegistrationColor)); } } 
        }
        
        public ObservableCollection<string> ClassOptions { get; set; } = new ObservableCollection<string>();
        
        private int _selectedClassIndex = -1;
        public int SelectedClassIndex 
        { 
            get => _selectedClassIndex; 
            set { 
                if (_selectedClassIndex != value) { 
                    _selectedClassIndex = value; 
                    if (value >= 0 && value < ClassOptions.Count) {
                        string opt = ClassOptions[value];
                        SelectedClassSchedule = ClassSchedules.ContainsKey(opt) ? ClassSchedules[opt] : "";
                        SelectedClassRoom = ClassRooms.ContainsKey(opt) ? ClassRooms[opt] : "";
                    } else {
                        SelectedClassSchedule = "";
                        SelectedClassRoom = "";
                    }
                    OnPropertyChanged(nameof(SelectedClassIndex)); 
                    OnPropertyChanged(nameof(SelectedClassSchedule));
                    OnPropertyChanged(nameof(SelectedClassRoom));
                } 
            } 
        }

        public string SelectedClassSchedule { get; private set; } = string.Empty;
        public Dictionary<string, string> ClassSchedules { get; set; } = new Dictionary<string, string>();

        public string SelectedClassRoom { get; private set; } = string.Empty;
        public Dictionary<string, string> ClassRooms { get; set; } = new Dictionary<string, string>();

        public ObservableCollection<string> LecturerOptions { get; set; } = new ObservableCollection<string>() { "— Bất kỳ —" };
        
        private string _selectedLecturer = "— Bất kỳ —";
        public string SelectedLecturer
        {
            get => _selectedLecturer;
            set { if (_selectedLecturer != value) { _selectedLecturer = value; OnPropertyChanged(nameof(SelectedLecturer)); } }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public partial class AdvisorPage : Page
    {
        private List<AdvisorCourseSelectionItem> _courseSelections = new List<AdvisorCourseSelectionItem>();
        private HashSet<string> _passedCourseIds = new HashSet<string>();
        private HashSet<string> _allLearningCourseIds = new HashSet<string>();
        private ObservableCollection<AdvisorCourseSelectionItem> _selectedCoursesList = new ObservableCollection<AdvisorCourseSelectionItem>();
        private bool _isLoadingData = false;
        private bool _isRefreshing = false;

        public AdvisorPage()
        {
            InitializeComponent();
            DgSelectedCourses.ItemsSource = _selectedCoursesList;
            DgClasses.ItemsSource = _selectedCoursesList;
            LoadData();
        }

        private void LoadData()
        {
            _isLoadingData = true;
            _passedCourseIds.Clear();
            _allLearningCourseIds.Clear();
            
            if (SessionManager.CurrentAccount != null)
            {
                try
                {
                    using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString))
                    {
                        conn.Open();
                        string sql = @"SELECT m.ma_mon_hoc, t.trang_thai_hoc FROM TICH_LUY_TIN_CHI t JOIN MON_HOC m ON t.id_mon_hoc = m.id_mon_hoc WHERE t.id_sv = @uid";
                        using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@uid", SessionManager.CurrentAccount.IdAcc);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string ma = reader["ma_mon_hoc"].ToString();
                                    string tt = reader["trang_thai_hoc"].ToString();
                                    if (tt == "DaHoc") _passedCourseIds.Add(ma);
                                    if (tt == "DangHoc") _allLearningCourseIds.Add(ma);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            Services.DataService.Initialize();
            var prog = Services.DataService.GetProgramData("nhat");
            
            foreach (var item in _courseSelections) item.PropertyChanged -= SelectionItem_PropertyChanged;
            _courseSelections.Clear();
            _selectedCoursesList.Clear();
            int sn = 1;

            if (prog != null && prog.Semesters != null)
            {
                foreach (var sem in prog.Semesters)
                {
                    if (sem.Courses == null) continue;
                    foreach (var c in sem.Courses)
                    {
                        if (_passedCourseIds.Contains(c.Id)) continue;

                        var item = new AdvisorCourseSelectionItem
                        {
                            SerialNumber = sn++,
                            HocKy = sem.Semester,
                            CourseId = c.Id,
                            CourseName = c.Name,
                            SoTC = c.Credits,
                            IsSelected = _allLearningCourseIds.Contains(c.Id)
                        };

                        item.PropertyChanged += SelectionItem_PropertyChanged;
                        _courseSelections.Add(item);
                        if (item.IsSelected) _selectedCoursesList.Add(item);
                    }
                }
            }

            RefreshConditions();

            if (DgCourses != null)
            {
                DgCourses.ItemsSource = null;
                DgCourses.ItemsSource = _courseSelections;
            }
            _isLoadingData = false;
        }

        private void RefreshConditions()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            bool changed;
            do
            {
                changed = false;
                foreach (var item in _courseSelections)
                {
                    if (item.IsSelected) _allLearningCourseIds.Add(item.CourseId);
                    else _allLearningCourseIds.Remove(item.CourseId);
                }

                Services.DataService.Initialize();
                var prog = Services.DataService.GetProgramData("nhat");
                if (prog == null || prog.Semesters == null) break;

                var courseDict = prog.Semesters
                    .Where(s => s.Courses != null)
                    .SelectMany(s => s.Courses)
                    .GroupBy(c => c.Id)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var item in _courseSelections)
                {
                    if (!courseDict.TryGetValue(item.CourseId, out var c)) continue;

                    string regStatus = "Có thể đăng ký";
                    string regColor = "#059669";
                    bool canReg = true;

                    if (!string.IsNullOrWhiteSpace(c.Prerequisite))
                    {
                        var reqs = c.Prerequisite.Split(';');
                        foreach (var req in reqs)
                        {
                            var p = req.Trim().Split('-');
                            if (p.Length > 0 && !_passedCourseIds.Contains(p[0].Trim()))
                            {
                                regStatus = "Chưa đủ ĐK (Thiếu Tiên quyết)";
                                regColor = "#DC2626";
                                canReg = false;
                                break;
                            }
                        }
                    }

                    if (canReg && !string.IsNullOrWhiteSpace(c.Relation))
                    {
                        var reqs = c.Relation.Split(';');
                        foreach (var req in reqs)
                        {
                            var p = req.Trim().Split('-');
                            if (p.Length > 0 && !_passedCourseIds.Contains(p[0].Trim()))
                            {
                                regStatus = "Chưa đủ ĐK (Thiếu Học trước)";
                                regColor = "#DC2626";
                                canReg = false;
                                break;
                            }
                        }
                    }

                    if (canReg && !string.IsNullOrWhiteSpace(c.Corequisite))
                    {
                        var reqs = c.Corequisite.Split(';');
                        foreach (var req in reqs)
                        {
                            var p = req.Trim().Split('-');
                            if (p.Length > 0 && !_passedCourseIds.Contains(p[0].Trim()) && !_allLearningCourseIds.Contains(p[0].Trim()))
                            {
                                regStatus = "Cần đăng ký cùng";
                                regColor = "#D97706";
                            }
                        }
                    }

                    item.Registration = regStatus;
                    item.RegistrationColor = regColor;
                    item.CanRegister = canReg;

                    if ((regStatus.Contains("Chưa đủ ĐK") || regStatus == "Cần đăng ký cùng") && item.IsSelected)
                    {
                        changed = true;
                        item.IsSelected = false; 
                        _selectedCoursesList.Remove(item);
                    }
                }
            } while (changed);

            _isRefreshing = false;
        }

        private void SelectionItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isLoadingData || _isRefreshing) return;
            if (e.PropertyName == nameof(AdvisorCourseSelectionItem.IsSelected) && sender is AdvisorCourseSelectionItem item)
            {
                if (item.IsSelected && !_selectedCoursesList.Contains(item))
                    _selectedCoursesList.Add(item);
                else if (!item.IsSelected && _selectedCoursesList.Contains(item))
                    _selectedCoursesList.Remove(item);

                RefreshConditions();
            }
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCoursesList.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một học phần.", "Thông báo");
                return;
            }
            
            try
            {
                string path = @"D:\IT\HỌC\PBL3\PBL3-LTUD-APP\RENDER\HK2_2025.json";
                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    var allJsonCourses = new List<Tuple<string, string, string, string, string>>();
                    string[] parts = json.Split(new[] { "\"id\":" }, StringSplitOptions.None);
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var idMatch = System.Text.RegularExpressions.Regex.Match(parts[i], @"^\s*""([^""]+)""");
                        var groupMatch = System.Text.RegularExpressions.Regex.Match(parts[i], @"""group""\s*:\s*""([^""]+)""");
                        var gvMatch = System.Text.RegularExpressions.Regex.Match(parts[i], @"""lecturer_name""\s*:\s*""([^""]+)""");
                        if (!gvMatch.Success) gvMatch = System.Text.RegularExpressions.Regex.Match(parts[i], @"""lecturer""\s*:\s*""([^""]+)""");
                        
                        string scheduleStr = "";
                        var rooms = new HashSet<string>();
                        for (int d = 2; d <= 7; d++)
                        {
                            var dayMatch = System.Text.RegularExpressions.Regex.Match(parts[i], $@"""thu{d}""\s*:\s*""([^""]*)""");
                            if (dayMatch.Success && !string.IsNullOrWhiteSpace(dayMatch.Groups[1].Value))
                            {
                                var valParts = dayMatch.Groups[1].Value.Split(',');
                                string lessons = valParts[0].Trim();
                                if (valParts.Length > 1 && !string.IsNullOrWhiteSpace(valParts[1])) rooms.Add(valParts[1].Trim());

                                if (scheduleStr != "") scheduleStr += ", ";
                                scheduleStr += $"Thứ {d}: {lessons}";
                            }
                        }
                        string roomStr = string.Join(", ", rooms);

                        if (idMatch.Success && groupMatch.Success)
                        {
                            allJsonCourses.Add(new Tuple<string, string, string, string, string>(idMatch.Groups[1].Value, groupMatch.Groups[1].Value, gvMatch.Success ? gvMatch.Groups[1].Value : "Unknown", scheduleStr, roomStr));
                        }
                    }
                    var groupCounts = new Dictionary<string, int>();
                    foreach (var item in _selectedCoursesList)
                    {
                        item.ClassOptions.Clear();
                        var matchingCourses = allJsonCourses.Where(c => c.Item1.StartsWith(item.CourseId)).ToList();
                        var itemGroups = new HashSet<string>();
                        var addedOptions = new HashSet<string>();
                        foreach (var c in matchingCourses)
                        {
                            string optionStr = $"{c.Item1} - Nhóm {c.Item2} - GV: {c.Item3}";
                            if (!addedOptions.Contains(optionStr))
                            {
                                item.ClassOptions.Add(optionStr);
                                item.ClassSchedules[optionStr] = c.Item4;
                                item.ClassRooms[optionStr] = c.Item5;
                                addedOptions.Add(optionStr);
                            }
                            if (!string.IsNullOrWhiteSpace(c.Item3) && c.Item3 != "Unknown" && !item.LecturerOptions.Contains(c.Item3))
                            {
                                item.LecturerOptions.Add(c.Item3);
                            }
                            itemGroups.Add(c.Item2);
                        }
                        foreach (var g in itemGroups)
                        {
                            if (groupCounts.ContainsKey(g)) groupCounts[g]++;
                            else groupCounts[g] = 1;
                        }
                    }
                    string bestGroup = "";
                    int maxCount = 0;
                    foreach (var kvp in groupCounts)
                    {
                        if (kvp.Value > maxCount)
                        {
                            maxCount = kvp.Value;
                            bestGroup = kvp.Key;
                        }
                    }
                    foreach (var item in _selectedCoursesList)
                    {
                        item.SelectedClassIndex = -1;
                        bool found = false;
                        if (item.ClassOptions.Count > 0)
                        {
                            for (int i = 0; i < item.ClassOptions.Count; i++)
                            {
                                string classCode = item.ClassOptions[i].Split(' ')[0];
                                if (classCode.EndsWith("2499"))
                                {
                                    item.SelectedClassIndex = i;
                                    found = true;
                                    break;
                                }
                            }
                            if (!found && !string.IsNullOrEmpty(bestGroup))
                            {
                                for (int i = 0; i < item.ClassOptions.Count; i++)
                                {
                                    if (item.ClassOptions[i].Contains($"- Nhóm {bestGroup} -"))
                                    {
                                        item.SelectedClassIndex = i;
                                        break;
                                    }
                                }
                            }
                            if (!found && item.ClassOptions.Count > 0)
                            {
                                item.SelectedClassIndex = 0;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var item in _selectedCoursesList)
                    {
                        if (item.ClassOptions.Count == 0)
                        {
                            item.ClassOptions.Add($"{item.CourseId} - Nhóm 01");
                            item.ClassOptions.Add($"{item.CourseId} - Nhóm 02");
                            item.ClassOptions.Add($"{item.CourseId} - Nhóm 99");
                            item.SelectedClassIndex = 0;
                        }
                    }
                }
            }
            catch
            {
                foreach (var item in _selectedCoursesList)
                {
                    if (item.ClassOptions.Count == 0)
                    {
                        item.ClassOptions.Add($"{item.CourseId} - Nhóm 01");
                        item.ClassOptions.Add($"{item.CourseId} - Nhóm 02");
                        item.ClassOptions.Add($"{item.CourseId} - Nhóm 99");
                        item.SelectedClassIndex = 0;
                    }
                }
            }

            Step1Grid.Visibility = Visibility.Collapsed;
            Step2Grid.Visibility = Visibility.Visible;
        }

        private void BackStep_Click(object sender, RoutedEventArgs e)
        {
            Step2Grid.Visibility = Visibility.Collapsed;
            Step1Grid.Visibility = Visibility.Visible;
        }

        private void AutoSchedule_Click(object sender, RoutedEventArgs e)
        {
            var preferredLecturers = new Dictionary<string, string>();
            foreach (var item in _selectedCoursesList)
            {
                if (!string.IsNullOrEmpty(item.SelectedLecturer) && item.SelectedLecturer != "— Bất kỳ —")
                {
                    preferredLecturers[item.CourseId] = item.SelectedLecturer;
                }
            }
            int profileIndex = CmbScheduleProfile.SelectedIndex;

            var window = new StudentReminderApp.Views.Dialogs.ScheduleAdvisorWindow(_selectedCoursesList.ToList(), preferredLecturers, profileIndex);
            if (window.ShowDialog() == true && window.SelectedOption != null)
            {
                foreach (var cls in window.SelectedOption.Classes)
                {
                    var matchItem = _selectedCoursesList.FirstOrDefault(x => cls.classCode.StartsWith(x.CourseId));
                    if (matchItem != null)
                    {
                        for (int i = 0; i < matchItem.ClassOptions.Count; i++)
                        {
                            if (matchItem.ClassOptions[i].Contains(cls.classCode))
                            {
                                matchItem.SelectedClassIndex = i;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ConfirmRegistration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exportList = new List<object>();
                foreach (var item in _selectedCoursesList)
                {
                    if (item.ClassOptions.Count > 0 && item.SelectedClassIndex >= 0)
                    {
                        string optionStr = item.ClassOptions[item.SelectedClassIndex];
                        string scheduleStr = item.ClassSchedules.ContainsKey(optionStr) ? item.ClassSchedules[optionStr] : "";
                        string roomStr = item.ClassRooms.ContainsKey(optionStr) ? item.ClassRooms[optionStr] : "";
                        string lecturer = "";
                        string group = "";
                        
                        var parts = optionStr.Split(new string[] { " - " }, System.StringSplitOptions.None);
                        string classCode = parts.Length > 0 ? parts[0] : item.CourseId;
                        if (parts.Length > 1) group = parts[1].Replace("Nhóm ", "");
                        if (parts.Length > 2) lecturer = parts[2].Replace("GV: ", "");

                        exportList.Add(new
                        {
                            CourseId = item.CourseId,
                            CourseName = item.CourseName,
                            ClassCode = classCode,
                            Group = group,
                            LecturerName = lecturer,
                            ScheduleStr = scheduleStr,
                            RoomStr = roomStr
                        });
                    }
                }

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(exportList, Newtonsoft.Json.Formatting.Indented);

                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.FileName = "TKB_HK2_2025.json";
                if (saveFileDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveFileDialog.FileName, jsonString);
                    MessageBox.Show("Đã lưu thông tin Đăng ký Học phần thành công!\nBạn có thể Nhập (Import) file này tại tab Lịch học.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    BackStep_Click(null, null);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi lưu file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
