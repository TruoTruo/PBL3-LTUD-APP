using System;
using System.IO;

#nullable disable

namespace StudentReminderApp.Properties
{
    // Lớp cấu hình tự viết để thay thế cho Settings mặc định, dùng file Text để lưu trữ.
    public class Settings
    {
        // Chuyển sang lưu ở thư mục AppData của Windows để không bị mất khi Build lại
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudentReminderApp");
        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "user_settings.txt");
        private static Settings _default;

        public static Settings Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new Settings();
                    if (File.Exists(SettingsFilePath))
                    {
                        try
                        {
                            string[] lines = File.ReadAllLines(SettingsFilePath);
                            if (lines.Length >= 3)
                            {
                                _default.Username = lines[0];
                                _default.Password = lines[1];
                                bool.TryParse(lines[2], out bool rem);
                                _default.RememberMe = rem;
                            }
                        }
                        catch { }
                    }
                }
                return _default;
            }
        }

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;

        public void Save()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }
                string[] lines = { Username ?? "", Password ?? "", RememberMe.ToString() };
                File.WriteAllLines(SettingsFilePath, lines);
            }
            catch { }
        }
    }
}