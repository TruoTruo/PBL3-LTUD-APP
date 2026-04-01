using System;
using System.Windows;
using StudentReminderApp.ViewModels;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.Views.Dialogs
{
    public partial class CreatePostDialog : Window
    {
        public CreatePostDialog()
        {
            InitializeComponent();
            
            this.Loaded += (s, e) => 
            {
                if (this.DataContext is ForumViewModel vm)
                {
                    vm.CloseAction = new Action(this.Close);
                    vm.NewContent = string.Empty;
                }
                LoadUserData();
            };
        }

        private void LoadUserData()
        {
            if (SessionManager.CurrentUser != null && TxtUserName != null)
            {
                TxtUserName.Text = SessionManager.CurrentUser.HoTen;
                TxtPlaceholder.Text = $"{SessionManager.CurrentUser.HoTen} ơi, bạn đang nghĩ gì thế?";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnAnonymous_Toggle(object sender, RoutedEventArgs e)
        {
            if (TxtUserName == null || BtnAnonymous == null) return;
            
            TxtUserName.Text = (BtnAnonymous.IsChecked == true) 
                ? "Người dùng ẩn danh" 
                : SessionManager.CurrentUser?.HoTen;
        }
    }
}