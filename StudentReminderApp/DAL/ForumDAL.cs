using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;
using System.IO;

namespace StudentReminderApp.DAL
{
    public class ForumDAL : BaseDAL
    {
        // -------------------------------------------------------
        // HELPER: Đọc một hàng SqlDataReader → Post object
        // Tách ra để tái dùng giữa GetPosts / GetPendingPosts
        // -------------------------------------------------------
        private Post MapReaderToPost(SqlDataReader r)
        {
            return new Post
            {
                IdPost          = Convert.ToInt64(r["id_bai_viet"]),
                IdAcc           = Convert.ToInt64(r["id_acc"]),
                IdOriginalPost  = r["IdPostGoc"] != DBNull.Value ? Convert.ToInt64(r["IdPostGoc"]) : (long?)null,
                Title           = r["tieu_de"]?.ToString() ?? "",
                Content         = r["noi_dung"]?.ToString() ?? "",
                BackgroundColor = r["background_color"] != DBNull.Value ? r["background_color"].ToString() : "Transparent",
                CreatedAt       = Convert.ToDateTime(r["ngay_dang"]),
                Likes           = r["so_luot_thich"]    != DBNull.Value ? Convert.ToInt32(r["so_luot_thich"])    : 0,
                CommentCount    = r["so_luot_binh_luan"] != DBNull.Value ? Convert.ToInt32(r["so_luot_binh_luan"]) : 0,
                ShareCount      = r["so_luot_chia_se"]  != DBNull.Value ? Convert.ToInt32(r["so_luot_chia_se"])  : 0,
                IsLiked         = r["IsLikedByMe"]      != DBNull.Value && Convert.ToBoolean(r["IsLikedByMe"]),
                IsAnonymous     = r["is_anonymous"]      != DBNull.Value && Convert.ToBoolean(r["is_anonymous"]),
                AuthorName      = r["ho_ten"]?.ToString() ?? "",
                FilePaths       = new List<string>(),
                // approval_status: mặc định 1 nếu cột chưa tồn tại (tương thích ngược)
                ApprovalStatus  = r.GetOrdinal("approval_status") >= 0 && r["approval_status"] != DBNull.Value
                                    ? Convert.ToInt32(r["approval_status"])
                                    : PostStatus.Approved,
                RejectedReason  = r.GetOrdinal("rejected_reason") >= 0 && r["rejected_reason"] != DBNull.Value
                                    ? r["rejected_reason"].ToString()
                                    : null,
            };
        }

        // -------------------------------------------------------
        // Helper: Load ảnh đính kèm cho danh sách bài (dùng chung)
        // -------------------------------------------------------
        private void LoadFilePaths(List<Post> list, SqlConnection conn)
        {
            const string sqlImages = "SELECT id_bai_viet, duong_dan FROM DOCUMENTS WHERE id_bai_viet = @idP";
            foreach (var post in list)
            {
                using (var cmdImg = new SqlCommand(sqlImages, conn))
                {
                    cmdImg.Parameters.AddWithValue("@idP", post.IdPost);
                    using (var rImg = cmdImg.ExecuteReader())
                    {
                        while (rImg.Read())
                        {
                            string path = rImg["duong_dan"].ToString();
                            if (!string.IsNullOrEmpty(path))
                                post.FilePaths.Add(path);
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------
        // GetPosts: Chỉ lấy bài đã duyệt (approval_status = 1)
        // -------------------------------------------------------
        public List<Post> GetPosts()
        {
            long currentUserId = SessionManager.CurrentUser?.IdAcc ?? 0;
            var list = new List<Post>();

            const string sql = @"
                SELECT bv.*, u.ho_ten,
                    (SELECT COUNT(*) FROM YEU_THICH WHERE id_bai_viet = bv.id_bai_viet) AS TotalLikes,
                    (SELECT COUNT(*) FROM BINH_LUAN  WHERE id_bai_viet = bv.id_bai_viet) AS TotalComments,
                    (SELECT COUNT(*) FROM BAI_VIET   WHERE IdPostGoc   = bv.id_bai_viet) AS TotalShares,
                    CASE WHEN EXISTS (
                        SELECT 1 FROM YEU_THICH
                        WHERE id_bai_viet = bv.id_bai_viet AND id_acc = @currentUserId
                    ) THEN 1 ELSE 0 END AS IsLikedByMe
                FROM BAI_VIET bv
                LEFT JOIN [USER] u ON bv.id_acc = u.id_acc
                WHERE bv.approval_status = 1          -- CHỈ BÀI ĐÃ DUYỆT
                ORDER BY bv.ngay_dang DESC";

            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@currentUserId", currentUserId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read()) list.Add(MapReaderToPost(r));
                        }
                    }
                    LoadFilePaths(list, conn);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi GetPosts: " + ex.Message);
            }
            return list;
        }

        // -------------------------------------------------------
        // GetPendingPosts: Lấy bài chờ duyệt — CHỈ DÀNH CHO ADMIN
        // -------------------------------------------------------
        public List<Post> GetPendingPosts()
        {
            var list = new List<Post>();

            const string sql = @"
                SELECT bv.*, u.ho_ten,
                    (SELECT COUNT(*) FROM YEU_THICH WHERE id_bai_viet = bv.id_bai_viet) AS TotalLikes,
                    (SELECT COUNT(*) FROM BINH_LUAN  WHERE id_bai_viet = bv.id_bai_viet) AS TotalComments,
                    (SELECT COUNT(*) FROM BAI_VIET   WHERE IdPostGoc   = bv.id_bai_viet) AS TotalShares,
                    0 AS IsLikedByMe
                FROM BAI_VIET bv
                LEFT JOIN [USER] u ON bv.id_acc = u.id_acc
                WHERE bv.approval_status = 0          -- CHỜ DUYỆT
                ORDER BY bv.ngay_dang ASC";            //Bài cũ nhất ưu tiên trước

            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var r   = cmd.ExecuteReader())
                    {
                        while (r.Read()) list.Add(MapReaderToPost(r));
                    }
                    LoadFilePaths(list, conn);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi GetPendingPosts: " + ex.Message);
            }
            return list;
        }

