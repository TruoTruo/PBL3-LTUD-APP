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
                "SELECT id_acc,ho_ten,email,sdt,ngay_sinh,nganh_hoc,truong_hoc,khoa,nhom,que_quan,avatar_url FROM [USER] WHERE id_acc=@id";
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
                NgaySinh = r["ngay_sinh"] == DBNull.Value ? null : (DateTime?)r["ngay_sinh"],
                NganhHoc = r["nganh_hoc"] == DBNull.Value ? string.Empty : r["nganh_hoc"].ToString(),
                TruongHoc = r["truong_hoc"] == DBNull.Value ? string.Empty : r["truong_hoc"].ToString(),
                Khoa = r["khoa"] == DBNull.Value ? string.Empty : r["khoa"].ToString(),
                Nhom = r["nhom"] == DBNull.Value ? string.Empty : r["nhom"].ToString(),
                QueQuan = r["que_quan"] == DBNull.Value ? string.Empty : r["que_quan"].ToString(),
                AvatarUrl = r["avatar_url"] == DBNull.Value ? string.Empty : r["avatar_url"].ToString()
            };
        }

        public void Update(User u)
        {
            const string sql = @"
                UPDATE [USER]
                SET ho_ten=@ht, email=@em, sdt=@sd, ngay_sinh=@ns, nganh_hoc=@nh, truong_hoc=@tr, khoa=@kh, nhom=@nhom, que_quan=@qq, avatar_url=@ava
                WHERE id_acc=@id";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ht", u.HoTen);
            cmd.Parameters.AddWithValue("@em", u.Email ?? "");
            cmd.Parameters.AddWithValue("@sd", u.Sdt   ?? "");
            cmd.Parameters.AddWithValue("@ns", (object)u.NgaySinh ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nh", u.NganhHoc ?? "");
            cmd.Parameters.AddWithValue("@tr", u.TruongHoc ?? "");
            cmd.Parameters.AddWithValue("@kh", u.Khoa ?? "");
            cmd.Parameters.AddWithValue("@nhom", u.Nhom ?? "");
            cmd.Parameters.AddWithValue("@qq", u.QueQuan ?? "");
            cmd.Parameters.AddWithValue("@ava", u.AvatarUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", u.IdAcc);
            cmd.ExecuteNonQuery();
        }
    }
}
