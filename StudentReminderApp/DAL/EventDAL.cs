using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public class EventDAL : BaseDAL
    {
        public List<PersonalEvent> GetByAccount(long idAcc)
        {
            const string sql = @"
                SELECT id_event,id_acc,title,description,location,
                       start_time,end_time,event_type,recurrence_rule
                FROM   PERSONAL_EVENT WHERE id_acc=@id ORDER BY start_time";
            var list = new List<PersonalEvent>();
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idAcc);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Map(r));
            return list;
        }

        public List<PersonalEvent> GetByMonth(long idAcc, int year, int month)
        {
            const string sql = @"
                SELECT id_event,id_acc,title,description,location,
                       start_time,end_time,event_type,recurrence_rule
                FROM   PERSONAL_EVENT
                WHERE  id_acc=@id AND YEAR(start_time)=@y AND MONTH(start_time)=@m
                ORDER BY start_time";
            var list = new List<PersonalEvent>();
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idAcc);
            cmd.Parameters.AddWithValue("@y",  year);
            cmd.Parameters.AddWithValue("@m",  month);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Map(r));
            return list;
        }

        public long Insert(PersonalEvent e)
        {
            const string sql = @"
                INSERT INTO PERSONAL_EVENT
                    (id_acc,title,description,location,start_time,end_time,event_type,recurrence_rule)
                OUTPUT INSERTED.id_event
                VALUES(@acc,@ti,@de,@lo,@st,@en,@et,@rr)";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@acc", e.IdAcc);
            cmd.Parameters.AddWithValue("@ti",  e.Title);
            cmd.Parameters.AddWithValue("@de",  (object)e.Description    ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lo",  (object)e.Location       ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@st",  e.StartTime);
            cmd.Parameters.AddWithValue("@en",  e.EndTime);
            cmd.Parameters.AddWithValue("@et",  e.EventType);
            cmd.Parameters.AddWithValue("@rr",  (object)e.RecurrenceRule ?? DBNull.Value);
            return (long)cmd.ExecuteScalar();
        }

        public void Update(PersonalEvent e)
        {
            const string sql = @"
                UPDATE PERSONAL_EVENT
                SET title=@ti,description=@de,location=@lo,
                    start_time=@st,end_time=@en,event_type=@et
                WHERE id_event=@id";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ti", e.Title);
            cmd.Parameters.AddWithValue("@de", (object)e.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lo", (object)e.Location    ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@st", e.StartTime);
            cmd.Parameters.AddWithValue("@en", e.EndTime);
            cmd.Parameters.AddWithValue("@et", e.EventType);
            cmd.Parameters.AddWithValue("@id", e.IdEvent);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long idEvent)
        {
            const string sql = "DELETE FROM PERSONAL_EVENT WHERE id_event=@id";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idEvent);
            cmd.ExecuteNonQuery();
        }

        private static PersonalEvent Map(SqlDataReader r) => new PersonalEvent
        {
            IdEvent        = (long)r["id_event"],
            IdAcc          = (long)r["id_acc"],
            Title          = r["title"].ToString(),
            Description    = r["description"]?.ToString(),
            Location       = r["location"]?.ToString(),
            StartTime      = (DateTime)r["start_time"],
            EndTime        = (DateTime)r["end_time"],
            EventType      = r["event_type"].ToString(),
            RecurrenceRule = r["recurrence_rule"]?.ToString()
        };
    }
}
