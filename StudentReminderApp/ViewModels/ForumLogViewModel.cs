using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using StudentReminderApp.BLL;
using StudentReminderApp.Models;

namespace StudentReminderApp.ViewModels
{
    public class ForumLogViewModel : BaseViewModel
    {
        private readonly ForumBLL _bll;

        private ObservableCollection<UserLogModel> _logs;
        public ObservableCollection<UserLogModel> Logs
        {
            get => _logs;
            set
            {
                _logs = value;
                OnPropertyChanged(nameof(Logs));
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterLogs();
            }
        }

        private ObservableCollection<UserLogModel> _allLogs;

        public ICommand ReloadCommand { get; }

        public ForumLogViewModel()
        {
            _bll = new ForumBLL();
            Logs = new ObservableCollection<UserLogModel>();
            _allLogs = new ObservableCollection<UserLogModel>();
            ReloadCommand = new StudentReminderApp.Helpers.RelayCommand(_ => LoadLogs());
            LoadLogs();
        }

        public void LoadLogs()
        {
            var logsFromDb = _bll.GetForumLogs();
            _allLogs.Clear();
            foreach (var log in logsFromDb)
            {
                _allLogs.Add(log);
            }
            FilterLogs();
        }

        private void FilterLogs()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Logs = new ObservableCollection<UserLogModel>(_allLogs);
            }
            else
            {
                var lowerSearch = SearchText.ToLower();
                var filtered = _allLogs.Where(l => 
                    (l.UserName != null && l.UserName.ToLower().Contains(lowerSearch)) ||
                    (l.Action != null && l.Action.ToLower().Contains(lowerSearch))
                );
                Logs = new ObservableCollection<UserLogModel>(filtered);
            }
        }
    }
}
