using System;
using System.Windows;
using StudentReminderApp.Models;
using StudentReminderApp.ViewModels;

namespace StudentReminderApp.Views.Dialogs
{
    public partial class ShareDialog : Window
    {
        public ShareDialog(Post post)
        {
            InitializeComponent();

            if (post == null)
            {
                MessageBox.Show("Dữ liệu bài viết không hợp lệ!");
                this.Close();
                return;
            }

            this.DataContext = new ShareViewModel(post);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}