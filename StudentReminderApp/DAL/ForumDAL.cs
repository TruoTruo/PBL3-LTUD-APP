using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public class ForumDAL : BaseDAL
    {
        public List<Post> GetPosts()
        {
            // Dùng LEFT JOIN để không bị mất bài viết khi id_acc = 0 (ẩn danh)
            const string sql = @"
                SELECT bv.*, u.ho_ten 
                FROM BAI_VIET bv 
                LEFT JOIN [USER] u ON bv.id_acc = u.id_acc 
                ORDER BY bv.ngay_dang DESC";

            var list = new List<Post>();

            try
            {
                using (var conn = GetConnection())
                {
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                list.Add(new Post
                                {
                                    IdPost = Convert.ToInt64(r["id_bai_viet"]),
                                    IdAcc = Convert.ToInt64(r["id_acc"]),
                                    Title = r["tieu_de"]?.ToString() ?? "",
                                    Content = r["noi_dung"]?.ToString() ?? "",
                                    CreatedAt = Convert.ToDateTime(r["ngay_dang"]),
                                    Likes = r["so_luot_thich"] != DBNull.Value ? Convert.ToInt32(r["so_luot_thich"]) : 0,
                                    IsAnonymous = r["is_anonymous"] != DBNull.Value && Convert.ToBoolean(r["is_anonymous"]),
                                    AuthorName = r["ho_ten"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Database Error: " + ex.Message); }
            return list;
        }

        public bool AddPost(long idAcc, string title, string content, bool isAnonymous)
        {
            // Thêm trường is_anonymous vào câu lệnh INSERT
            const string sql = @"
        INSERT INTO BAI_VIET (id_acc, tieu_de, noi_dung, ngay_dang, so_luot_thich, is_anonymous) 
        VALUES (@id, @t, @c, GETDATE(), 0, @isAnon)";

            try
            {
                using (var conn = GetConnection())
                {
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idAcc);
                        cmd.Parameters.AddWithValue("@t", title ?? "");
                        cmd.Parameters.AddWithValue("@c", content ?? "");
                        cmd.Parameters.AddWithValue("@isAnon", isAnonymous); // Lưu giá trị từ Toggle/Checkbox

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

        public bool DeletePost(long idPost)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    // Bước A: Xóa các bình luận liên quan trước (nếu có bảng BINH_LUAN)
                    string sqlDelComments = "DELETE FROM BINH_LUAN WHERE id_bai_viet = @id";
                    using (var cmd1 = new SqlCommand(sqlDelComments, conn))
                    {
                        cmd1.Parameters.AddWithValue("@id", idPost);
                        cmd1.ExecuteNonQuery();
                    }

                    // Bước B: Xóa bài viết chính
                    const string sql = "DELETE FROM BAI_VIET WHERE id_bai_viet = @id";
                    using (var cmd2 = new SqlCommand(sql, conn))
                    {
                        cmd2.Parameters.AddWithValue("@id", idPost);
                        return cmd2.ExecuteNonQuery() > 0;
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