using System;
using System.Data.SqlClient;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public class UserDAL : BaseDAL
    {
        public User GetById(long idAcc)
        {
            const string sql =
                "SELECT id_acc,ho_ten,email,sdt,ngay_sinh FROM [USER] WHERE id_acc=@id";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idAcc);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new User
            {
                IdAcc    = (long)r["id_acc"],
                HoTen    = r["ho_ten"].ToString(),
                Email    = r["email"].ToString(),
                Sdt      = r["sdt"].ToString(),
                NgaySinh = r["ngay_sinh"] == DBNull.Value ? null : (DateTime?)r["ngay_sinh"]
            };
        }

        public void Update(User u)
        {
            const string sql = @"
                UPDATE [USER]
                SET ho_ten=@ht, email=@em, sdt=@sd, ngay_sinh=@ns
                WHERE id_acc=@id";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ht", u.HoTen);
            cmd.Parameters.AddWithValue("@em", u.Email ?? "");
            cmd.Parameters.AddWithValue("@sd", u.Sdt   ?? "");
            cmd.Parameters.AddWithValue("@ns", (object)u.NgaySinh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", u.IdAcc);
            cmd.ExecuteNonQuery();
        }
    }
}
