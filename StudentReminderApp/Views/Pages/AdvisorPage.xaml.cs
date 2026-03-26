using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Pages
{
    public partial class AdvisorPage : Page
    {
        private readonly AdvisorBLL _bll       = new AdvisorBLL();
        private readonly CourseBLL  _courseBll = new CourseBLL();

        public AdvisorPage() { InitializeComponent(); Loaded += (s, e) => Analyze(); }

        private void Analyze()
        {
            int    hk   = CmbHocKy.SelectedIndex + 1;
            string nh   = TxtNamHoc.Text.Trim();
            long   idSv = SessionManager.CurrentAccount.IdAcc;

            var summary = _bll.GetSummary(idSv, hk, nh);
            TxtAccCredit.Text  = summary.TotalAccumulatedCredits.ToString();
            TxtRegCredit.Text  = summary.RegisteredCreditsThisTerm.ToString();
            TxtRemaining.Text  = $"Còn có thể đăng ký: {summary.RemainingCredits} TC";
            TxtGpa.Text        = summary.GPAFormatted;
            TxtGpaLevel.Text   = summary.GPALevel;

            // Progress bar
            double pct = summary.MaxCreditsAllowed > 0
                ? (double)summary.RegisteredCreditsThisTerm / summary.MaxCreditsAllowed : 0;
            TxtCreditFraction.Text = $"{summary.RegisteredCreditsThisTerm}/{summary.MaxCreditsAllowed} TC";
            TxtProgressLabel.Text  = $"Đã dùng {pct * 100:F0}% giới hạn đăng ký học kỳ";

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, (System.Action)(() =>
            {
                double w = (ProgressBar.Parent as Border)?.ActualWidth - 24 ?? 300;
                ProgressBar.Width = w * pct;
            }));

            // Suggested
            var suggested = _bll.GetSuggestedCourses(idSv, hk, nh);
            TxtSuggestCount.Text = suggested.Count.ToString();
            if (suggested.Count > 0)
            {
                DgSuggested.ItemsSource = suggested;
                DgSuggested.Visibility  = Visibility.Visible;
                TxtNoSuggest.Visibility = Visibility.Collapsed;
            }
            else
            {
                DgSuggested.Visibility  = Visibility.Collapsed;
                TxtNoSuggest.Visibility = Visibility.Visible;
            }

            // Registered
            RegisteredList.ItemsSource = _bll.GetRegisteredCourses(idSv, hk, nh);
        }

        private void BtnAnalyze_Click(object sender, RoutedEventArgs e) => Analyze();

        private void BtnQuickRegister_Click(object sender, RoutedEventArgs e)
        {
            var lhp = (LopHocPhan)((Button)sender).Tag;
            var (ok, msg) = _courseBll.Register(SessionManager.CurrentAccount.IdAcc, lhp.IdLopHp);
            if (!ok) { MessageBox.Show(msg, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            MessageBox.Show($"Đã đăng ký môn {lhp.TenMonHoc} thành công!", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Analyze();
        }
    }
}
