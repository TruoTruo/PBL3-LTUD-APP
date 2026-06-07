using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace StudentReminderApp.Views.Pages
{
    public partial class StudentManagementPage : UserControl
    {
        public StudentManagementPage()
        {
            InitializeComponent();
        }

        private void dgStudents_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Thiết lập Header của row là số thứ tự (Index + 1)
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void BtnViewIdCard_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Models.StudentModel student)
            {
                string idCardDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "idcards");
                string targetPath = null;
                string[] exts = { ".png", ".jpg", ".jpeg" };
                foreach (var ext in exts)
                {
                    string p = System.IO.Path.Combine(idCardDir, $"idcard_{student.IdAcc}{ext}");
                    if (System.IO.File.Exists(p))
                    {
                        targetPath = p;
                        break;
                    }
                }

                if (targetPath != null)
                {
                    var window = new System.Windows.Window
                    {
                        Title = $"Thẻ sinh viên - {student.HoTen} ({student.Mssv})",
                        Width = 600,
                        Height = 400,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                        Content = new Image
                        {
                            Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(targetPath, UriKind.Absolute)),
                            Stretch = System.Windows.Media.Stretch.Uniform
                        }
                    };
                    window.ShowDialog();
                }
                else
                {
                    System.Windows.MessageBox.Show("Sinh viên này chưa tải lên ảnh thẻ.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
        }
    }

    // ── Converter: trả về true khi chuỗi không rỗng ──────────────
    // Dùng trong Trigger của SearchBox để ẩn watermark khi có ký tự.
    public class IsNotEmptyConverter : IValueConverter
    {
        public static readonly IsNotEmptyConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string s && s.Length > 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ── Converter STT (giữ lại để tương thích nếu có nơi khác dùng) ──
    public class IndexPlusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int index ? index + 1 : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
