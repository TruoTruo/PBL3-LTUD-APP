using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using StudentReminderApp.BLL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.Views.Pages
{
    public partial class CoursePage : Page
    {
        private readonly CourseBLL _bll = new CourseBLL();
        private List<LopHocPhan>   _allCourses;

        public CoursePage() { InitializeComponent(); Loaded += (s, e) => LoadData(); }

        private void LoadData()
        {
            int    hk = CmbHocKy.SelectedIndex + 1;
            string nh = TxtNamHoc.Text.Trim();
            _allCourses           = _bll.GetAvailable(hk, nh, SessionManager.CurrentAccount.IdAcc);
            DgCourses.ItemsSource = _allCourses;
        }

        private void Filter_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_allCourses == null) return;
            string q = TxtSearch.Text.ToLower();
            DgCourses.ItemsSource = string.IsNullOrWhiteSpace(q)
                ? _allCourses
                : _allCourses.Where(c =>
                    c.TenMonHoc.ToLower().Contains(q)    ||
                    c.TenGiangVien.ToLower().Contains(q) ||
                    c.MaMonHoc.ToLower().Contains(q)).ToList();
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var lhp = (LopHocPhan)((Button)sender).Tag;
            if (lhp.DaDangKy)
            {
                _bll.Unregister(SessionManager.CurrentAccount.IdAcc, lhp.IdLopHp);
                lhp.DaDangKy = false;
            }
            else
            {
                var (ok, msg) = _bll.Register(SessionManager.CurrentAccount.IdAcc, lhp.IdLopHp);
                if (!ok)
                {
                    MessageBox.Show(msg, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                lhp.DaDangKy = true;
            }
            DgCourses.Items.Refresh();
        }
    }
}
