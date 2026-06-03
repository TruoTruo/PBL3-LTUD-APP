using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;
using StudentReminderApp.Services;

namespace StudentReminderApp.Views.Pages
{
    public class CurriculumItem : INotifyPropertyChanged
    {
        public int SerialNumber { get; set; }
        public int HocKy { get; set; }
        public string Symbol { get; set; }
        public string MaHocPhan { get; set; }
        public string TenHocPhan { get; set; }
        public double SoTC { get; set; }
        public string Optional { get; set; }
        public string HtDa { get; set; }
        public string TqDa { get; set; }
        public string Relation { get; set; }
        public string Prerequisite { get; set; }
        public string Corequisite { get; set; }

        // Format hiển thị tách biệt 3 cột trên bảng
        public string HocTruocStr => string.IsNullOrWhiteSpace(Relation) ? "-" : Relation.Replace("\n", "; ");
        public string SongHanhStr => string.IsNullOrWhiteSpace(Corequisite) ? "-" : Corequisite.Replace("\n", "; ");
        public string TienQuyetStr => string.IsNullOrWhiteSpace(Prerequisite) ? "-" : Prerequisite.Replace("\n", "; ");

        public string Registration
        {
            get
            {
                if (StatusText == "Đã học" || StatusText == "Đang học")
                    return "Đã đăng ký";

                if (!string.IsNullOrWhiteSpace(Optional) && string.IsNullOrWhiteSpace(Relation) && string.IsNullOrWhiteSpace(Prerequisite) && string.IsNullOrWhiteSpace(Corequisite))
                    return "Tự chọn";

                if (!string.IsNullOrWhiteSpace(Relation) || !string.IsNullOrWhiteSpace(Prerequisite))
                    return "Chưa đủ điều kiện";

                if (!string.IsNullOrWhiteSpace(Corequisite))
                    return "Cần đăng ký cùng";

                return "Có thể đăng ký";
            }
        }
        
        public string RegistrationColor
        {
            get
            {
                string reg = Registration;
                if (reg == "Chưa đủ điều kiện") return "#DC2626"; // Đỏ
                if (reg == "Cần đăng ký cùng") return "#D97706"; // Vàng cam
                if (reg == "Tự chọn") return "#2563EB"; // Xanh dương
                return "#059669"; // Xanh lá
            }
        }

