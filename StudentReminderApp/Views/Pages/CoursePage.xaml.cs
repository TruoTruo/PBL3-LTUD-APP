using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;
using StudentReminderApp.Services;

namespace StudentReminderApp.Views.Pages
{
    public class CurriculumItem
    {
        public int HocKy { get; set; }
        public string MaHocPhan { get; set; }
        public string TenHocPhan { get; set; }
        public double SoTC { get; set; }
        public string StatusText { get; set; }  // Đã học, Đang học, Chưa học
        public string BgColor { get; set; }     // Flat Background Color
        public string FgColor { get; set; }     // Flat Foreground Text Color
        public string DiemChu { get; set; }
        public double DiemSo { get; set; }
        public string GiangVien { get; set; } = "N/A";
        public string ThoiKhoaBieu { get; set; } = "N/A";
        public string TuanHoc { get; set; } = "1-15";
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

            int maxSemester = (CmbHeDaoTao != null && CmbHeDaoTao.SelectedIndex == 0) ? 8 : 10;
            var filteredCourses = new List<CurriculumItem>();

            foreach (var sem in programData.Semesters.Where(s => s != null && s.Semester <= maxSemester))
            {
                if (sem.Courses == null) continue;
                foreach (var c in sem.Courses)
                {
                    // Giả lập trạng thái theo kỳ học
                    string status = sem.Semester < 4 ? "Đã học" : (sem.Semester == 4 ? "Đang học" : "Chưa học");

                    var item = new CurriculumItem
                    {
                        HocKy = sem.Semester,
                        MaHocPhan = c.Id ?? "N/A",
                        TenHocPhan = c.Name ?? "N/A",
                        SoTC = c.Credits,
                        StatusText = status,
                        GiangVien = c.Lecturer ?? "Chưa phân công",
                        ThoiKhoaBieu = c.Session ?? "Chưa sắp xếp",
                        TuanHoc = c.Weeks ?? "1-15"
                    };

                    if (status == "Đã học") { item.BgColor = "#DBEAFE"; item.FgColor = "#1E3A8A"; item.DiemChu = "B+"; item.DiemSo = 3.5; }
                    else if (status == "Đang học") { item.BgColor = "#FEF3C7"; item.FgColor = "#92400E"; item.DiemChu = "-"; item.DiemSo = 0; }
                    else { item.BgColor = "#F1F5F9"; item.FgColor = "#475569"; item.DiemChu = "-"; item.DiemSo = 0; }

                    filteredCourses.Add(item);
                }
            }

            // 2. Đổ dữ liệu vào DataGrid Khung chương trình
            DgRoadmap.ItemsSource = filteredCourses;

            // 3. Đổ dữ liệu môn Đang học hiện tại (Phần 1)
            DgCurrentCourses.ItemsSource = filteredCourses.Where(c => c.StatusText == "Đang học").ToList();

            // 4. Đổ dữ liệu History (chỉ những môn Đã học / Đang học)
            DgHistory.ItemsSource = filteredCourses.Where(c => c.StatusText != "Chưa học").ToList();
        }
    }
}