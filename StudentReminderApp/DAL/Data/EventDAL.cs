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
                       start_time,end_time,event_type,recurrence_rule,
                       color_category, is_completed, is_all_day, GroupId
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
                       start_time,end_time,event_type,recurrence_rule,
                       color_category, is_completed, is_all_day, GroupId
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
                    (id_acc,title,description,location,start_time,end_time,event_type,recurrence_rule,color_category,is_completed,is_all_day,GroupId)
                OUTPUT INSERTED.id_event
                VALUES(@acc,@ti,@de,@lo,@st,@en,@et,@rr,@col,@comp,@all,@gid)";
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
            cmd.Parameters.AddWithValue("@col", (object)e.ColorCategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@comp", e.IsCompleted);
            cmd.Parameters.AddWithValue("@all", e.IsAllDay);
            cmd.Parameters.AddWithValue("@gid", (object)e.GroupId ?? DBNull.Value);
            return (long)cmd.ExecuteScalar();
        }

        public void Update(PersonalEvent e)
        {
            const string sql = @"
                UPDATE PERSONAL_EVENT
                SET title=@ti,description=@de,location=@lo,
                    start_time=@st,end_time=@en,event_type=@et,recurrence_rule=@rr,
                    color_category=@col,is_completed=@comp,is_all_day=@all,GroupId=@gid
                WHERE id_event=@id";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ti", e.Title);
            cmd.Parameters.AddWithValue("@de", (object)e.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lo", (object)e.Location    ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@st", e.StartTime);
            cmd.Parameters.AddWithValue("@en", e.EndTime);
            cmd.Parameters.AddWithValue("@et", e.EventType);
            cmd.Parameters.AddWithValue("@rr", (object)e.RecurrenceRule ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@col", (object)e.ColorCategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@comp", e.IsCompleted);
            cmd.Parameters.AddWithValue("@all", e.IsAllDay);
            cmd.Parameters.AddWithValue("@gid", (object)e.GroupId ?? DBNull.Value);
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

        public void DeleteRelatedEvents(long idAcc, string title, string eventType)
        {
            const string sql = "DELETE FROM PERSONAL_EVENT WHERE id_acc=@acc AND title=@ti AND event_type=@et";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@acc", idAcc);
            cmd.Parameters.AddWithValue("@ti", title);
            cmd.Parameters.AddWithValue("@et", eventType);
            cmd.ExecuteNonQuery();
        }

        public void DeleteEventGroup(long idAcc, string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return;
            const string sql = "DELETE FROM PERSONAL_EVENT WHERE id_acc=@acc AND GroupId=@gid";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@acc", idAcc);
            cmd.Parameters.AddWithValue("@gid", groupId);
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
            RecurrenceRule = r["recurrence_rule"]?.ToString(),
            ColorCategory  = r["color_category"]?.ToString() ?? "",
            IsCompleted    = r["is_completed"] != DBNull.Value && Convert.ToBoolean(r["is_completed"]),
            IsAllDay       = r["is_all_day"] != DBNull.Value && Convert.ToBoolean(r["is_all_day"]),
            GroupId        = r["GroupId"]?.ToString()
        };

        public List<EventTag> GetTags(long idAcc)
        {
            const string sql = "SELECT id_tag, id_acc, tag_type, tag_name FROM EVENT_TAG WHERE id_acc=@id";
            var list = new List<EventTag>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idAcc);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new EventTag
                {
                    IdTag = (long)r["id_tag"],
                    IdAcc = (long)r["id_acc"],
                    TagType = r["tag_type"].ToString(),
                    TagName = r["tag_name"].ToString()
                });
            }
            return list;
        }

        public long InsertTag(EventTag t)
        {
            const string sql = "INSERT INTO EVENT_TAG (id_acc, tag_type, tag_name) OUTPUT INSERTED.id_tag VALUES (@acc, @type, @name)";
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@acc", t.IdAcc);
            cmd.Parameters.AddWithValue("@type", t.TagType);
            cmd.Parameters.AddWithValue("@name", t.TagName);
            return (long)cmd.ExecuteScalar();
        }

        public void UpdateTag(long idTag, string newName)
        {
            const string sql = "UPDATE EVENT_TAG SET tag_name=@name WHERE id_tag=@id";
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", newName);
            cmd.Parameters.AddWithValue("@id", idTag);
            cmd.ExecuteNonQuery();
        }

        public void DeleteTag(long idTag)
        {
            const string sql = "DELETE FROM EVENT_TAG WHERE id_tag=@id";
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idTag);
            cmd.ExecuteNonQuery();
        }

        public List<long> GetTagIdsForEvent(long idEvent)
        {
            var list = new List<long>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand("SELECT id_tag FROM EVENT_TAG_MAPPING WHERE id_event=@id", conn);
            cmd.Parameters.AddWithValue("@id", idEvent);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add((long)r["id_tag"]);
            return list;
        }

        public void SaveTagIdsForEvent(long idEvent, List<long> tagIds)
        {
            using var conn = GetConnection();
            using var cmdDel = new SqlCommand("DELETE FROM EVENT_TAG_MAPPING WHERE id_event=@id", conn);
            cmdDel.Parameters.AddWithValue("@id", idEvent);
            cmdDel.ExecuteNonQuery();

            if (tagIds == null || tagIds.Count == 0) return;
            foreach (var tid in tagIds)
            {
                using var cmdIns = new SqlCommand("INSERT INTO EVENT_TAG_MAPPING (id_event, id_tag) VALUES (@eid, @tid)", conn);
                cmdIns.Parameters.AddWithValue("@eid", idEvent);
                cmdIns.Parameters.AddWithValue("@tid", tid);
                cmdIns.ExecuteNonQuery();
            }
        }
    }
}
