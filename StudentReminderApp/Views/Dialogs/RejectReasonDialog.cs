using System.Windows;
using System.Windows.Controls;

namespace StudentReminderApp.Views.Dialogs
{
    /// <summary>
    /// Dialog đơn giản để Admin nhập lý do từ chối bài viết.
    /// Không cần file XAML riêng — tạo UI hoàn toàn bằng code-behind.
    /// </summary>
    public class RejectReasonDialog : Window
    {
        private readonly TextBox _reasonBox;

        public string Reason => _reasonBox.Text?.Trim() ?? string.Empty;

        public RejectReasonDialog(string postTitle)
        {
            Title = "Từ chối bài viết";
            Width = 440;
            Height = 230;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = $"Lý do từ chối bài \"{Truncate(postTitle, 40)}\":",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            });

            _reasonBox = new TextBox
            {
                Height = 70,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 14),
                Text = "Vi phạm nội quy diễn đàn."
            };
            panel.Children.Add(_reasonBox);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnConfirm = new Button
            {
                Content = "Từ chối bài",
                Width = 110,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(244, 67, 54)), // đỏ
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnConfirm.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_reasonBox.Text))
                {
                    MessageBox.Show("Vui lòng nhập lý do từ chối!", "Thiếu thông tin",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DialogResult = true;
                Close();
            };

            var btnCancel = new Button
            {
                Content = "Hủy",
                Width = 70,
                Height = 34
            };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            btnPanel.Children.Add(btnConfirm);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);

            Content = panel;

            // Focus vào TextBox khi mở
            Loaded += (s, e) =>
            {
                _reasonBox.Focus();
                _reasonBox.SelectAll();
            };
        }

        private static string Truncate(string text, int maxLen)
            => text?.Length > maxLen ? text.Substring(0, maxLen) + "…" : text ?? "";
    }
}
