using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace StudentReminderApp.Converters
{
    /// <summary>
    /// Ngược lại BooleanToVisibilityConverter:
    /// true  → Collapsed
    /// false → Visible
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Chuyển ApprovalStatus (int) → màu Background Badge:
    /// 0 (Chờ) → Cam
    /// 1 (Đã duyệt) → Xanh lá
    /// 2 (Từ chối) → Đỏ
    /// </summary>
    [ValueConversion(typeof(int), typeof(Brush))]
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int status)
            {
                return status switch
                {
                    0 => new SolidColorBrush(Color.FromRgb(255, 152, 0)),  // Orange
                    1 => new SolidColorBrush(Color.FromRgb(76,  175, 80)), // Green
                    2 => new SolidColorBrush(Color.FromRgb(244, 67,  54)), // Red
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Chuyển ApprovalStatus (int) → chuỗi hiển thị badge
    /// </summary>
    [ValueConversion(typeof(int), typeof(string))]
    public class StatusToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int status
                ? status switch
                {
                    0 => "⏳ Chờ duyệt",
                    1 => "✅ Đã duyệt",
                    2 => "❌ Từ chối",
                    _ => "Không rõ"
                }
                : "Không rõ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// NullToVisibilityConverter: null → Collapsed, không null → Visible
    /// Dùng để ẩn phần "Bài gốc" khi OriginalPost == null
    /// </summary>
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
