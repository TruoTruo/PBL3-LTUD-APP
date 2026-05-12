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
            try
            {
                if (!SessionManager.IsLoggedIn) return;
                
                var list = _dal.GetPending(SessionManager.CurrentAccount.IdAcc);
                
                if (list == null || list.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No pending notifications");
                    return;
                }

                foreach (var n in list)
                {
                    try
                    {
                        if (n != null && !string.IsNullOrWhiteSpace(n.Title))
                        {
                            NotificationReady?.Invoke(n.Title, n.Content ?? "");
                            _dal.MarkSent(n.IdQueue);
                        }
                    }
                    catch (Exception notificationEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing notification {n?.IdQueue}: {notificationEx.Message}");
                    }
                }
            }
            catch (InvalidOperationException iex)
            {
                System.Diagnostics.Debug.WriteLine($"Database error in Check(): {iex.Message}");
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {iex.InnerException?.Message}");
            }
            catch (NullReferenceException nex)
            {
                System.Diagnostics.Debug.WriteLine($"Null reference error in Check(): {nex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {nex.StackTrace}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error in Check(): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}