        // -------------------------------------------------------
        // UpdatePostStatus: Duyệt / Từ chối bài — Admin only
        // -------------------------------------------------------
        public bool UpdatePostStatus(long idPost, int newStatus, long adminIdAcc, string? reason = null)
        {
            // Bảo vệ tầng DAL: kiểm tra quyền trước khi gọi DB
            const string checkAdminSql = @"
                SELECT COUNT(1) FROM ACCOUNT a
                JOIN ROLES r ON a.id_role = r.id_role
                WHERE a.id_acc = @adminId AND r.role_name = N'Admin'";

            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();

                    // --- Kiểm tra quyền Admin ---
                    using (var cmdCheck = new SqlCommand(checkAdminSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@adminId", adminIdAcc);
                        int isAdmin = (int)cmdCheck.ExecuteScalar();
                        if (isAdmin == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("DAL UpdatePostStatus: Tài khoản không phải Admin!");
                            return false;
                        }
                    }

                    // --- Cập nhật trạng thái ---
                    const string updateSql = @"
                        UPDATE BAI_VIET
                        SET 
                            approval_status = @status,
                            rejected_reason = CASE WHEN @status = 2 THEN @reason ELSE NULL END
                        WHERE id_bai_viet = @idPost";

                    using (var cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idPost",  idPost);
                        cmd.Parameters.AddWithValue("@status",  newStatus);
                        cmd.Parameters.AddWithValue("@reason",  (object?)reason ?? DBNull.Value);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi UpdatePostStatus DAL: " + ex.Message);
                return false;
            }
        }

        // -------------------------------------------------------
        // AdminDeletePost: Xóa bài bất kỳ — Admin only
        // Dùng transaction để đảm bảo toàn vẹn dữ liệu
        // Không làm xung đột Trigger TRG_UpdateShareCount vì
        // chúng ta NULL hóa IdPostGoc trước khi xóa bài con
        // -------------------------------------------------------
        public bool AdminDeletePost(long idPost, long adminIdAcc)
        {
            const string checkAdminSql = @"
                SELECT COUNT(1) FROM ACCOUNT a
                JOIN ROLES r ON a.id_role = r.id_role
                WHERE a.id_acc = @adminId AND r.role_name = N'Admin'";

            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();

                    // Kiểm tra quyền
                    using (var cmdCheck = new SqlCommand(checkAdminSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@adminId", adminIdAcc);
                        if ((int)cmdCheck.ExecuteScalar() == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("AdminDeletePost: Không có quyền Admin!");
                            return false;
                        }
                    }

                    // Xóa theo đúng thứ tự để tránh lỗi FK + tránh xung đột Trigger
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            void Exec(string sql, long id)
                            {
                                using var cmd = new SqlCommand(sql, conn, tx);
                                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
                                cmd.ExecuteNonQuery();
                            }

                            // 1. Xóa tài liệu đính kèm
                            Exec("DELETE FROM DOCUMENTS WHERE id_bai_viet = @id", idPost);

                            // 2. Xóa lượt thích → TRG_UpdateLikeCount tự cập nhật so_luot_thich
                            Exec("DELETE FROM YEU_THICH WHERE id_bai_viet = @id", idPost);

                            // 3. Xóa bình luận → TRG_UpdateCommentCount tự cập nhật so_luot_binh_luan
                            Exec("DELETE FROM BINH_LUAN WHERE id_bai_viet = @id", idPost);

                            // 4. NULL hóa IdPostGoc của bài share con
                            //    → KHÔNG xóa bài share, chỉ cắt liên kết
                            //    → TRG_UpdateShareCount sẽ KHÔNG kích hoạt vì IdPostGoc thay đổi
                            //      nhưng bản ghi BAI_VIET vẫn tồn tại (không DELETE)
                            Exec("UPDATE BAI_VIET SET IdPostGoc = NULL WHERE IdPostGoc = @id", idPost);

                            // 5. Xóa bài chính → TRG_UpdateShareCount của bài CHA cập nhật đúng
                            Exec("DELETE FROM BAI_VIET WHERE id_bai_viet = @id", idPost);

                            // 6. Ghi log Admin
                            using var logCmd = new SqlCommand(
                                "INSERT INTO USER_LOG (hanh_dong, id_acc) VALUES (@action, @aid)", conn, tx);
                            logCmd.Parameters.AddWithValue("@action", $"Admin xóa bài viết ID: {idPost}");
                            logCmd.Parameters.AddWithValue("@aid",    adminIdAcc);
                            logCmd.ExecuteNonQuery();

                            tx.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            System.Diagnostics.Debug.WriteLine("AdminDeletePost transaction lỗi: " + ex.Message);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AdminDeletePost DAL lỗi: " + ex.Message);
                return false;
            }
        }

        // -------------------------------------------------------
        // =========== CÁC HÀM CŨ GIỮ NGUYÊN ====================
        // -------------------------------------------------------

        public long InsertPost(long idAcc, string title, string content, bool isAnonymous,
                               long? idOriginalPost = null, string backgroundColor = "Transparent")
        {
            return AddPostAndGetId(idAcc, title, content, isAnonymous, idOriginalPost, backgroundColor);
        }

        public long AddPostAndGetId(long idAcc, string title, string content, bool isAnonymous,
                                    long? idOriginalPost = null, string backgroundColor = "Transparent")
        {
            if (string.IsNullOrWhiteSpace(title)) title = "Thảo luận";

            // approval_status = 0 (Chờ duyệt) cho bài MỚI tạo
            // Nếu muốn bài tự duyệt, đổi thành 1
            const string sql = @"
                INSERT INTO BAI_VIET 
                    (id_acc, tieu_de, noi_dung, ngay_dang, so_luot_thich,
                     is_anonymous, IdPostGoc, background_color, status, IsPublic, approval_status)
                VALUES
                    (@id, @t, @c, GETDATE(), 0,
                     @isAnon, @idOriginal, @bg, 'Active', 1, 0);
                SELECT SCOPE_IDENTITY();";
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id",         idAcc);
                        cmd.Parameters.AddWithValue("@t",          title);
                        cmd.Parameters.AddWithValue("@c",          (object?)content ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@isAnon",     isAnonymous);
                        cmd.Parameters.AddWithValue("@idOriginal", (object?)idOriginalPost ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@bg",         backgroundColor ?? "Transparent");
                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt64(result) : -1;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi AddPostAndGetId: " + ex.Message);
                return -1;
            }
        }

        public bool AddDocument(long idPost, string fileName, string filePath)
        {
            const string sql = "INSERT INTO DOCUMENTS (id_bai_viet, ten_file, duong_dan) VALUES (@idP, @name, @path)";
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idP",  idPost);
                        cmd.Parameters.AddWithValue("@name", fileName ?? "");
                        cmd.Parameters.AddWithValue("@path", filePath ?? "");
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi AddDocument: " + ex.Message);
                return false;
            }
        }

        public List<Comment> GetCommentsByPostId(long idPost)
        {
            var list = new List<Comment>();
            const string sql = @"
                SELECT bl.*, u.ho_ten
                FROM BINH_LUAN bl
                JOIN [USER] u ON bl.id_acc = u.id_acc
                WHERE bl.id_bai_viet = @idPost
                ORDER BY bl.ngay_binh_luan ASC";
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idPost", idPost);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                list.Add(new Comment
                                {
                                    IdComment  = Convert.ToInt64(r["id_binh_luan"]),
                                    IdAcc      = Convert.ToInt64(r["id_acc"]),
                                    IdPost     = Convert.ToInt64(r["id_bai_viet"]),
                                    Content    = r["noi_dung"]?.ToString() ?? "",
                                    CreatedAt  = r["ngay_binh_luan"] != DBNull.Value
                                                    ? Convert.ToDateTime(r["ngay_binh_luan"])
                                                    : DateTime.Now,
                                    AuthorName = r["ho_ten"]?.ToString() ?? "",
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi GetCommentsByPostId: " + ex.Message);
            }
            return list;
        }

        public bool AddComment(long idPost, long idAcc, string content)
        {
            const string sql = @"
                INSERT INTO BINH_LUAN (id_bai_viet, id_acc, noi_dung, ngay_binh_luan)
                VALUES (@idP, @idA, @c, GETDATE())";
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idP", idPost);
                        cmd.Parameters.AddWithValue("@idA", idAcc);
                        cmd.Parameters.AddWithValue("@c",   content);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi AddComment: " + ex.Message);
                return false;
            }
        }

