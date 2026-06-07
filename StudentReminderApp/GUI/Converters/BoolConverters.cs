using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StudentReminderApp.Converters
{
    public class BoolToBgConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => (bool)v
                ? new SolidColorBrush(Color.FromRgb(236,253,245))
                : new SolidColorBrush(Color.FromRgb(241,245,249));
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class BoolToFgConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => (bool)v
                ? new SolidColorBrush(Color.FromRgb(16,185,129))
                : new SolidColorBrush(Color.FromRgb(100,116,139));
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => (bool)v ? "Đã đăng ký" : "Chưa đăng ký";
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class BoolToActionConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, CultureInfo c)
            => (bool)v ? "Hủy ĐK" : "Đăng ký";
        public object ConvertBack(object v, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }
}
