using System;
using System.Collections.Generic;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;
using StudentReminderApp.Helpers;
using System.IO;

namespace StudentReminderApp.BLL
{
    public class ForumBLL
    {
        private readonly ForumDAL _forumDAL = new ForumDAL();

        private bool IsCurrentUserAdmin()
        {
            return SessionManager.IsAdmin;
        }

        public List<Post> GetAllPosts() => GetApprovedPosts();

        public List<Post> GetApprovedPosts()
        {
            return _forumDAL.GetPosts();
        }

        public List<Post> GetPendingPosts()
        {
            if (!IsCurrentUserAdmin())
            {
                System.Diagnostics.Debug.WriteLine("BLL GetPendingPosts: Không có quyền Admin!");
                return new List<Post>();
            }
            return _forumDAL.GetPendingPosts();
        }

        public bool UpdatePostStatus(long idPost, int newStatus, string? reason = null)
        {
            if (idPost <= 0) return false;

            if (newStatus != PostStatus.Pending  &&
                newStatus != PostStatus.Approved &&
                newStatus != PostStatus.Rejected)
                return false;

            if (SessionManager.CurrentAccount == null || !IsCurrentUserAdmin())
            {
                System.Diagnostics.Debug.WriteLine("BLL UpdatePostStatus: Không có quyền Admin!");
                return false;
            }

            if (newStatus == PostStatus.Rejected && string.IsNullOrWhiteSpace(reason))
                reason = "Vi phạm nội quy diễn đàn.";

            try
            {
                return _forumDAL.UpdatePostStatus(
                    idPost, newStatus,
                    SessionManager.CurrentAccount.IdAcc,
                    reason);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BLL UpdatePostStatus Error: " + ex.Message);
                return false;
            }
        }

        public bool AdminDeletePost(long idPost)
        {
            if (idPost <= 0) return false;

            if (SessionManager.CurrentAccount == null || !IsCurrentUserAdmin())
            {
                System.Diagnostics.Debug.WriteLine("BLL AdminDeletePost: Không có quyền Admin!");
                return false;
            }

            try
            {
                return _forumDAL.AdminDeletePost(idPost, SessionManager.CurrentAccount.IdAcc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BLL AdminDeletePost Error: " + ex.Message);
                return false;
            }
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
            try { return _forumDAL.ToggleLike(idAcc, idPost); }
            catch (Exception ex) { Console.WriteLine("BLL ToggleLike Error: " + ex.Message); return false; }
        }

        public bool CreatePost(long idAcc, string title, string content, bool isPublic,
                               List<string> filePaths, long? idPostGoc = null, string theme = "Transparent")
        {
            try
            {
                long newPostId = _forumDAL.InsertPost(idAcc, title, content, isPublic, idPostGoc, theme);
                if (newPostId > 0 && filePaths != null)
                {
                    string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                    string[] allowedExtensions = { ".jpg", ".png", ".jpeg", ".bmp", ".gif" };
                    foreach (string originalPath in filePaths)
                    {
                        if (File.Exists(originalPath))
                        {
                            string ext = Path.GetExtension(originalPath).ToLower();
                            if (Array.Exists(allowedExtensions, e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                            {
                                string fileName = Guid.NewGuid().ToString() + ext;
                                string destPath = Path.Combine(folderPath, fileName);
                                File.Copy(originalPath, destPath, true);
                                _forumDAL.AddDocument(newPostId, fileName, destPath);
                            }
                        }
                    }
                    return true;
                }
                return newPostId > 0;
            }
            catch (Exception ex) { Console.WriteLine("Lỗi khi lưu bài viết (BLL): " + ex.Message); return false; }
        }

        public bool SharePost(long idPostGoc, long idAccNguoiChiaSe, string noiDungThem, bool laCongKhai)
        {
            if (idPostGoc <= 0 || idAccNguoiChiaSe <= 0) return false;
            try
            {
                return CreatePost(idAccNguoiChiaSe, "Chia sẻ bài viết", noiDungThem,
                                  false, new List<string>(), idPostGoc, "Transparent");
            }
            catch (Exception ex) { Console.WriteLine("BLL SharePost Error: " + ex.Message); return false; }
        }

        public bool RemovePost(long idPost)
        {
            if (idPost <= 0) return false;
            try { return _forumDAL.DeletePost(idPost); }
            catch (Exception ex) { Console.WriteLine("BLL RemovePost Error: " + ex.Message); return false; }
        }

        public bool DeleteComment(long idComment)
        {
            if (idComment <= 0) return false;
            try { return _forumDAL.DeleteComment(idComment); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Lỗi BLL DeleteComment: " + ex.Message); return false; }
        }
    }
}
