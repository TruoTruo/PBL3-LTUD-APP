using System.Data.SqlClient;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public class AccountDAL : BaseDAL
    {
        public Account GetByUsername(string username)
        {
            const string sql = @"
                SELECT a.id_acc, a.username, a.password_hash,
                       a.id_role, a.status, r.role_name
                FROM   ACCOUNT a
                LEFT JOIN ROLES r ON a.id_role = r.id_role
                WHERE  a.username = @u";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new Account
            {
                IdAcc        = (long)r["id_acc"],
                Username     = r["username"].ToString(),
                PasswordHash = r["password_hash"].ToString(),
                IdRole       = (long)r["id_role"],
                Status       = r["status"].ToString(),
                RoleName     = r["role_name"].ToString()
            };
        }

        public bool UsernameExists(string username)
        {
            const string sql = "SELECT COUNT(1) FROM ACCOUNT WHERE username=@u";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);
            return (int)cmd.ExecuteScalar() > 0;
        }

        public long CreateWithProfile(string username, string hash,
                                      string hoTen, string email, string sdt)
        {
            const string sqlAcc  = @"
                INSERT INTO ACCOUNT(username,password_hash,id_role,status)
                OUTPUT INSERTED.id_acc VALUES(@u,@h,2,'Active')";
            const string sqlUser = @"
                INSERT INTO [USER](id_acc,ho_ten,email,sdt)
                VALUES(@id,@ht,@em,@sd)";
            using var conn = GetConnection();
            using var tran = conn.BeginTransaction();
            try
            {
                long newId;
                using (var cmd = new SqlCommand(sqlAcc, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@h", hash);
                    newId = (long)cmd.ExecuteScalar();
                }
                using (var cmd = new SqlCommand(sqlUser, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@ht", hoTen);
                    cmd.Parameters.AddWithValue("@em", email ?? "");
                    cmd.Parameters.AddWithValue("@sd", sdt   ?? "");
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
                return newId;
            }
            catch { tran.Rollback(); throw; }
        }
    }
}
