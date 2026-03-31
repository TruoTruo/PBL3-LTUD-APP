using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudentReminderApp.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Hàm này giúp thông báo cho Giao diện cập nhật khi dữ liệu thay đổi
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}