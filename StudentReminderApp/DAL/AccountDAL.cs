using System;
using System.Data;
using System.Data.SqlClient;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public partial class AccountDAL : BaseDAL
    {
        // ════════════════════════════════════════════════════════════
        // LẤY ACCOUNT THEO USERNAME
        // ════════════════════════════════════════════════════════════
        public Account GetAccountByUsername(string username)
        {
            const string sql = @"
                SELECT a.id_acc, a.username, a.password_hash,
                       a.id_role, a.status,
                       ISNULL(a.is_verified, 0) AS is_verified,
                       ISNULL(r.role_name, '')  AS role_name
                FROM   ACCOUNT a
                LEFT JOIN ROLES r ON r.id_role = a.id_role
                WHERE  a.username = @username";
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                Account acc = new Account();
                                acc.IdAcc        = Convert.ToInt64(r["id_acc"]);
                                acc.Username     = r["username"].ToString() ?? string.Empty;
                                acc.PasswordHash = r["password_hash"].ToString() ?? string.Empty;
                                acc.IdRole       = Convert.ToInt64(r["id_role"]);
                                acc.Status       = r["status"].ToString() ?? "Active";
                                acc.IsVerified   = Convert.ToBoolean(r["is_verified"]);
                                acc.RoleName     = r["role_name"].ToString() ?? string.Empty;
                                return acc;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AccountDAL.GetAccountByUsername: " + ex.Message);
            }
            return null;
        }

        // ════════════════════════════════════════════════════════════
        // LẤY USER CƠ BẢN THEO ID (không kèm TenLop)
        // ════════════════════════════════════════════════════════════
        public User GetUserByIdAcc(long idAcc)
        {
            const string sql = @"
                SELECT id_acc, ho_ten, email, sdt, ngay_sinh, id_lop
                FROM   [USER]
                WHERE  id_acc = @id";
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idAcc);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                User u = new User();
                                u.IdAcc    = Convert.ToInt64(r["id_acc"]);
                                u.HoTen    = r["ho_ten"]    == DBNull.Value ? string.Empty : r["ho_ten"].ToString();
                                u.Email    = r["email"]     == DBNull.Value ? string.Empty : r["email"].ToString();
                                u.Sdt      = r["sdt"]       == DBNull.Value ? string.Empty : r["sdt"].ToString();
                                u.NgaySinh = r["ngay_sinh"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ngay_sinh"]);
                                u.IdLop    = r["id_lop"]    == DBNull.Value ? (long?)null    : Convert.ToInt64(r["id_lop"]);
                                u.TenLop   = string.Empty;
                                return u;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AccountDAL.GetUserByIdAcc: " + ex.Message);
            }
            return null;
        }

        // ════════════════════════════════════════════════════════════
        // LẤY USER KÈM TEN_LOP (LEFT JOIN) — fix lỗi TenLop null
        // ════════════════════════════════════════════════════════════
        public User GetUserWithClass(long idAcc)
        {
            const string sql = @"
                SELECT u.id_acc,
                       u.ho_ten,
                       ISNULL(u.email,    '') AS email,
                       ISNULL(u.sdt,      '') AS sdt,
                       u.ngay_sinh,
                       u.id_lop,
                       ISNULL(l.ten_lop,  '') AS ten_lop
                FROM   [USER] u
                LEFT JOIN LOP_SINH_VIEN l ON l.id_lop = u.id_lop
                WHERE  u.id_acc = @idAcc";
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idAcc", idAcc);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                User u = new User();
                                u.IdAcc    = Convert.ToInt64(r["id_acc"]);
                                u.HoTen    = r["ho_ten"]    == DBNull.Value ? string.Empty : r["ho_ten"].ToString();
                                u.Email    = r["email"]     == DBNull.Value ? string.Empty : r["email"].ToString();
                                u.Sdt      = r["sdt"]       == DBNull.Value ? string.Empty : r["sdt"].ToString();
                                u.NgaySinh = r["ngay_sinh"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["ngay_sinh"]);
                                u.IdLop    = r["id_lop"]    == DBNull.Value ? (long?)null    : Convert.ToInt64(r["id_lop"]);
                                u.TenLop   = r["ten_lop"]   == DBNull.Value ? string.Empty   : r["ten_lop"].ToString();
                                return u;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AccountDAL.GetUserWithClass: " + ex.Message);
            }
            return null;
        }

        // ════════════════════════════════════════════════════════════
        // TÌM ACCOUNT THEO EMAIL (dùng cho Quên mật khẩu)
        // ════════════════════════════════════════════════════════════
        // Trả về Tuple có thể null — dùng kiểu tường minh tránh CS8130
        public Tuple<long, string> GetAccountByEmail(string email)
        {
            const string sql = @"
                SELECT a.id_acc, u.email
                FROM   ACCOUNT a
                INNER JOIN [USER] u ON u.id_acc = a.id_acc
                WHERE  u.email = @email";
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email.Trim());
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                long   idAcc     = Convert.ToInt64(r["id_acc"]);
                                string emailFromDb = r["email"] == DBNull.Value
                                    ? string.Empty : r["email"].ToString();

                                if (string.IsNullOrWhiteSpace(emailFromDb)) return null;
                                return Tuple.Create(idAcc, emailFromDb);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AccountDAL.GetAccountByEmail: " + ex.Message);
            }
            return null;
        }

        // ════════════════════════════════════════════════════════════
        // ĐĂNG KÝ: Chèn đồng thời vào ACCOUNT + [USER] (Transaction)
        // ════════════════════════════════════════════════════════════
        public bool InsertNewStudentAccount(
            string mssv, string passwordHash, long idRole,
            string hoTen, string email, string sdt)
        {
            const string sqlAcc = @"
                INSERT INTO ACCOUNT (username, password_hash, id_role, status, is_verified, created_at)
                VALUES (@username, @hash, @idRole, 'Active', 0, GETDATE());
                SELECT SCOPE_IDENTITY();";

            const string sqlUser = @"
                INSERT INTO [USER] (id_acc, ho_ten, email, sdt, id_lop)
                VALUES (@idAcc, @hoTen, @email, @sdt, NULL);";

            using (SqlConnection conn = GetConnection())
            {
                if (conn.State == ConnectionState.Closed) conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    // 1. Chèn ACCOUNT
                    long newIdAcc;
                    using (SqlCommand cmdAcc = new SqlCommand(sqlAcc, conn, trans))
                    {
                        cmdAcc.Parameters.AddWithValue("@username", mssv);
                        cmdAcc.Parameters.AddWithValue("@hash",     passwordHash);
                        cmdAcc.Parameters.AddWithValue("@idRole",   idRole);
                        newIdAcc = Convert.ToInt64(cmdAcc.ExecuteScalar());
                    }

                    // 2. Chèn [USER]
                    using (SqlCommand cmdUsr = new SqlCommand(sqlUser, conn, trans))
                    {
                        cmdUsr.Parameters.AddWithValue("@idAcc", newIdAcc);
                        cmdUsr.Parameters.AddWithValue("@hoTen", hoTen  ?? string.Empty);
                        cmdUsr.Parameters.AddWithValue("@email", email  ?? string.Empty);
                        cmdUsr.Parameters.AddWithValue("@sdt",   sdt    ?? string.Empty);
                        cmdUsr.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    System.Diagnostics.Debug.WriteLine("AccountDAL.InsertNewStudentAccount: " + ex.Message);
                    return false;
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        // OTP — Lưu mã
        // ════════════════════════════════════════════════════════════
        public bool SaveOtp(long idAcc, string otpCode)
        {
            const string sql = @"
                UPDATE ACCOUNT
                SET otp_code       = @otp,
                    otp_expired_at = DATEADD(MINUTE, 5, GETDATE()),
                    updated_at     = GETDATE()
                WHERE id_acc = @id";
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@otp", otpCode);
                        cmd.Parameters.AddWithValue("@id",  idAcc);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AccountDAL.SaveOtp: " + ex.Message);
                return false;
            }
        }

        // ════════════════════════════════════════════════════════════
        // OTP — Xác nhận mã (còn hạn?)
        // ════════════════════════════════════════════════════════════
        public bool VerifyOtp(long idAcc, string otpCode)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM   ACCOUNT
                WHERE  id_acc       = @id
                  AND  otp_code     = @otp
                  AND  otp_expired_at >= GETDATE()";
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id",  idAcc);
                        cmd.Parameters.AddWithValue("@otp", otpCode);
                        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AccountDAL.VerifyOtp: " + ex.Message);
                return false;
            }
        }

        // ════════════════════════════════════════════════════════════
        // OTP — Đặt lại mật khẩu + xoá OTP
        // ════════════════════════════════════════════════════════════
        public bool ResetPassword(long idAcc, string newPasswordHash)
        {
            const string sql = @"
                UPDATE ACCOUNT
                SET password_hash  = @hash,
                    otp_code       = NULL,
                    otp_expired_at = NULL,
                    updated_at     = GETDATE()
                WHERE id_acc = @id";
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@hash", newPasswordHash);
                        cmd.Parameters.AddWithValue("@id",   idAcc);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AccountDAL.ResetPassword: " + ex.Message);
                return false;
            }
        }
    }
}