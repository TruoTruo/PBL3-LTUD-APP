using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.DAL
{
    public class ForumDAL : BaseDAL
    {
        /// Lấy danh sách bài viết kèm số lượng Like và trạng thái Like của User hiện tại
        public List<Post> GetPosts()
        {
            long currentUserId = SessionManager.CurrentUser?.IdAcc ?? 0;

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

            var list = new List<Post>();

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
                                    // Sửa tên cột nhận diện bài gốc thành IdPostGoc
                                    IdOriginalPost = r["IdPostGoc"] != DBNull.Value ? Convert.ToInt64(r["IdPostGoc"]) : (long?)null,
                                    Title = r["tieu_de"]?.ToString() ?? "",
                                    Content = r["noi_dung"]?.ToString() ?? "",
                                    CreatedAt = Convert.ToDateTime(r["ngay_dang"]),
                                    Likes = Convert.ToInt32(r["TotalLikes"]),
                                    IsLiked = Convert.ToBoolean(r["IsLikedByMe"]),
                                    IsAnonymous = r["is_anonymous"] != DBNull.Value && Convert.ToBoolean(r["is_anonymous"]),
                                    AuthorName = r["ho_ten"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi GetPosts: " + ex.Message);
            }
            return list;
        }

        public List<Comment> GetCommentsByPostId(long idPost)
        {
            var list = new List<Comment>();
            string sql = @"SELECT bl.*, u.ho_ten 
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
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi ToggleLike: " + ex.Message);
                return false;
            }
        }

        public bool AddPost(long idAcc, string title, string content, bool isAnonymous, long? idOriginalPost = null)
        {
            const string sql = @"
                INSERT INTO BAI_VIET (id_acc, tieu_de, noi_dung, ngay_dang, so_luot_thich, is_anonymous, IdPostGoc) 
                VALUES (@id, @t, @c, GETDATE(), 0, @isAnon, @idOriginal)";

            try
            {
                using (var conn = GetConnection())
                {
                    if (conn.State == ConnectionState.Closed) conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idAcc);
                        cmd.Parameters.AddWithValue("@t", title ?? "");
                        cmd.Parameters.AddWithValue("@c", content ?? "");
                        cmd.Parameters.AddWithValue("@isAnon", isAnonymous);
                        cmd.Parameters.AddWithValue("@idOriginal", (object)idOriginalPost ?? DBNull.Value);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lưu bài viết: " + ex.Message);
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

                            const string sqlMain = "DELETE FROM BAI_VIET WHERE id_bai_viet = @id";
                            using (var cmd3 = new SqlCommand(sqlMain, conn, transaction))
                            {
                                cmd3.Parameters.AddWithValue("@id", idPost);
                                int result = cmd3.ExecuteNonQuery();
                                transaction.Commit();
                                return result > 0;
                            }
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi xóa bài viết: " + ex.Message);
                return false;
            }
        }
    }
}