using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.Views.Main
{
    public partial class ThemeToggleButton : UserControl
    {
        public ThemeToggleButton()
        {
            InitializeComponent();
            ThemeSwitch.IsChecked = ThemeManager.IsDarkTheme;
        }

        private void ThemeSwitch_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ToggleTheme();
        }
    }
}
