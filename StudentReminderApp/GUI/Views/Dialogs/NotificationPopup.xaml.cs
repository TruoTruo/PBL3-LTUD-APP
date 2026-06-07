using System;
using System.Windows;
using System.Windows.Threading;

namespace StudentReminderApp.Views.Dialogs
{
    public partial class NotificationPopup : Window
    {
        public NotificationPopup(string title, string body)
        {
            InitializeComponent();
            TxtPopupTitle.Text = title;
            TxtPopupBody.Text  = body;

            var wa = SystemParameters.WorkArea;
            Left = wa.Right  - Width  - 20;
            Top  = wa.Bottom - Height - 20;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, e) => { timer.Stop(); Close(); };
            timer.Start();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
