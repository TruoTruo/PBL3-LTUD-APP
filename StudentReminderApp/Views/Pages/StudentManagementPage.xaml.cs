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
