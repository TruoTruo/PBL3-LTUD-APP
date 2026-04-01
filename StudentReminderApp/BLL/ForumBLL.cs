using System;
using System.Collections.Generic;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;

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

        /// Xử lý Like/Unlike bài viết
    
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

        /// Tạo bài viết
        public bool CreatePost(long idAcc, string title, string content, bool isAnonymous, long? idOriginalPost = null)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;

            try
            {
                return _forumDAL.AddPost(idAcc, title, content, isAnonymous, idOriginalPost);
            }
            catch (Exception ex)
            {
                Console.WriteLine("BLL CreatePost Error: " + ex.Message);
                return false;
            }
        }

        /// chia sẻ bài viết
        public bool SharePost(long idPostGoc, long idAccNguoiChiaSe, string noiDungThem, bool laCongKhai)
        {
            if (idPostGoc <= 0 || idAccNguoiChiaSe <= 0) return false;

            try
            {
                return CreatePost(idAccNguoiChiaSe, "Chia sẻ bài viết", noiDungThem, false, idPostGoc);
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
    }
}