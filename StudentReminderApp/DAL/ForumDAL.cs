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
        public List<Post> GetPosts()
        {
            long currentUserId = SessionManager.CurrentUser?.IdAcc ?? 0;
            var list = new List<Post>();

            const string sql = @"
        SELECT bv.*, u.ho_ten,
        (SELECT COUNT(*) FROM YEU_THICH WHERE id_bai_viet = bv.id_bai_viet) as TotalLikes,
        CASE 
            WHEN EXISTS (SELECT 1 FROM YEU_THICH WHERE id_bai_viet = bv.id_bai_viet AND id_acc = @currentUserId) 
            THEN 1 ELSE 0 
        END as IsLikedByMe
        FROM BAI_VIET bv 
        LEFT JOIN [USER] u ON bv.id_acc = u.id_acc 
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
                            while (r.Read())
                            {
                                list.Add(new Post
                                {
                                    IdPost = Convert.ToInt64(r["id_bai_viet"]),
                                    IdAcc = Convert.ToInt64(r["id_acc"]),
                                    IdOriginalPost = r["IdPostGoc"] != DBNull.Value ? Convert.ToInt64(r["IdPostGoc"]) : (long?)null,
                                    Title = r["tieu_de"]?.ToString() ?? "",
                                    Content = r["noi_dung"]?.ToString() ?? "",
                                    BackgroundColor = r["background_color"] != DBNull.Value ? r["background_color"].ToString() : "Transparent",
                                    CreatedAt = Convert.ToDateTime(r["ngay_dang"]),
                                    Likes = Convert.ToInt32(r["TotalLikes"]),
                                    IsLiked = Convert.ToBoolean(r["IsLikedByMe"]),
                                    IsAnonymous = r["is_anonymous"] != DBNull.Value && Convert.ToBoolean(r["is_anonymous"]),
                                    AuthorName = r["ho_ten"]?.ToString() ?? "",
                                    ImagePaths = new List<string>(),
                                    FilePaths = new List<string>()
                                });
                            }
                        }
                    }

                    foreach (var post in list)
                    {
                        const string sqlImages = "SELECT duong_dan FROM DOCUMENTS WHERE id_bai_viet = @idP";
                        using (var cmdImg = new SqlCommand(sqlImages, conn))
                        {
                            cmdImg.Parameters.AddWithValue("@idP", post.IdPost);
                            using (var rImg = cmdImg.ExecuteReader())
                            {
                                while (rImg.Read())
                                {
                                    string path = rImg["duong_dan"].ToString();
                                    if (!string.IsNullOrEmpty(path))
                                    {
                                        post.FilePaths.Add(path);
                                        string ext = Path.GetExtension(path).ToLower();
                                        if (ext == ".jpg" || ext == ".png" || ext == ".jpeg" || ext == ".bmp")
                                        {
                                            post.ImagePaths.Add(path);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi GetPosts: " + ex.Message);
            }
            return list;
        }

        public long InsertPost(long idAcc, string title, string content, bool isPublic, long? idOriginalPost = null, string backgroundColor = "Transparent")
        {
            return AddPostAndGetId(idAcc, title, content, !isPublic, idOriginalPost, backgroundColor);
        }

        public long AddPostAndGetId(long idAcc, string title, string content, bool isAnonymous, long? idOriginalPost = null, string backgroundColor = "Transparent")
        {
            if (string.IsNullOrWhiteSpace(title)) title = "Thảo luận";
            const string sql = @"
                INSERT INTO BAI_VIET (id_acc, tieu_de, noi_dung, ngay_dang, so_luot_thich, is_anonymous, IdPostGoc, background_color, status, IsPublic) 
                VALUES (@id, @t, @c, GETDATE(), 0, @isAnon, @idOriginal, @bg, 'Active', 1);
                SELECT SCOPE_IDENTITY();";
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idAcc);
                        cmd.Parameters.AddWithValue("@t", title);
                        cmd.Parameters.AddWithValue("@c", (object)content ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@isAnon", isAnonymous);
                        cmd.Parameters.AddWithValue("@idOriginal", (object)idOriginalPost ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@bg", backgroundColor ?? "Transparent");
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
                        cmd.Parameters.AddWithValue("@idP", idPost);
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
            string sql = @"SELECT bl.*, u.ho_ten FROM BINH_LUAN bl JOIN [USER] u ON bl.id_acc = u.id_acc WHERE bl.id_bai_viet = @idPost ORDER BY bl.ngay_binh_luan ASC";
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
                                    IdComment = Convert.ToInt64(r["id_binh_luan"]),
                                    IdAcc = Convert.ToInt64(r["id_acc"]),
                                    IdPost = Convert.ToInt64(r["id_bai_viet"]),
                                    Content = r["noi_dung"].ToString(),
                                    CreatedAt = Convert.ToDateTime(r["ngay_binh_luan"]),
                                    AuthorName = r["ho_ten"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Lỗi GetComments: " + ex.Message); }
            return list;
        }

        public bool AddComment(long idPost, long idAcc, string content)
        {
            string sql = "INSERT INTO BINH_LUAN (id_bai_viet, id_acc, noi_dung, ngay_binh_luan) VALUES (@idP, @idA, @c, GETDATE())";
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idP", idPost);
                        cmd.Parameters.AddWithValue("@idA", idAcc);
                        cmd.Parameters.AddWithValue("@c", content);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Lỗi AddComment: " + ex.Message); return false; }
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
                        cmdCheck.Parameters.AddWithValue("@idAcc", idAcc);
                        cmdCheck.Parameters.AddWithValue("@idPost", idPost);
                        exists = (int)cmdCheck.ExecuteScalar();
                    }
                    string actionSql = exists > 0
                        ? "DELETE FROM YEU_THICH WHERE id_acc = @idAcc AND id_bai_viet = @idPost"
                        : "INSERT INTO YEU_THICH (id_acc, id_bai_viet) VALUES (@idAcc, @idPost)";
                    using (var cmdAction = new SqlCommand(actionSql, conn))
                    {
                        cmdAction.Parameters.AddWithValue("@idAcc", idAcc);
                        cmdAction.Parameters.AddWithValue("@idPost", idPost);
                        return cmdAction.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Lỗi ToggleLike: " + ex.Message); return false; }
        }

        public bool AddPost(long idAcc, string title, string content, bool isAnonymous, long? idOriginalPost = null, string backgroundColor = "Transparent")
        {
            return AddPostAndGetId(idAcc, title, content, isAnonymous, idOriginalPost, backgroundColor) > 0;
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
                        cmd.Parameters.AddWithValue("@IdPostGoc", idPostGoc);
                        cmd.Parameters.AddWithValue("@IdAcc", idAccNguoiChiaSe);
                        cmd.Parameters.AddWithValue("@NoiDungShare", noiDungThem ?? "");
                        cmd.Parameters.AddWithValue("@IsPublic", laCongKhai);
                        object result = cmd.ExecuteScalar();
                        if (result != null) return Convert.ToInt32(result) > 0;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi DAL AddSharedPost: " + ex.Message);
                return false;
            }
        }

        public bool DeletePost(long idPost)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string sqlDelDocs = "DELETE FROM DOCUMENTS WHERE id_bai_viet = @id";
                            using (var cmd0 = new SqlCommand(sqlDelDocs, conn, transaction))
                            {
                                cmd0.Parameters.AddWithValue("@id", idPost);
                                cmd0.ExecuteNonQuery();
                            }
                            string sqlDelLikes = "DELETE FROM YEU_THICH WHERE id_bai_viet = @id";
                            using (var cmd1 = new SqlCommand(sqlDelLikes, conn, transaction))
                            {
                                cmd1.Parameters.AddWithValue("@id", idPost);
                                cmd1.ExecuteNonQuery();
                            }
                            string sqlDelComments = "DELETE FROM BINH_LUAN WHERE id_bai_viet = @id";
                            using (var cmd2 = new SqlCommand(sqlDelComments, conn, transaction))
                            {
                                cmd2.Parameters.AddWithValue("@id", idPost);
                                cmd2.ExecuteNonQuery();
                            }
                            string sqlNullifyShared = "UPDATE BAI_VIET SET IdPostGoc = NULL WHERE IdPostGoc = @id";
                            using (var cmd3 = new SqlCommand(sqlNullifyShared, conn, transaction))
                            {
                                cmd3.Parameters.AddWithValue("@id", idPost);
                                cmd3.ExecuteNonQuery();
                            }
                            const string sqlMain = "DELETE FROM BAI_VIET WHERE id_bai_viet = @id";
                            using (var cmd4 = new SqlCommand(sqlMain, conn, transaction))
                            {
                                cmd4.Parameters.AddWithValue("@id", idPost);
                                int result = cmd4.ExecuteNonQuery();
                                transaction.Commit();
                                return result > 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
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

        // --- HÀM DELETE COMMENT ĐÃ ĐƯỢC TỐI ƯU ---
        public bool DeleteComment(long idComment)
        {
            const string sql = "DELETE FROM BINH_LUAN WHERE id_binh_luan = @id";

            try
            {
                using (var conn = GetConnection())
                {
                    // Luôn đảm bảo Connection được mở
                    if (conn.State == ConnectionState.Closed) conn.Open();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        // Dùng SqlDbType.BigInt để khớp chính xác với kiểu bigint trong DB của Dung
                        cmd.Parameters.Add("@id", SqlDbType.BigInt).Value = idComment;

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi khi xóa bình luận DAL: " + ex.Message);
                return false;
            }
        }
    }
}