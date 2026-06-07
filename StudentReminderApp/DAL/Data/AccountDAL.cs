using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public partial class AccountDAL : BaseDAL
    {
        // ════════════════════════════════════════════════════════════
        // LẤY TẤT CẢ TÀI KHOẢN (MANAGEMENT)
        // ════════════════════════════════════════════════════════════
        public List<UserManagementDto> GetAllUsersManagement()
        {
            var list = new List<UserManagementDto>();
            const string sql = @"
                SELECT 
                    a.id_acc, a.username, r.role_name,
                    u.ho_ten, u.email, u.sdt, u.ngay_sinh, 
                    u.nganh_hoc, u.truong_hoc, u.khoa, u.ten_lop, 
                    u.nhom, u.que_quan, u.avatar_url
                FROM ACCOUNT a
                LEFT JOIN ROLES r ON a.id_role = r.id_role
                LEFT JOIN [USER] u ON a.id_acc = u.id_acc
                ORDER BY a.id_role ASC, u.ho_ten ASC";

            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            list.Add(new UserManagementDto
                            {
                                IdAcc = Convert.ToInt64(rdr["id_acc"]),
                                Username = rdr["username"] == DBNull.Value ? "" : rdr["username"].ToString(),
                                RoleName = rdr["role_name"] == DBNull.Value ? "" : rdr["role_name"].ToString(),
                                HoTen = rdr["ho_ten"] == DBNull.Value ? "" : rdr["ho_ten"].ToString(),
                                Email = rdr["email"] == DBNull.Value ? "" : rdr["email"].ToString(),
                                Sdt = rdr["sdt"] == DBNull.Value ? "" : rdr["sdt"].ToString(),
                                NgaySinh = rdr["ngay_sinh"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["ngay_sinh"]),
                                NganhHoc = rdr["nganh_hoc"] == DBNull.Value ? "" : rdr["nganh_hoc"].ToString(),
                                TruongHoc = rdr["truong_hoc"] == DBNull.Value ? "" : rdr["truong_hoc"].ToString(),
                                Khoa = rdr["khoa"] == DBNull.Value ? "" : rdr["khoa"].ToString(),
                                TenLop = rdr["ten_lop"] == DBNull.Value ? "" : rdr["ten_lop"].ToString(),
                                Nhom = rdr["nhom"] == DBNull.Value ? "" : rdr["nhom"].ToString(),
                                QueQuan = rdr["que_quan"] == DBNull.Value ? "" : rdr["que_quan"].ToString(),
                                AvatarUrl = rdr["avatar_url"] == DBNull.Value ? "" : rdr["avatar_url"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllUsersManagement Error: " + ex.Message);
            }
            return list;
        }

        // ════════════════════════════════════════════════════════════
        // XÓA TÀI KHOẢN
        // ════════════════════════════════════════════════════════════
        public void DeleteAccount(long idAcc)
        {
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    // Sử dụng Transaction để xóa toàn bộ dữ liệu liên quan
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            string[] deleteQueries = new string[]
                            {
                                "IF OBJECT_ID('USER_LOG', 'U') IS NOT NULL DELETE FROM USER_LOG WHERE id_acc = @id",
                                "IF OBJECT_ID('DEVICE_INFO', 'U') IS NOT NULL DELETE FROM DEVICE_INFO WHERE id_acc = @id",
                                "IF OBJECT_ID('DANG_KY_HOC_PHAN', 'U') IS NOT NULL DELETE FROM DANG_KY_HOC_PHAN WHERE id_sv = @id",
                                "IF OBJECT_ID('NOTIFICATION_QUEUE', 'U') IS NOT NULL DELETE FROM NOTIFICATION_QUEUE WHERE id_acc = @id",
                                "IF OBJECT_ID('REMINDER_CONFIG', 'U') IS NOT NULL DELETE FROM REMINDER_CONFIG WHERE id_acc = @id",
                                "IF OBJECT_ID('USER_PREFERENCE', 'U') IS NOT NULL DELETE FROM USER_PREFERENCE WHERE id_acc = @id",
                                "IF OBJECT_ID('DANH_GIA_GIANG_VIEN', 'U') IS NOT NULL DELETE FROM DANH_GIA_GIANG_VIEN WHERE id_acc = @id",
                                "IF OBJECT_ID('YEU_THICH', 'U') IS NOT NULL DELETE FROM YEU_THICH WHERE id_acc = @id",
                                "IF OBJECT_ID('YEU_THICH', 'U') IS NOT NULL DELETE FROM YEU_THICH WHERE id_bai_viet IN (SELECT id_bai_viet FROM BAI_VIET WHERE id_acc = @id)",
                                "IF OBJECT_ID('BINH_LUAN', 'U') IS NOT NULL DELETE FROM BINH_LUAN WHERE id_acc = @id",
                                "IF OBJECT_ID('BINH_LUAN', 'U') IS NOT NULL DELETE FROM BINH_LUAN WHERE id_bai_viet IN (SELECT id_bai_viet FROM BAI_VIET WHERE id_acc = @id)",
                                "IF OBJECT_ID('DOCUMENTS', 'U') IS NOT NULL DELETE FROM DOCUMENTS WHERE id_bai_viet IN (SELECT id_bai_viet FROM BAI_VIET WHERE id_acc = @id)",
                                "IF OBJECT_ID('TICH_LUY_TIN_CHI', 'U') IS NOT NULL DELETE FROM TICH_LUY_TIN_CHI WHERE id_sv = @id",
                                "IF OBJECT_ID('EVENT_ATTENDEE', 'U') IS NOT NULL DELETE FROM EVENT_ATTENDEE WHERE id_acc = @id",
                                "IF OBJECT_ID('EVENT_TAG', 'U') IS NOT NULL DELETE FROM EVENT_TAG WHERE id_acc = @id",
                                "IF OBJECT_ID('PERSONAL_EVENT', 'U') IS NOT NULL DELETE FROM PERSONAL_EVENT WHERE id_acc = @id",
                                "IF OBJECT_ID('BAI_VIET', 'U') IS NOT NULL UPDATE BAI_VIET SET IdPostGoc = NULL WHERE IdPostGoc IN (SELECT id_bai_viet FROM BAI_VIET WHERE id_acc = @id)",
                                "IF OBJECT_ID('BAI_VIET', 'U') IS NOT NULL DELETE FROM BAI_VIET WHERE id_acc = @id",
                                "IF OBJECT_ID('SYSTEM_ANNOUNCEMENT', 'U') IS NOT NULL DELETE FROM SYSTEM_ANNOUNCEMENT WHERE id_acc = @id",
                                "IF OBJECT_ID('USER', 'U') IS NOT NULL DELETE FROM [USER] WHERE id_acc = @id",
                                "IF OBJECT_ID('ACCOUNT', 'U') IS NOT NULL DELETE FROM ACCOUNT WHERE id_acc = @id"
                            };

                            foreach (var q in deleteQueries)
                            {
                                using (SqlCommand cmd = new SqlCommand(q, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@id", idAcc);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            trans.Commit();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DeleteAccount Error: " + ex.Message);
                throw;
            }
        }

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
        // LẤY ACCOUNT THEO EMAIL (DÙNG CHO ĐĂNG NHẬP)
        // ════════════════════════════════════════════════════════════
        public Account GetAccountByEmailForLogin(string email)
        {
            const string sql = @"
                SELECT a.id_acc, a.username, a.password_hash,
                       a.id_role, a.status,
                       ISNULL(a.is_verified, 0) AS is_verified,
                       ISNULL(r.role_name, '')  AS role_name
                FROM   ACCOUNT a
                LEFT JOIN ROLES r ON r.id_role = a.id_role
                INNER JOIN [USER] u ON a.id_acc = u.id_acc
                WHERE  (u.email = @email OR a.username = @email)";
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
                System.Diagnostics.Debug.WriteLine("AccountDAL.GetAccountByEmailForLogin: " + ex.Message);
            }
            return null;
        }

        // ════════════════════════════════════════════════════════════
        // LẤY USER CƠ BẢN THEO ID (không kèm TenLop)
        // ════════════════════════════════════════════════════════════
        public User GetUserByIdAcc(long idAcc)
        {
            const string sql = @"
                SELECT id_acc, ho_ten, email, sdt, ngay_sinh, ten_lop, nganh_hoc, truong_hoc, khoa, nhom, que_quan, avatar_url
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
                                u.TenLop   = r["ten_lop"]   == DBNull.Value ? string.Empty : r["ten_lop"].ToString();
                                u.NganhHoc = r["nganh_hoc"] == DBNull.Value ? string.Empty : r["nganh_hoc"].ToString();
                                u.TruongHoc = r["truong_hoc"] == DBNull.Value ? string.Empty : r["truong_hoc"].ToString();
                                u.Khoa = r["khoa"] == DBNull.Value ? string.Empty : r["khoa"].ToString();
                                u.Nhom = r["nhom"] == DBNull.Value ? string.Empty : r["nhom"].ToString();
                                u.QueQuan = r["que_quan"] == DBNull.Value ? string.Empty : r["que_quan"].ToString();
                                u.AvatarUrl = r["avatar_url"] == DBNull.Value ? string.Empty : r["avatar_url"].ToString();
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
                       ISNULL(u.ten_lop,  '') AS ten_lop,
                       ISNULL(u.nganh_hoc,'') AS nganh_hoc,
                       ISNULL(u.truong_hoc,'') AS truong_hoc,
                       ISNULL(u.khoa,'') AS khoa,
                       ISNULL(u.nhom,'') AS nhom,
                       ISNULL(u.que_quan,'') AS que_quan,
                       ISNULL(u.avatar_url,'') AS avatar_url
                FROM   [USER] u
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
                                u.TenLop   = r["ten_lop"]   == DBNull.Value ? string.Empty   : r["ten_lop"].ToString();
                                u.NganhHoc = r["nganh_hoc"] == DBNull.Value ? string.Empty   : r["nganh_hoc"].ToString();
                                u.TruongHoc = r["truong_hoc"] == DBNull.Value ? string.Empty   : r["truong_hoc"].ToString();
                                u.Khoa = r["khoa"] == DBNull.Value ? string.Empty   : r["khoa"].ToString();
                                u.Nhom = r["nhom"] == DBNull.Value ? string.Empty   : r["nhom"].ToString();
                                u.QueQuan = r["que_quan"] == DBNull.Value ? string.Empty   : r["que_quan"].ToString();
                                u.AvatarUrl = r["avatar_url"] == DBNull.Value ? string.Empty   : r["avatar_url"].ToString();
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
                INSERT INTO [USER] (id_acc, ho_ten, email, sdt, ten_lop)
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