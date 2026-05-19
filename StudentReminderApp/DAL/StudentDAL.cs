using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public class StudentDAL : BaseDAL
    {
        // ─────────────────────────────────────────────────────────
        // Lấy toàn bộ sinh viên
        // ─────────────────────────────────────────────────────────
        public List<StudentModel> GetAllStudents()
        {
            var list = new List<StudentModel>();
            const string sql = @"
                SELECT
                    a.id_acc,
                    a.username      AS mssv,
                    a.status,
                    a.is_verified,
                    a.created_at,
                    a.lock_until,
                    u.ho_ten,
                    ISNULL(u.email, '') AS email,
                    ISNULL(u.sdt,   '') AS sdt,
                    ISNULL(l.ten_lop,   N'Chưa phân lớp') AS ten_lop,
                    ISNULL(l.nien_khoa, '')                AS nien_khoa,
                    u.id_lop
                FROM ACCOUNT a
                JOIN [USER]  u ON a.id_acc  = u.id_acc
                LEFT JOIN LOP_SINH_VIEN l ON u.id_lop = l.id_lop
                JOIN ROLES   r ON a.id_role = r.id_role
                WHERE r.role_name = N'Student'
                ORDER BY a.created_at DESC";

            try
            {
                using var conn = GetConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                using var r   = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new StudentModel
                    {
                        IdAcc      = Convert.ToInt64(r["id_acc"]),
                        Mssv       = r["mssv"].ToString()      ?? "",
                        HoTen      = r["ho_ten"].ToString()    ?? "",
                        Email      = r["email"].ToString()     ?? "",
                        Sdt        = r["sdt"].ToString()       ?? "",
                        TenLop     = r["ten_lop"].ToString()   ?? "",
                        NienKhoa   = r["nien_khoa"].ToString() ?? "",
                        IdLop      = r["id_lop"]     != DBNull.Value ? Convert.ToInt64(r["id_lop"])        : null,
                        LockUntil  = r["lock_until"] != DBNull.Value ? Convert.ToDateTime(r["lock_until"]) : null,
                        Status     = r["status"].ToString()    ?? "Active",
                        IsVerified = r["is_verified"] != DBNull.Value && Convert.ToBoolean(r["is_verified"]),
                        CreatedAt  = r["created_at"] != DBNull.Value
                                        ? Convert.ToDateTime(r["created_at"]) : DateTime.Now,
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentDAL.GetAllStudents: " + ex.Message);
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────
        // Lấy danh sách lớp (ComboBox)
        // ─────────────────────────────────────────────────────────
        public List<(long IdLop, string TenLop)> GetAllClasses()
        {
            var list = new List<(long, string)>();
            const string sql = "SELECT id_lop, ten_lop FROM LOP_SINH_VIEN ORDER BY ten_lop";
            try
            {
                using var conn = GetConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                using var r   = cmd.ExecuteReader();
                while (r.Read())
                    list.Add((Convert.ToInt64(r["id_lop"]), r["ten_lop"].ToString() ?? ""));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentDAL.GetAllClasses: " + ex.Message);
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────
        // Cập nhật lớp của sinh viên (từ Profile hoặc Admin)
        // idLop = null → xóa lớp (Chưa phân lớp)
        // ─────────────────────────────────────────────────────────
        public bool UpdateStudentClass(long idAcc, long? idLop)
        {
            const string sql = "UPDATE [USER] SET id_lop = @idLop WHERE id_acc = @idAcc";
            try
            {
                using var conn = GetConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@idAcc", SqlDbType.BigInt).Value = idAcc;
                cmd.Parameters.Add("@idLop", SqlDbType.BigInt).Value =
                    idLop.HasValue ? (object)idLop.Value : DBNull.Value;
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentDAL.UpdateStudentClass: " + ex.Message);
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // Khóa tài khoản
        // ─────────────────────────────────────────────────────────
        public bool BanStudent(long idAcc, DateTime? lockUntil)
        {
            const string sql = @"
                UPDATE ACCOUNT
                SET status = 'Banned', lock_until = @lockUntil
                WHERE id_acc = @id";
            try
            {
                using var conn = GetConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@id",        SqlDbType.BigInt).Value   = idAcc;
                cmd.Parameters.Add("@lockUntil", SqlDbType.DateTime).Value =
                    lockUntil.HasValue ? (object)lockUntil.Value : DBNull.Value;
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentDAL.BanStudent: " + ex.Message);
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // Mở khóa tài khoản
        // ─────────────────────────────────────────────────────────
        public bool UnbanStudent(long idAcc)
        {
            const string sql = "UPDATE ACCOUNT SET status='Active', lock_until=NULL WHERE id_acc=@id";
            try
            {
                using var conn = GetConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = idAcc;
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentDAL.UnbanStudent: " + ex.Message);
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────
        // Xác thực sinh viên
        // ─────────────────────────────────────────────────────────
        public bool VerifyStudent(long idAcc)
        {
            const string sql = "UPDATE ACCOUNT SET is_verified=1 WHERE id_acc=@id";
            try
            {
                using var conn = GetConnection();
                if (conn.State == ConnectionState.Closed) conn.Open();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = idAcc;
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentDAL.VerifyStudent: " + ex.Message);
                return false;
            }
        }
    }
}
