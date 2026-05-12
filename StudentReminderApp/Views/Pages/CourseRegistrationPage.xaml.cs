using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Pages
{
    public partial class CourseRegistrationPage : Page
    {
        private readonly List<SuggestedCourseViewModel> _selectedCourses;
        private readonly ObservableCollection<LopHocPhan> _registeredCourses;
        private List<ResultClassViewModel> _resultViewModels = new List<ResultClassViewModel>();

        public CourseRegistrationPage(List<SuggestedCourseViewModel> selectedCourses, ObservableCollection<LopHocPhan> registeredCourses)
        {
            InitializeComponent();
            _selectedCourses = selectedCourses;
            _registeredCourses = registeredCourses;
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateResults();
        }

        private async void GenerateResults()
        {
            await Task.Delay(100); // Giả lập delay nhỏ

            _resultViewModels.Clear();
            var random = new Random();

            var courseList = _selectedCourses.Select(x => x.Course).ToList();
            string selectedGroup = (CmbClassGroup.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "24.Nh99";

            string[] lecturers = { "PGS.TS Phạm Thị Hoa", "TS. Nguyễn Văn Hùng", "ThS. Trần Thị Lan", "ThS. Lê Văn Minh" };
            string[] rooms = { "A101", "A102", "B201", "C301", "C302" };
            string[] days = { "Thứ 2 (Tiết 1-3)", "Thứ 3 (Tiết 4-6)", "Thứ 4 (Tiết 7-9)", "Thứ 5 (Tiết 1-3)", "Thứ 6 (Tiết 4-6)" };

            List<string> occupiedSlots = new List<string>();

            foreach (var course in courseList)
            {
                string day = days[random.Next(days.Length)];
                bool isConflict = occupiedSlots.Contains(day);
                occupiedSlots.Add(day);

                _resultViewModels.Add(new ResultClassViewModel
                {
                    OriginalCourse = course,
                    TenMonHoc = course.TenMonHoc,
                    IdLopHp = $"{selectedGroup}-{course.IdLopHp % 1000}",
                    TenGiangVien = random.NextDouble() > 0.5 ? course.TenGiangVien : lecturers[random.Next(lecturers.Length)],
                    SoTinChi = course.SoTinChi,
                    TenPhong = rooms[random.Next(rooms.Length)],
                    NgayThu = day,
                    TuanHoc = "1-15",
                    IsConflict = isConflict
                });
            }

            ApplyFiltersAndSort();

            int totalExpectedTc = _resultViewModels.Sum(x => x.SoTinChi);
            TxtTotalNewCredits.Text = $"Tổng số tín chỉ dự kiến: {totalExpectedTc} TC";

            if (CreditStatusBorder != null)
            {
                if (totalExpectedTc < 15)
                    CreditStatusBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // Xanh lá
                else if (totalExpectedTc <= 20)
                    CreditStatusBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)); // Vàng
                else
                    CreditStatusBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)); // Đỏ
            }
        }

        private void ApplyFiltersAndSort()
        {
            if (DgResultClasses == null) return;

            var list = _resultViewModels.ToList();

            if (CmbTeacherFilter != null && CmbTeacherFilter.SelectedIndex > 0)
            {
                if (CmbTeacherFilter.SelectedItem is ComboBoxItem cbItem && cbItem.Content != null)
                {
                    string selectedTeacher = cbItem.Content.ToString();
                    list = list.OrderByDescending(c => c.TenGiangVien == selectedTeacher).ToList();
                }
            }
            else
            {
                list = list.OrderBy(c => c.NgayThu).ToList();
            }

            if (DgResultClasses != null)
            {
                DgResultClasses.ItemsSource = list;
            }
        }

        private void QuickFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (CmbTeacherFilter == null) return;
            if (IsLoaded) ApplyFiltersAndSort();
        }

        private void CmbClassGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) GenerateResults();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            GenerateResults();
        }

        private void BtnConfirmAll_Click(object sender, RoutedEventArgs e)
        {
            if (_resultViewModels.Any(r => r.IsConflict))
            {
                MessageBox.Show("⚠️ Có các môn học bị trùng lịch! Vui lòng làm mới hoặc chọn lại hệ lớp khác.", "Lỗi trùng lịch", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int currentTc = _registeredCourses.Sum(c => c.SoTinChi);
            int totalNewTc = _resultViewModels.Sum(x => x.SoTinChi);

            if (currentTc + totalNewTc > 25)
            {
                MessageBox.Show("⚠️ Không thể đăng ký! Vượt quá giới hạn tối đa 25 tín chỉ cho học kỳ này.", "Lỗi vượt quá tín chỉ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int successCount = 0;
            foreach (var result in _resultViewModels)
            {
                if (!_registeredCourses.Any(c => c.TenMonHoc == result.TenMonHoc))
                {
                    var lhp = new LopHocPhan
                    {
                        IdLopHp = result.OriginalCourse.IdLopHp,
                        MaMonHoc = result.OriginalCourse.MaMonHoc,
                        TenMonHoc = result.TenMonHoc,
                        SoTinChi = result.SoTinChi,
                        TenGiangVien = result.TenGiangVien,
                        TenPhong = result.TenPhong
                    };
                    _registeredCourses.Add(lhp);
                    successCount++;
                }
            }

            if (successCount > 0)
            {
                MessageBox.Show($"Đăng ký thành công {successCount} môn học!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                if (NavigationService != null && NavigationService.CanGoBack) NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("Các môn học này đã được đăng ký trước đó.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DgResultClasses_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Hiển thị cột STT (Số thứ tự)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}