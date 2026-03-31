using System.Collections.Generic;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;
using System;

namespace StudentReminderApp.BLL
{
    public class ForumBLL
    {
        private readonly ForumDAL _forumDAL = new ForumDAL();

        public List<Post> GetAllPosts()
        {
            return _forumDAL.GetPosts();
        }

        // Thêm tham số bool isAnonymous vào đây
        public bool CreatePost(long idAcc, string title, string content, bool isAnonymous)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            try
            {
                // Truyền đủ 4 tham số xuống tầng DAL
                return _forumDAL.AddPost(idAcc, title, content, isAnonymous);
            }
            catch (Exception ex)
            {
                Console.WriteLine("BLL Error: " + ex.Message);
                return false;
            }
        }

        public bool RemovePost(long idPost)
        {
            // Gọi thông qua đối tượng đã khởi tạo
            return _forumDAL.DeletePost(idPost);
        }
    }
}