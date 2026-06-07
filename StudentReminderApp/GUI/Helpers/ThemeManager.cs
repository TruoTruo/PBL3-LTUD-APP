using System;
using System.Windows;

namespace StudentReminderApp.Helpers
{
    public static class ThemeManager
    {
        public static bool IsDarkTheme { get; private set; } = false;

        public static void ApplyTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            var app = Application.Current;
            var dicts = app.Resources.MergedDictionaries;

            var newThemeDict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/StudentReminderApp;component/GUI/Resources/Themes/{(isDark ? "DarkTheme" : "LightTheme")}.xaml", UriKind.Absolute)
            };

            // Remove all dictionaries except Styles.xaml
            for (int i = dicts.Count - 1; i >= 0; i--)
            {
                var dict = dicts[i];
                if (dict.Source != null && !dict.Source.ToString().Contains("Styles.xaml"))
                {
                    dicts.RemoveAt(i);
                }
            }

            // Insert new theme
            dicts.Add(newThemeDict);
        }

        public static void ToggleTheme()
        {
            ApplyTheme(!IsDarkTheme);
        }
    }
}
