using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Navigation;
using System.Threading.Tasks;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;
using StudentReminderApp.Services;

namespace StudentReminderApp.Views.Pages
{
    public class AiCourseItem
    {
        public string IndexStr { get; set; } = string.Empty;
        public string TenMonHoc { get; set; } = string.Empty;
        public string GiangVien { get; set; } = string.Empty;
        public string Buoi { get; set; } = string.Empty;
    }

    public partial class AdvisorPage : Page
    {
        private int _currentWizardStep = 1;

        private ObservableCollection<LopHocPhan> _mockRegisteredCourses = new ObservableCollection<LopHocPhan>();
        private Dictionary<long, Button> _registerButtons = new Dictionary<long, Button>();
        private List<LopHocPhan> _currentLoadedCourses = new List<LopHocPhan>();
        private string _currentProgramId = "ai";

        private readonly AdvisorBLL _advisorBll = new AdvisorBLL();

        public AdvisorPage()
        {
            InitializeComponent();
            DataService.Initialize(); // Khởi tạo Load file JSON ngay khi mở trang
            Loaded += AdvisorPage_Loaded;
        }

        private async void AdvisorPage_Loaded(object sender, RoutedEventArgs e)
        {
            await AnalyzeDataAsync(saveManualInput: false); // Fix: Không lưu đè dữ liệu rác mặc định khi vừa mở trang
        }

        private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            await AnalyzeDataAsync(saveManualInput: true); // Chỉ lưu khi người dùng chủ động bấm nút
        }

        private bool _isLoading = false;