        public bool ToggleLike(long idAcc, long idPost)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    string checkSql = "SELECT COUNT(1) FROM YEU_THICH WHERE id_acc = @idAcc AND id_bai_viet = @idPost";
                    int exists;
                    using (var cmdCheck = new SqlCommand(checkSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@idAcc",  idAcc);
                        cmdCheck.Parameters.AddWithValue("@idPost", idPost);
                        exists = (int)cmdCheck.ExecuteScalar();
                    }
                    string actionSql = exists > 0
                        ? "DELETE FROM YEU_THICH WHERE id_acc = @idAcc AND id_bai_viet = @idPost"
                        : "INSERT INTO YEU_THICH (id_acc, id_bai_viet) VALUES (@idAcc, @idPost)";
                    using (var cmdAction = new SqlCommand(actionSql, conn))
                    {
                        cmdAction.Parameters.AddWithValue("@idAcc",  idAcc);
                        cmdAction.Parameters.AddWithValue("@idPost", idPost);
                        return cmdAction.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Lỗi ToggleLike: " + ex.Message); return false; }
        }

        public bool DeletePost(long idPost)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            void Exec(string sql, long id)
                            {
                                using var cmd = new SqlCommand(sql, conn, tx);
                                cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
                                cmd.ExecuteNonQuery();
                            }
                            Exec("DELETE FROM DOCUMENTS WHERE id_bai_viet = @id",             idPost);
                            Exec("DELETE FROM YEU_THICH WHERE id_bai_viet = @id",             idPost);
                            Exec("DELETE FROM BINH_LUAN WHERE id_bai_viet = @id",             idPost);
                            Exec("UPDATE BAI_VIET SET IdPostGoc = NULL WHERE IdPostGoc = @id", idPost);
                            Exec("DELETE FROM BAI_VIET WHERE id_bai_viet = @id",              idPost);
                            tx.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi xóa bài viết DAL: " + ex.Message);
                return false;
            }
        }

        public bool DeleteComment(long idComment)
        {
            const string sql = "DELETE FROM BINH_LUAN WHERE id_binh_luan = @id";
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = idComment;
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi khi xóa bình luận DAL: " + ex.Message);
                return false;
            }
        }

        public bool AddSharedPost(long idPostGoc, long idAccNguoiChiaSe, string noiDungThem, bool laCongKhai)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand("sp_SharePost", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IdPostGoc",      idPostGoc);
                        cmd.Parameters.AddWithValue("@IdAcc",          idAccNguoiChiaSe);
                        cmd.Parameters.AddWithValue("@NoiDungShare",   noiDungThem ?? "");
                        cmd.Parameters.AddWithValue("@IsPublic",       laCongKhai);
                        object result = cmd.ExecuteScalar();
                        return result != null && Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi DAL AddSharedPost: " + ex.Message);
                return false;
            }
        }
    }
}
