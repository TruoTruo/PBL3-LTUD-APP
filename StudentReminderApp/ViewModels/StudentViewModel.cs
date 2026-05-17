using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using StudentReminderApp.BLL;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.ViewModels
{
    public class ClassItem
    {
        public long?  IdLop  { get; set; }
        public string TenLop { get; set; } = string.Empty;
    }

    public class StudentViewModel : BaseViewModel
    {
        private readonly StudentBLL _bll = new StudentBLL();

        private readonly ObservableCollection<StudentModel> _allStudents = new();
        private ICollectionView _studentsView = null!;

        public ICollectionView StudentsView
        {
            get => _studentsView;
            private set { _studentsView = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ClassItem> ClassList { get; } = new();

        // ── BỘ LỌC ───────────────────────────────────────────────
        private ClassItem? _selectedClass;
        public ClassItem? SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; OnPropertyChanged(); RefreshFilter(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); RefreshFilter(); }
        }

        private bool _showBannedOnly;
        public bool ShowBannedOnly
        {
            get => _showBannedOnly;
            set
            {
                _showBannedOnly = value; OnPropertyChanged();
                if (value && _showUnverifiedOnly) { _showUnverifiedOnly = false; OnPropertyChanged(nameof(ShowUnverifiedOnly)); }
                RefreshFilter();
            }
        }

        private bool _showUnverifiedOnly;
        public bool ShowUnverifiedOnly
        {
            get => _showUnverifiedOnly;
            set
            {
                _showUnverifiedOnly = value; OnPropertyChanged();
                if (value && _showBannedOnly) { _showBannedOnly = false; OnPropertyChanged(nameof(ShowBannedOnly)); }
                RefreshFilter();
            }
        }

        private int  _totalCount;
        public  int  TotalCount    { get => _totalCount;    set { _totalCount    = value; OnPropertyChanged(); } }
        private int  _filteredCount;
        public  int  FilteredCount { get => _filteredCount; set { _filteredCount = value; OnPropertyChanged(); } }
        private bool _isLoading;
        public  bool IsLoading     { get => _isLoading;     set { _isLoading     = value; OnPropertyChanged(); } }

        // ── COMMANDS ─────────────────────────────────────────────
        public ICommand Ban24hCommand       { get; }
        public ICommand BanPermanentCommand { get; }
        public ICommand UnbanCommand        { get; }
        public ICommand VerifyCommand       { get; }
        public ICommand RefreshCommand      { get; }
        public ICommand ClearFilterCommand  { get; }

        public StudentViewModel()
        {
            StudentsView        = CollectionViewSource.GetDefaultView(_allStudents);
            StudentsView.Filter = ApplyFilter;

            Ban24hCommand = new RelayCommand(
                obj => ExecuteBan(obj as StudentModel, DateTime.Now.AddHours(24)),
                obj => obj is StudentModel sv && !sv.IsBanned);

            BanPermanentCommand = new RelayCommand(
                obj => ExecuteBan(obj as StudentModel, null),
                obj => obj is StudentModel sv && !sv.IsBanned);

            UnbanCommand = new RelayCommand(
                obj => ExecuteUnban(obj as StudentModel),
                obj => obj is StudentModel sv && sv.IsBanned);

            // ── FIX: VerifyCommand — không kiểm tra IsAdmin ở đây,
            //         đã kiểm tra trong BLL rồi. CanExecute chỉ lọc
            //         theo IsVerified để nút/menu không bị disable sai.
            VerifyCommand = new RelayCommand(
                obj => ExecuteVerify(obj as StudentModel),
                obj => obj is StudentModel sv && !sv.IsVerified);

            RefreshCommand     = new RelayCommand(async _ => await LoadDataAsync());
            ClearFilterCommand = new RelayCommand(_ => ClearAllFilters());

            _ = LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var svTask  = Task.Run(() => _bll.GetAllStudents());
                var lopTask = Task.Run(() => _bll.GetAllClasses());
                await Task.WhenAll(svTask, lopTask);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ClassList.Clear();
                    ClassList.Add(new ClassItem { IdLop = null, TenLop = "📋 Tất cả lớp" });
                    foreach (var (id, ten) in lopTask.Result)
                        ClassList.Add(new ClassItem { IdLop = id, TenLop = ten });

                    if (_selectedClass == null) { _selectedClass = ClassList[0]; OnPropertyChanged(nameof(SelectedClass)); }

                    _allStudents.Clear();
                    foreach (var sv in svTask.Result) _allStudents.Add(sv);

                    TotalCount = _allStudents.Count;
                    StudentsView.Refresh();
                    UpdateFilteredCount();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentViewModel.LoadDataAsync: " + ex.Message);
                MessageBox.Show("Lỗi tải dữ liệu sinh viên:\n" + ex.Message,
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        private bool ApplyFilter(object obj)
        {
            if (obj is not StudentModel sv) return false;
            if (_selectedClass?.IdLop != null && sv.IdLop != _selectedClass.IdLop) return false;
            if (_showBannedOnly     && !sv.IsBanned)   return false;
            if (_showUnverifiedOnly &&  sv.IsVerified)  return false;
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string kw = _searchText.Trim().ToLower();
                if (!sv.HoTen.ToLower().Contains(kw) && !sv.Mssv.ToLower().Contains(kw)) return false;
            }
            return true;
        }

        private void RefreshFilter() =>
            Application.Current.Dispatcher.Invoke(() => { StudentsView.Refresh(); UpdateFilteredCount(); });

        private void UpdateFilteredCount()
        {
            int n = 0; foreach (var _ in StudentsView) n++;
            FilteredCount = n;
        }

        private void ClearAllFilters()
        {
            SearchText = string.Empty;
            ShowBannedOnly = false;
            ShowUnverifiedOnly = false;
            if (ClassList.Count > 0) SelectedClass = ClassList[0];
        }

        private void ExecuteBan(StudentModel? sv, DateTime? lockUntil)
        {
            if (sv == null) return;
            string label = lockUntil.HasValue ? "khóa 24 giờ" : "khóa vĩnh viễn";
            if (MessageBox.Show($"Bạn có chắc muốn {label} tài khoản\n\"{sv.HoTen}\" ({sv.Mssv})?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            if (_bll.BanStudent(sv.IdAcc, lockUntil))
            {
                sv.Status    = "Banned";
                sv.LockUntil = lockUntil;
                StudentsView.Refresh(); UpdateFilteredCount();
                MessageBox.Show(lockUntil.HasValue
                    ? $"✅ Đã khóa 24 giờ tài khoản {sv.HoTen}!"
                    : $"✅ Đã khóa vĩnh viễn tài khoản {sv.HoTen}!",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
                MessageBox.Show("❌ Thao tác thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ExecuteUnban(StudentModel? sv)
        {
            if (sv == null) return;
            if (MessageBox.Show($"Mở khóa tài khoản \"{sv.HoTen}\" ({sv.Mssv})?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            if (_bll.UnbanStudent(sv.IdAcc))
            {
                sv.Status = "Active"; sv.LockUntil = null;
                StudentsView.Refresh(); UpdateFilteredCount();
                MessageBox.Show($"✅ Đã mở khóa {sv.HoTen}!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
                MessageBox.Show("❌ Mở khóa thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ── FIX ExecuteVerify ─────────────────────────────────────
        // Sau khi set IsVerified = true, gọi StudentsView.Refresh()
        // để DataGrid redraw cell "Xác thực" ngay lập tức.
        private void ExecuteVerify(StudentModel? sv)
        {
            if (sv == null || sv.IsVerified) return;
            if (MessageBox.Show($"Xác thực tài khoản \"{sv.HoTen}\" ({sv.Mssv})?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            if (_bll.VerifyStudent(sv.IdAcc))
            {
                sv.IsVerified = true;  // triggers OnPropertyChanged → VerifiedIcon, VerifiedColor, VerifiedBg
                StudentsView.Refresh(); // bắt buộc để DataGrid repaint cell
                UpdateFilteredCount();
                MessageBox.Show($"✅ Đã xác thực tài khoản {sv.HoTen}!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
                MessageBox.Show("❌ Xác thực thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
