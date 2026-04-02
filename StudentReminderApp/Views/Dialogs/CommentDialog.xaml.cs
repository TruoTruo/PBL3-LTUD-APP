using System.Windows;
using StudentReminderApp.Models;
using StudentReminderApp.ViewModels;

namespace StudentReminderApp.Views.Dialogs
{
    public partial class CommentDialog : Window
    {
        public CommentDialog(Post post)
        {
            InitializeComponent();

            if (post != null)
            {
                this.DataContext = new CommentViewModel(post);
            }
            else
            {
                this.Close();
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}