        private string _statusText;
        public string StatusText 
        { 
            get => _statusText; 
            set 
            { 
                if (_statusText != value)
                {
                    _statusText = value;
                    UpdateColors();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BgColor));
                    OnPropertyChanged(nameof(FgColor));
                    OnPropertyChanged(nameof(DiemChu));
                    OnPropertyChanged(nameof(DiemSo));
                    OnPropertyChanged(nameof(Registration));
                    OnPropertyChanged(nameof(RegistrationColor));
                }
            }
        }  // Đã học, Đang học, Chưa học
        
        public string BgColor { get; set; }     // Flat Background Color
        public string FgColor { get; set; }     // Flat Foreground Text Color
        public string DiemChu { get; set; }
        public double DiemSo { get; set; }
        public string GiangVien { get; set; } = "N/A";
        public string ThoiKhoaBieu { get; set; } = "N/A";
        public string TuanHoc { get; set; } = "1-15";
        
        public long IdMonHoc { get; set; }  // Để lưu vào database
        public long IdSv { get; set; }      // Để lưu vào database
        
        // Hàm cập nhật màu sắc dựa trên trạng thái
        private void UpdateColors()
        {
            if (_statusText == "Đã học") 
            { 
                BgColor = "#D1FAE5"; // Màu xanh lá nhạt
                FgColor = "#065F46"; // Màu xanh lá đậm
                DiemChu = "B+"; 
                DiemSo = 3.5;
            }
            else if (_statusText == "Đang học") 
            { 
                BgColor = "#FEF3C7"; 
                FgColor = "#92400E"; 
                DiemChu = "-"; 
                DiemSo = 0;
            }
            else 
            { 
                BgColor = "#F1F5F9"; 
                FgColor = "#475569"; 
                DiemChu = "-"; 
                DiemSo = 0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class CoursePage : Page
    {
        public CoursePage()
        {
            InitializeComponent();
            DataService.Initialize(); // Khởi tạo Service JSON
            LoadStudentInfo();
            RenderRoadmap();
        }

        private void LoadStudentInfo()
        {
            if (SessionManager.CurrentUser != null)
            {
                TxtStudentInfo.Text = $"SV: {SessionManager.CurrentUser.HoTen} - ID: {SessionManager.CurrentUser.IdAcc}";
            }
        }

        private void Config_Changed(object sender, RoutedEventArgs e)
        {
            // Sự kiện gọi khi đổi ComboBox Hệ đào tạo hoặc RadioButton Chuyên ngành
            if (IsLoaded) RenderRoadmap();
        }

        // Event handler cho thay đổi trạng thái

        private void SaveCourseStatus(CurriculumItem item)
        {
            try
            {
                // TODO: Gọi BLL/DAL để lưu trạng thái
                // Tạm thời chỉ cập nhật ngoài bộ nhớ
                System.Diagnostics.Debug.WriteLine($"Lưu trạng thái: {item.MaHocPhan} - {item.StatusText}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu trạng thái: {ex.Message}");
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurriculumItem.StatusText) && sender is CurriculumItem item)
            {
                SaveCourseStatus(item);
                RefreshDataGrids();
            }
        }

        private void RefreshDataGrids()
        {
            if (DgRoadmap.ItemsSource is List<CurriculumItem> courses)
            {
                DgCurrentCourses.ItemsSource = null;
                DgCurrentCourses.ItemsSource = courses.Where(c => c.StatusText == "Đang học").ToList();
                DgHistory.ItemsSource = null;
                DgHistory.ItemsSource = courses.Where(c => c.StatusText != "Chưa học").ToList();
            }
        }

        private void RenderRoadmap()
        {
            // Nạp dữ liệu JSON thực tế theo thiết lập hệ
            string programId = "ai";
            if (RbNhat != null && RbNhat.IsChecked == true) programId = "nhat";
            else if (RbDacThu != null && RbDacThu.IsChecked == true) programId = "dacthu";

            var programData = DataService.GetProgramData(programId);
            if (programData == null || programData.Semesters == null)
            {
                if (DgRoadmap != null) DgRoadmap.ItemsSource = new List<CurriculumItem>();
                if (DgCurrentCourses != null) DgCurrentCourses.ItemsSource = new List<CurriculumItem>();
                if (DgHistory != null) DgHistory.ItemsSource = new List<CurriculumItem>();
                return;
            }

            // Hủy đăng ký event cho danh sách cũ để tránh memory leak khi chuyển đổi giữa các chuyên ngành
            if (DgRoadmap.ItemsSource is List<CurriculumItem> oldCourses)
            {
                foreach (var c in oldCourses) c.PropertyChanged -= Item_PropertyChanged;
            }

            int maxSemester = (CmbHeDaoTao != null && CmbHeDaoTao.SelectedIndex == 0) ? 8 : 10;
            var filteredCourses = new List<CurriculumItem>();

            foreach (var sem in programData.Semesters.Where(s => s != null && s.Semester <= maxSemester))
            {
                if (sem.Courses == null) continue;
                foreach (var c in sem.Courses)
                {
                    string status = !string.IsNullOrWhiteSpace(c.Status)
                        ? c.Status!
                        : sem.Semester < 4 ? "Đã học" : (sem.Semester == 4 ? "Đang học" : "Chưa học");

                    var item = new CurriculumItem
                    {
                        SerialNumber = filteredCourses.Count + 1,
                        HocKy = sem.Semester,
                        Symbol = string.IsNullOrWhiteSpace(c.Symbol) ? "" : c.Symbol!,
                        MaHocPhan = c.Id ?? "N/A",
                        TenHocPhan = c.Name ?? "N/A",
                        SoTC = c.Credits,
                        Optional = string.IsNullOrWhiteSpace(c.Optional) ? "" : c.Optional!,
                        HtDa = string.IsNullOrWhiteSpace(c.HtDa) ? "" : c.HtDa!,
                        TqDa = string.IsNullOrWhiteSpace(c.TqDa) ? "" : c.TqDa!,
                        Relation = string.IsNullOrWhiteSpace(c.Relation) ? "" : c.Relation!,
                        Prerequisite = string.IsNullOrWhiteSpace(c.Prerequisite) ? "" : c.Prerequisite!,
                        Corequisite = string.IsNullOrWhiteSpace(c.Corequisite) ? "" : c.Corequisite!,
                        StatusText = status,
                        GiangVien = string.IsNullOrWhiteSpace(c.Lecturer) ? "Chưa phân công" : c.Lecturer!,
                        ThoiKhoaBieu = string.IsNullOrWhiteSpace(c.Session) ? "Chưa sắp xếp" : c.Session!,
                        TuanHoc = string.IsNullOrWhiteSpace(c.Weeks) ? "1-15" : c.Weeks!
                    };

                    item.PropertyChanged += Item_PropertyChanged;
                    filteredCourses.Add(item);
                }
            }

            // 2. Đổ dữ liệu vào DataGrid Khung chương trình
            DgRoadmap.ItemsSource = filteredCourses;

            // 3. Đổ dữ liệu môn Đang học hiện tại
            DgCurrentCourses.ItemsSource = filteredCourses.Where(c => c.StatusText == "Đang học").ToList();

            // 4. Đổ dữ liệu History (chỉ những môn Đã học / Đang học)
            DgHistory.ItemsSource = filteredCourses.Where(c => c.StatusText != "Chưa học").ToList();

            if (TxtCourseStatus != null)
            {
                TxtCourseStatus.Text = filteredCourses.Count == 0
                    ? $"Không có môn học cho chương trình '{programData?.ProgramInfo?.Name ?? programId}'. Kiểm tra lại file CSV và đường dẫn."
                    : $"Đã nạp {filteredCourses.Count} môn học từ file {programData?.ProgramInfo?.Name ?? programId}.csv.";
            }
        }
    }
}