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
            var from = DateTime.Now;
            var to   = DateTime.Now.AddDays(days);
            return all.FindAll(e => e.StartTime >= from && e.StartTime <= to);
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
                _notifDal.CreateForEvent(e.IdAcc, e, minsBeforeReminder);
            }
            else
                _eventDal.Update(e);
            return (true, "Lưu thành công!");
        }

        public void Delete(long idEvent) => _eventDal.Delete(idEvent);
    }
}
