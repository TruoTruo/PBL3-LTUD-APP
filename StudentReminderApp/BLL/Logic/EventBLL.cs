using System;
using System.Collections.Generic;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;

namespace StudentReminderApp.BLL
{
    public class EventBLL
    {
        private readonly EventDAL        _eventDal = new EventDAL();
        private readonly NotificationDAL _notifDal = new NotificationDAL();

        public List<PersonalEvent> GetByMonth(long idAcc, int year, int month)
            => _eventDal.GetByMonth(idAcc, year, month);

        public List<PersonalEvent> GetUpcoming(long idAcc, int days = 7)
        {
            var all  = _eventDal.GetByAccount(idAcc);
            var from = DateTime.Today;  // Midnight of today
            var to   = DateTime.Today.AddDays(days);  // Midnight of day+days
            
            // Filter events that start from today onwards up to the specified number of days
            // Include events that start on or after today and before the end date
            return all.FindAll(e => 
            {
                var eventDate = e.StartTime.Date;
                return eventDate >= from.Date && eventDate < to.Date;
            });
        }

        public (bool ok, string msg) Save(PersonalEvent e, int minsBeforeReminder = 15)
        {
            if (string.IsNullOrWhiteSpace(e.Title))
                return (false, "Tiêu đề không được để trống.");
            if (e.EndTime <= e.StartTime)
                return (false, "Thời gian kết thúc phải sau thời gian bắt đầu.");
            if (e.IdEvent == 0)
            {
                e.IdEvent = _eventDal.Insert(e);
            }
            else
                _eventDal.Update(e);
            return (true, "Lưu thành công!");
        }

        public void Delete(long idEvent) => _eventDal.Delete(idEvent);
        public void DeleteRelatedEvents(long idAcc, string title, string eventType) => _eventDal.DeleteRelatedEvents(idAcc, title, eventType);
        public void DeleteEventGroup(long idAcc, string groupId) => _eventDal.DeleteEventGroup(idAcc, groupId);
        public List<CalendarItem> GetCalendarItemsForMonth(long idAcc, int year, int month)
        {
            var items = new List<CalendarItem>();
            var personalEvents = GetByMonth(idAcc, year, month);
            
            foreach (var e in personalEvents)
            {
                items.Add(new CalendarItem
                {
                    Id = e.IdEvent,
                    Title = e.Title,
                    Description = e.Description,
                    Location = e.Location,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    EventType = e.EventType,
                    OriginalEvent = e
                });
            }

            return items;
        }
    }
}
