using System.Windows.Controls;
using StudentReminderApp.ViewModels;

namespace StudentReminderApp.Views.Pages
{
    public partial class ForumLogPage : Page
    {
        public ForumLogPage()
        {
            InitializeComponent();
            this.DataContext = new ForumLogViewModel();
        }
    }
}
