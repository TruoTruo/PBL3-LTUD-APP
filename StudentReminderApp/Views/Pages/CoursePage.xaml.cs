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

        public Func<string, bool> IsCoursePassed { get; set; }
        public Func<string, bool> IsCourseLearningOrPassed { get; set; }
        public Func<string, int> GetOptionalGroupPassedCount { get; set; }

        // Format hiển thị tách biệt 3 cột trên bảng
        public string HocTruocStr => string.IsNullOrWhiteSpace(Relation) ? "-" : Relation.Replace("\n", "; ");
        public string SongHanhStr => string.IsNullOrWhiteSpace(Corequisite) ? "-" : Corequisite.Replace("\n", "; ");
        public string TienQuyetStr => string.IsNullOrWhiteSpace(Prerequisite) ? "-" : Prerequisite.Replace("\n", "; ");

        public string Registration
        {
            get
            {
                // Kiểm tra Tiên quyết
                if (!string.IsNullOrWhiteSpace(Prerequisite))
                {
                    var preReqs = Prerequisite.Split(';');
                    foreach (var req in preReqs)
                    {
                        var parts = req.Trim().Split('-');
                        if (parts.Length > 0 && IsCoursePassed != null && !IsCoursePassed(parts[0].Trim()))
                            return "Chưa đủ ĐK (Thiếu Tiên quyết)";
                    }
                }

                // Kiểm tra Học trước
                if (!string.IsNullOrWhiteSpace(Relation))
                {
                    var relations = Relation.Split(';');
                    foreach (var req in relations)
                    {
                        var parts = req.Trim().Split('-');
                        if (parts.Length > 0 && IsCoursePassed != null && !IsCoursePassed(parts[0].Trim()))
                            return "Chưa đủ ĐK (Thiếu Học trước)";
                    }
                }

                // Kiểm tra Song hành
                if (!string.IsNullOrWhiteSpace(Corequisite))
                {
                    var coReqs = Corequisite.Split(';');
                    foreach (var req in coReqs)
                    {
                        var parts = req.Trim().Split('-');
                        if (parts.Length > 0 && IsCourseLearningOrPassed != null && !IsCourseLearningOrPassed(parts[0].Trim()))
                            return "Cần đăng ký cùng";
                    }
                }

                // Sau khi vượt qua tất cả Ràng buộc, mới trả về trạng thái Đã Đăng Ký nếu đang học
                if (StatusText == "Đã học" || StatusText == "Đang học")
                    return "Đã đăng ký";

                // Kiểm tra môn tự chọn
                if (!string.IsNullOrWhiteSpace(Optional))
                {
                    if (Optional.Contains("Chọn") && Optional.Contains("trong"))
                    {
                        var parts = Optional.Split(' ');
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int required))
                        {
                            if (GetOptionalGroupPassedCount != null && GetOptionalGroupPassedCount(Optional) >= required)
                                return "Đã đủ tự chọn";
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(Optional) && string.IsNullOrWhiteSpace(Relation) && string.IsNullOrWhiteSpace(Prerequisite) && string.IsNullOrWhiteSpace(Corequisite))
                    return "Tự chọn";

                return "Có thể đăng ký";
            }
        }
        
        public string RegistrationColor
        {
            get
            {
                string reg = Registration;
            if (reg.Contains("Chưa đủ ĐK")) return "#DC2626"; // Đỏ
                if (reg == "Cần đăng ký cùng") return "#D97706"; // Vàng cam
                if (reg == "Tự chọn") return "#2563EB"; // Xanh dương
            if (reg == "Đã đủ tự chọn") return "#9CA3AF"; // Xám nhạt
                return "#059669"; // Xanh lá
            }
        }

        public bool CanEditStatus => !Registration.Contains("Chưa đủ ĐK") && Registration != "Cần đăng ký cùng";

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
                    OnPropertyChanged(nameof(CanEditStatus));
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

        public void TriggerDependencies()
        {
            OnPropertyChanged(nameof(Registration));
            OnPropertyChanged(nameof(RegistrationColor));
            OnPropertyChanged(nameof(CanEditStatus));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class CoursePage : Page
    {
        private bool _isRendering = false;
        private Dictionary<string, string> _dbStatuses = new Dictionary<string, string>();
        private List<CurriculumItem> _allCourses = new List<CurriculumItem>();

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
            if (SessionManager.CurrentAccount == null) return;
            try
            {
                string dbStatus = item.StatusText switch { "Đã học" => "DaHoc", "Đang học" => "DangHoc", _ => "ChuaHoc" };
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    // 1. Đảm bảo môn học tồn tại
                    long idMonHoc = 0;
                    using (var cmdCheck = new System.Data.SqlClient.SqlCommand("SELECT id_mon_hoc FROM MON_HOC WHERE ma_mon_hoc = @ma", conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@ma", item.MaHocPhan);
                        var res = cmdCheck.ExecuteScalar();
                        if (res != null) idMonHoc = Convert.ToInt64(res);
                    }
                    if (idMonHoc == 0)
                    {
                        using (var cmdIns = new System.Data.SqlClient.SqlCommand("INSERT INTO MON_HOC (ma_mon_hoc, ten_mon_hoc, so_tin_chi) OUTPUT INSERTED.id_mon_hoc VALUES (@ma, @ten, @tc)", conn))
                        {
                            cmdIns.Parameters.AddWithValue("@ma", item.MaHocPhan);
                            cmdIns.Parameters.AddWithValue("@ten", item.TenHocPhan);
                            cmdIns.Parameters.AddWithValue("@tc", (int)item.SoTC);
                            idMonHoc = Convert.ToInt64(cmdIns.ExecuteScalar());
                        }
                    }
                    
                    // 2. Lưu trạng thái (Upsert)
                    string sqlUpsert = @"
                        IF EXISTS (SELECT 1 FROM TICH_LUY_TIN_CHI WHERE id_sv = @sv AND id_mon_hoc = @mh)
                            UPDATE TICH_LUY_TIN_CHI SET trang_thai_hoc = @tt, is_passed = @pass WHERE id_sv = @sv AND id_mon_hoc = @mh
                        ELSE
                            INSERT INTO TICH_LUY_TIN_CHI (id_sv, id_mon_hoc, trang_thai_hoc, is_passed) VALUES (@sv, @mh, @tt, @pass)";
                    using (var cmdUp = new System.Data.SqlClient.SqlCommand(sqlUpsert, conn))
                    {
                        cmdUp.Parameters.AddWithValue("@sv", SessionManager.CurrentAccount.IdAcc);
                        cmdUp.Parameters.AddWithValue("@mh", idMonHoc);
                        cmdUp.Parameters.AddWithValue("@tt", dbStatus);
                        cmdUp.Parameters.AddWithValue("@pass", dbStatus == "DaHoc" ? 1 : 0);
                        cmdUp.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu trạng thái: {ex.Message}");
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurriculumItem.StatusText) && sender is CurriculumItem item && !_isRendering)
            {
                SaveCourseStatus(item);
                
                // Cập nhật điều kiện và kiểm tra lan truyền (cascade) nếu bị mất điều kiện tiên quyết
                bool cascadeOccurred;
                do
                {
                    cascadeOccurred = false;
                    foreach (var c in _allCourses)
                    {
                        c.TriggerDependencies();
                        if ((c.Registration.Contains("Chưa đủ ĐK") || c.Registration == "Cần đăng ký cùng") && c.StatusText != "Chưa học")
                        {
                            _isRendering = true; // Tạm tắt event để không bị lặp vô hạn
                            c.StatusText = "Chưa học";
                            SaveCourseStatus(c);
                            _isRendering = false;
                            cascadeOccurred = true;
                        }
                    }
                } while (cascadeOccurred);
                
                RefreshDataGrids();
                UpdateDashboard();
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

        private void LoadStatusesFromDB()
        {
            _dbStatuses.Clear();
            if (SessionManager.CurrentAccount == null) return;
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(AppConfig.ConnectionString))
                {
                    conn.Open();
                    string sql = @"SELECT m.ma_mon_hoc, t.trang_thai_hoc 
                                   FROM TICH_LUY_TIN_CHI t JOIN MON_HOC m ON t.id_mon_hoc = m.id_mon_hoc 
                                   WHERE t.id_sv = @uid";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", SessionManager.CurrentAccount.IdAcc);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _dbStatuses[reader["ma_mon_hoc"].ToString()] = reader["trang_thai_hoc"].ToString();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void RenderRoadmap()
        {
            _isRendering = true;
            LoadStatusesFromDB();

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
            _allCourses.Clear();

            foreach (var sem in programData.Semesters.Where(s => s != null && s.Semester <= maxSemester))
            {
                if (sem.Courses == null) continue;
                foreach (var c in sem.Courses)
                {
                    string safeId = c.Id ?? "";
                    string dbStatus = _dbStatuses.ContainsKey(safeId) ? _dbStatuses[safeId] : "ChuaHoc";
                    string status = dbStatus == "DaHoc" ? "Đã học" : (dbStatus == "DangHoc" ? "Đang học" : "Chưa học");

                    var item = new CurriculumItem
                    {
                        SerialNumber = _allCourses.Count + 1,
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
                    TuanHoc = string.IsNullOrWhiteSpace(c.Weeks) ? "1-15" : c.Weeks!,
                    IsCoursePassed = (ma) => _allCourses.Any(x => x.MaHocPhan == ma && x.StatusText == "Đã học"),
                    IsCourseLearningOrPassed = (ma) => _allCourses.Any(x => x.MaHocPhan == ma && (x.StatusText == "Đã học" || x.StatusText == "Đang học")),
                    GetOptionalGroupPassedCount = (opt) => _allCourses.Count(x => x.Optional == opt && x.StatusText == "Đã học")
                    };

                    item.PropertyChanged += Item_PropertyChanged;
                _allCourses.Add(item);
                }
            }
        
        foreach (var c in _allCourses) c.TriggerDependencies();
        _isRendering = false;

            // 2. Đổ dữ liệu vào DataGrid Khung chương trình
        DgRoadmap.ItemsSource = _allCourses;

            // 3. Đổ dữ liệu môn Đang học hiện tại
        DgCurrentCourses.ItemsSource = _allCourses.Where(c => c.StatusText == "Đang học").ToList();

            // 4. Đổ dữ liệu History (chỉ những môn Đã học / Đang học)
        DgHistory.ItemsSource = _allCourses.Where(c => c.StatusText != "Chưa học").ToList();

            if (TxtCourseStatus != null)
            {
                UpdateDashboard();
            }
        }

    private void UpdateDashboard()
    {
        if (TxtCourseStatus == null) return;
        double totalCredits = _allCourses.Sum(x => x.SoTC);
        double passedCredits = _allCourses.Where(x => x.StatusText == "Đã học").Sum(x => x.SoTC);
        double learningCredits = _allCourses.Where(x => x.StatusText == "Đang học").Sum(x => x.SoTC);
        double progress = totalCredits > 0 ? (passedCredits / totalCredits) * 100 : 0;

        TxtCourseStatus.Text = $"📈 TIẾN ĐỘ HỌC TẬP: Đã tích lũy {passedCredits}/{totalCredits} Tín chỉ ({progress:0.1}%) | Đang học: {learningCredits} Tín chỉ";
    }
    }
}