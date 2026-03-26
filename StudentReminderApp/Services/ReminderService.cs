using System;
using System.Windows.Threading;
using StudentReminderApp.DAL;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.Services
{
    public class ReminderService
    {
        private readonly DispatcherTimer _timer  = new DispatcherTimer();
        private readonly NotificationDAL _dal    = new NotificationDAL();

        public event Action<string, string> NotificationReady;

        public ReminderService()
        {
            _timer.Interval = TimeSpan.FromSeconds(60);
            _timer.Tick    += Check;
        }

        public void Start() => _timer.Start();
        public void Stop()  => _timer.Stop();

        private void Check(object s, EventArgs e)
        {
            if (!SessionManager.IsLoggedIn) return;
            var list = _dal.GetPending(SessionManager.CurrentAccount.IdAcc);
            foreach (var n in list)
            {
                NotificationReady?.Invoke(n.Title, n.Content);
                _dal.MarkSent(n.IdQueue);
            }
        }
    }
}
