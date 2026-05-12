using System.Data.SqlClient;

namespace StudentReminderApp.DAL
{
    public abstract class BaseDAL
    {
        protected SqlConnection GetConnection()
        {
            var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
