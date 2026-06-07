using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public class NotificationDAL : BaseDAL
    {
        public List<NotificationQueue> GetPending(long idAcc)
        {
            const string sql = @"
                SELECT id_queue,id_acc,title,content,scheduled_at,status,id_buoi_hoc,id_event
                FROM   NOTIFICATION_QUEUE
                WHERE  id_acc=@id AND status='PENDING' AND scheduled_at<=GETDATE()";
            var list = new List<NotificationQueue>();
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idAcc);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new NotificationQueue
                {
                    IdQueue     = (long)r["id_queue"],
                    IdAcc       = (long)r["id_acc"],
                    Title       = r["title"].ToString(),
                    Content     = r["content"].ToString(),
                    ScheduledAt = (DateTime)r["scheduled_at"],
                    Status      = r["status"].ToString(),
                    IdBuoiHoc   = r["id_buoi_hoc"] == DBNull.Value ? null : (long?)r["id_buoi_hoc"],
                    IdEvent     = r["id_event"]    == DBNull.Value ? null : (long?)r["id_event"]
                });
            return list;
        }

        public void MarkSent(long idQueue)
        {
            const string sql =
                "UPDATE NOTIFICATION_QUEUE SET status='SENT',sent_at=GETDATE() WHERE id_queue=@id";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idQueue);
            cmd.ExecuteNonQuery();
        }

        public void CreateForEvent(long idAcc, PersonalEvent ev, int minsBefore)
        {
            const string sql = @"
                INSERT INTO NOTIFICATION_QUEUE(id_acc,title,content,scheduled_at,id_event,status)
                VALUES(@acc,@ti,@co,@sc,@ev,'PENDING')";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@acc", idAcc);
            cmd.Parameters.AddWithValue("@ti",  $"Nhắc: {ev.Title}");
            cmd.Parameters.AddWithValue("@co",  $"Sự kiện bắt đầu lúc {ev.StartTime:HH:mm dd/MM}");
            cmd.Parameters.AddWithValue("@sc",  ev.StartTime.AddMinutes(-minsBefore));
            cmd.Parameters.AddWithValue("@ev",  ev.IdEvent);
            cmd.ExecuteNonQuery();
        }
    }
}