        private async Task AnalyzeDataAsync(bool saveManualInput)
        {
            if (!SessionManager.IsLoggedIn || _isLoading) return;
            _isLoading = true;
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                await Task.Delay(300); // Giả lập delay tải dữ liệu mượt mà

                long idSv = SessionManager.CurrentUser.IdAcc;
                int hocKy = (CmbHocKy != null && CmbHocKy.SelectedIndex >= 0) ? CmbHocKy.SelectedIndex + 1 : 1;
                string namHoc = (CmbNamHoc != null && CmbNamHoc.SelectedItem is ComboBoxItem item) ? item.Content?.ToString() ?? "2024-2025" : "2024-2025";

                if (saveManualInput)
                {
                    // Chỉ lưu GPA và Tín chỉ nếu người dùng có sửa tay
                    string rawGpa = TxtGpa.Text.Trim().Replace(",", ".");
                    if (double.TryParse(rawGpa, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double gpa) && int.TryParse(TxtAccCredit.Text.Trim(), out int cred))
                    {
                        await _advisorBll.UpdateManualStatsAsync(idSv, gpa, cred);
                    }
                }

                // [Fix Lỗi Nghiêm Trọng]: Xóa bỏ hoàn toàn gọi API Mock từ DB cũ, sử dụng 100% JSON
                ApplyMockData(hocKy);

                // --- CẬP NHẬT CÂU CHÀO TRANG 1 ---
                TxtStep1Greeting.Text = $"🤖 Trợ lý AI - Tổng quan tiến độ\nGPA hiện tại: {TxtGpa.Text} | Tích lũy: {TxtAccCredit.Text} TC. Bạn muốn đăng ký học phần cho học kỳ nào?";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi phân tích dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private async void ProgramTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                if (_currentProgramId == btn.Tag.ToString()) return; // Tránh load lại nếu click nút đang chọn

                _currentProgramId = btn.Tag.ToString();
                UpdateTabStyles(btn);

                if (IsLoaded && !_isLoading)
                {
                    // Hard Reset toàn bộ số liệu về 0
                    _mockRegisteredCourses.Clear();
                    _currentLoadedCourses.Clear();
                    DgSuggested.ItemsSource = null;
                    TxtAccCredit.Text = "0";
                    TxtGpa.Text = "0.0";
                    TxtSuggestCount.Text = "0";
                    UpdateProgressBar(0);

                    await AnalyzeDataAsync(saveManualInput: false);
                }
            }
        }

        private void UpdateTabStyles(Button activeBtn)
        {
            var inactiveBg = System.Windows.Media.Brushes.Transparent;
            var inactiveFg = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
            var activeBg = System.Windows.Media.Brushes.White;
            var activeFg = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235)); // #2563eb

            Button[] tabs = { BtnTabNhat, BtnTabDacthu, BtnTabAi };
            foreach (var tab in tabs)
            {
                bool isActive = (tab == activeBtn);
                tab.Background = isActive ? activeBg : inactiveBg;
                tab.Foreground = isActive ? activeFg : inactiveFg;
                tab.Effect = isActive ? new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Color = System.Windows.Media.Colors.Black, Opacity = 0.05 } : null;
            }
        }

        private void QuickFilter_Changed(object? sender, RoutedEventArgs? e)
        {
            if (RbPriorityTeacher == null || CmbTeacherFilter == null || DgSuggested == null) return;

            CmbTeacherFilter.Visibility = RbPriorityTeacher.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            var list = _currentLoadedCourses != null ? _currentLoadedCourses.ToList() : new List<LopHocPhan>();

            if (RbPrioritySubject != null && RbPrioritySubject.IsChecked == true)
            {
                list = list.OrderByDescending(c => c.SoTinChi).ThenBy(c => c.TenMonHoc).ToList();
            }
            else if (RbPriorityTeacher != null && RbPriorityTeacher.IsChecked == true && CmbTeacherFilter.SelectedIndex > 0)
            {
                if (CmbTeacherFilter.SelectedItem is ComboBoxItem cbItem && cbItem.Content != null)
                {
                    string selectedTeacher = cbItem.Content.ToString();
                    list = list.OrderByDescending(c => c.TenGiangVien == selectedTeacher).ThenBy(c => c.TenMonHoc).ToList();
                }
            }

            DgSuggested.ItemsSource = list;
        }

        private void UpdateProgressBar(int registeredCredits)
        {
            TxtRegCredit.Text = registeredCredits.ToString();
            TxtCreditFraction.Text = $"{registeredCredits} / 25 TC";

            double percent = (double)registeredCredits / 25 * 100;
            if (percent > 100) percent = 100;
            CreditProgressBar.Width = percent * 2.5; // Giả sử chiều rộng tối đa là 250px
            if (BottomCreditProgressBar != null) BottomCreditProgressBar.Width = percent * 2.5;

            // Đổi màu thanh Đăng ký tín chỉ: Xanh (An toàn), Vàng (Cảnh báo), Đỏ (Tối đa)
            if (registeredCredits >= 22)
            {
                CreditProgressBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)); // Đỏ
                if (BottomCreditProgressBar != null) BottomCreditProgressBar.Background = CreditProgressBar.Background;
                if (TxtCreditFraction != null) TxtCreditFraction.Foreground = CreditProgressBar.Background;
            }
            else if (registeredCredits >= 15)
            {
                CreditProgressBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)); // Vàng
                if (BottomCreditProgressBar != null) BottomCreditProgressBar.Background = CreditProgressBar.Background;
                if (TxtCreditFraction != null) TxtCreditFraction.Foreground = CreditProgressBar.Background;
            }
            else
            {
                CreditProgressBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // Xanh
                if (BottomCreditProgressBar != null) BottomCreditProgressBar.Background = CreditProgressBar.Background;
                if (TxtCreditFraction != null) TxtCreditFraction.Foreground = CreditProgressBar.Background;
            }

            // Auto-Grayout: Tự động kiểm tra và làm xám các nút Đăng ký nếu vượt 25 TC
            foreach (var kvp in _registerButtons)
            {
                var btn = kvp.Value;
                if (btn != null && btn.Tag is LopHocPhan lhp)
                {
                    if (_mockRegisteredCourses.Contains(lhp))
                    {
                        btn.IsEnabled = false; // Môn đã đăng ký -> Xám (Khóa)
                    }
                    else
                    {
                        btn.IsEnabled = (registeredCredits + lhp.SoTinChi <= 25); // Nếu đăng ký thêm mà lố 25 TC -> Xám (Khóa)
                    }
                }
            }
        }

        private async void BtnAutoRegister_Click(object sender, RoutedEventArgs e)
        {
            if (!SessionManager.IsLoggedIn) return;

            try
            {
                var list = DgSuggested.ItemsSource as List<LopHocPhan>;
                if (list == null || list.Count == 0)
                {
                    MessageBox.Show("Không có môn học gợi ý nào để đăng ký.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                long idSv = SessionManager.CurrentUser.IdAcc;
                int successCount = 0;
                int currentTc = 0;
                int.TryParse(TxtRegCredit.Text, out currentTc);

                foreach (var lhp in list)
                {
                    if (currentTc + lhp.SoTinChi > 25) continue; // Khống chế mức 25 TC

                    // Sử dụng mảng RAM làm Data source (Không gọi API DB như yêu cầu)
                    if (!_mockRegisteredCourses.Contains(lhp)) _mockRegisteredCourses.Add(lhp);
                    bool ok = true;

                    if (ok)
                    {
                        successCount++;
                        currentTc += lhp.SoTinChi;
                    }
                }

                if (successCount > 0)
                {
                    MessageBox.Show($"AI đã tự động đăng ký thành công {successCount} môn học dựa vào mức ưu tiên của bạn!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateProgressBar(currentTc);
                }
                else
                {
                    MessageBox.Show("Không thể tự động đăng ký môn nào. Có thể đã trùng giờ, vượt mức tín chỉ hoặc đã đủ số lượng đăng ký.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tự động đăng ký: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnQuickRegister_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is LopHocPhan lhp)
            {
                int currentTc = 0;
                int.TryParse(TxtRegCredit.Text, out currentTc);

                // Kiểm tra giới hạn tín chỉ cứng (25 TC)
                if (currentTc + lhp.SoTinChi > 25)
                {
                    MessageBox.Show("⚠️ Không thể đăng ký! Bạn đã đạt giới hạn tối đa 25 tín chỉ cho học kỳ này.",
                                    "Lỗi vượt quá tín chỉ", MessageBoxButton.OK, MessageBoxImage.Error);
                    btn.IsEnabled = false; // Vô hiệu hóa nút của môn này
                    return;
                }

                btn.IsEnabled = false;

                try
                {
                    // Nạp Object môn học vào mảng selectedCourses và cập nhật UI Realtime
                    _mockRegisteredCourses.Add(lhp);
                    currentTc += lhp.SoTinChi;
                    UpdateProgressBar(currentTc);

                    // Cập nhật lại thanh Progress thay vì báo Alert liên tục gây khó chịu UX
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi đăng ký: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    btn.IsEnabled = true;
                }
            }
        }

        private void BtnQuickRegister_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is LopHocPhan lhp)
            {
                // Lưu cache tham chiếu các nút vào Dictionary lúc tải lên giao diện
                _registerButtons[lhp.IdLopHp] = btn;

                int currentTc = 0;
                int.TryParse(TxtRegCredit.Text, out currentTc);

                if (_mockRegisteredCourses.Contains(lhp))
                {
                    btn.IsEnabled = false;
                }
                else
                {
                    btn.IsEnabled = (currentTc + lhp.SoTinChi <= 25);
                }
            }
        }

        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }

        private async void BtnRemoveRegistered_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is LopHocPhan lhp)
            {
                int currentTc = 0;
                int.TryParse(TxtRegCredit.Text, out currentTc);

                if (_mockRegisteredCourses.Contains(lhp))
                {
                    // Fade out effect
                    var container = FindParent<Border>(btn);
                    if (container != null)
                    {
                        var anim = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
                        container.BeginAnimation(UIElement.OpacityProperty, anim);
                        await Task.Delay(200);
                    }

                    // (2) Xóa vĩnh viễn thẻ Card khỏi danh sách UI
                    _mockRegisteredCourses.Remove(lhp);

                    // (1) Trừ số tín chỉ và cập nhật Progress Bar realtime
                    currentTc -= lhp.SoTinChi;
                    UpdateProgressBar(currentTc);

                    // Phục hồi nút [+ Đăng ký] bên trái
                    if (_registerButtons.ContainsKey(lhp.IdLopHp))
                    {
                        var regBtn = _registerButtons[lhp.IdLopHp];
                        if (regBtn != null) regBtn.IsEnabled = true;
                    }
                }
            }
        }

        // --- XỬ LÝ LOGIC KỊCH BẢN WIZARD (3 BƯỚC) ---
        private void UpdateWizardUI()
        {
            if (RegistrationGrid != null)
            {
                RegistrationGrid.Visibility = _currentWizardStep > 1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ApplyMockData(int hk)
        {
            int accCredit = 0;
            double gpa = 0.0;

            // [Task 2: Hard Reset] Làm rỗng mảng selectedCourses
            _mockRegisteredCourses.Clear();
            _registerButtons.Clear();
            RegisteredList.ItemsSource = _mockRegisteredCourses;

            // Nạp dữ liệu từ JSON thông qua DataService
            var programData = DataService.GetProgramData(_currentProgramId);
            var courses = DataService.GetCoursesBySemester(_currentProgramId, hk) ?? new List<CourseData>();

            // Tự động cộng dồn tín chỉ từ các kỳ trước dựa trên file JSON
            if (programData != null && programData.Semesters != null)
            {
                // Fix: Ngăn chặn Crash NullReferenceException nếu có Học kỳ bị rỗng môn học
                accCredit = programData.Semesters.Where(s => s != null && s.Semester < hk && s.Courses != null)
                                                 .SelectMany(s => s.Courses!)
                                                 .Sum(c => (int)c.Credits);
            }
            gpa = hk > 1 ? 3.2 : 0.0;

            // Cập nhật text tổng tín chỉ chương trình (Ví dụ: / 180 TC)
            int totalProgramCredits = programData?.ProgramInfo?.TotalCredits ?? 180;
            if (TxtTotalCreditProgram != null)
                TxtTotalCreditProgram.Text = $"/ {totalProgramCredits} TC chương trình";

            // 2. Cập nhật các chỉ số (Stats) trên giao diện chính
            TxtAccCredit.Text = accCredit.ToString();
            TxtGpa.Text = gpa.ToString("0.0#");
            TxtSuggestCount.Text = courses.Count.ToString();

            // Cập nhật thanh màu GPA và hiển thị mức giới hạn TC
            double gpaPercent = (gpa / 4.0) * 100;
            if (gpaPercent > 100) gpaPercent = 100;
            GpaProgressBar.Width = gpaPercent * 1.5; // Thanh bar rộng khoảng 150px

            if (gpa >= 3.2)
            {
                TxtGpaLevel.Text = "Tốt (Tối đa 25 TC)";
                GpaProgressBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // Xanh
            }
            else if (gpa >= 2.0)
            {
                TxtGpaLevel.Text = "Khá (Tối đa 22 TC)";
                GpaProgressBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)); // Vàng
            }
            else
            {
                TxtGpaLevel.Text = "Cảnh báo (Tối đa 14 TC)";
                GpaProgressBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)); // Đỏ
            }

            // [Task 2: Hard Reset] Thanh Progress Bar thiết lập lại 0% (Hành động này bao gồm hàm UpdateProgressBar(0))
            UpdateProgressBar(0);
            TxtRemaining.Text = $"Còn lại: {totalProgramCredits - accCredit} TC";

            // 3. Cập nhật DataGrid (Bảng danh sách gợi ý lớn)
            _currentLoadedCourses.Clear();

            foreach (var c in courses)
            {
                var lhp = new LopHocPhan
                {
                    IdLopHp = Math.Abs((c.Id ?? Guid.NewGuid().ToString()).GetHashCode()), // Fix: Tránh null ID
                    MaMonHoc = c.Id ?? Guid.NewGuid().ToString(),
                    TenMonHoc = c.Name ?? "Chưa có tên",
                    SoTinChi = (int)c.Credits,
                    TenGiangVien = c.Lecturer ?? "Chưa phân công",
                    TenPhong = c.Session ?? "Chưa xếp phòng"
                };
                _currentLoadedCourses.Add(lhp);
            }

            // Kích hoạt bộ lọc ban đầu
            QuickFilter_Changed(null, null);

            TxtNoSuggest.Visibility = _currentLoadedCourses.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnAiChonHk_Click(object sender, RoutedEventArgs e)
        {
            int hk = (CmbHocKy != null && CmbHocKy.SelectedIndex >= 0) ? CmbHocKy.SelectedIndex + 1 : 1;
            ApplyMockData(hk);
            _currentWizardStep = 2;
            UpdateWizardUI();
        }

        private void BtnWizardHome_Click(object sender, RoutedEventArgs e)
        {
            _currentWizardStep = 1;
            UpdateWizardUI();
        }

        private void BtnWizardReset_Click(object sender, RoutedEventArgs e)
        {
            _mockRegisteredCourses.Clear();
            UpdateProgressBar(0);

            // Khôi phục toàn bộ trạng thái nút Đăng ký
            foreach (var btn in _registerButtons.Values)
            {
                if (btn != null) btn.IsEnabled = true;
            }

            _currentWizardStep = 1;
            UpdateWizardUI();
        }
    }
}