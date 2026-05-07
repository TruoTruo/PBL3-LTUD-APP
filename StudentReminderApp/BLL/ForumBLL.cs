using System;
using System.Collections.Generic;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;
using System.IO;

namespace StudentReminderApp.BLL
{
    public class ForumBLL
    {
        private readonly ForumDAL _forumDAL = new ForumDAL();

        public List<Post> GetAllPosts()
        {
            return _forumDAL.GetPosts();
        }

        public List<Comment> GetComments(long idPost)
        {
            return _forumDAL.GetCommentsByPostId(idPost);
        }

        public bool PostComment(long idPost, long idAcc, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            return _forumDAL.AddComment(idPost, idAcc, content);
        }

        public bool ToggleLike(long idAcc, long idPost)
        {
            if (idAcc <= 0 || idPost <= 0) return false;
            try
            {
                return _forumDAL.ToggleLike(idAcc, idPost);
            }
            catch (Exception ex)
            {
                Console.WriteLine("BLL ToggleLike Error: " + ex.Message);
                return false;
            }
        }

        public bool CreatePost(long idAcc, string title, string content, bool isPublic, List<string> filePaths, long? idPostGoc = null, string theme = "Transparent")
        {
            try
            {
                long newPostId = _forumDAL.InsertPost(idAcc, title, content, isPublic, idPostGoc, theme);

                if (newPostId > 0 && filePaths != null)
                {
                    
                    string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images");

                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    foreach (string originalPath in filePaths)
                    {
                        if (File.Exists(originalPath))
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(originalPath);
                            string destPath = Path.Combine(folderPath, fileName);

                            File.Copy(originalPath, destPath, true);

                            _forumDAL.AddDocument(newPostId, fileName, fileName);
                        }
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi lưu bài viết: " + ex.Message);
                return false;
            }
        }

        public bool SharePost(long idPostGoc, long idAccNguoiChiaSe, string noiDungThem, bool laCongKhai)
        {
            if (idPostGoc <= 0 || idAccNguoiChiaSe <= 0) return false;

            try
            {
                
                return CreatePost(idAccNguoiChiaSe, "Chia sẻ bài viết", noiDungThem, false, null, idPostGoc, "Transparent");
            }
            catch (Exception ex)
            {
                Console.WriteLine("BLL SharePost Error: " + ex.Message);
                return false;
            }
        }

        /// Xóa bài viết
        public bool RemovePost(long idPost)
        {
            if (idPost <= 0) return false;
            try
            {
                return _forumDAL.DeletePost(idPost);
            }
            catch (Exception ex)
            {
                Console.WriteLine("BLL RemovePost Error: " + ex.Message);
                return false;
            }
        }

        public bool DeleteComment(long idComment)
        {
            if (idComment <= 0)
            {
                System.Diagnostics.Debug.WriteLine("BLL: ID Bình luận không hợp lệ.");
                return false;
            }

            try
            {
                bool isDeleted = _forumDAL.DeleteComment(idComment);

                if (isDeleted)
                {
                    System.Diagnostics.Debug.WriteLine($"BLL: Đã xóa thành công bình luận ID {idComment}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"BLL: Không tìm thấy bình luận ID {idComment} để xóa hoặc có lỗi SQL.");
                }

                return isDeleted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi BLL DeleteComment: " + ex.Message);
                return false;
            }
        }
    }
}
