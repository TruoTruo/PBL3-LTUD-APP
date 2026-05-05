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
            try
            {
                return _forumDAL.GetPosts();
            }
            catch (InvalidOperationException iex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL Error in GetAllPosts: {iex.Message}");
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {iex.InnerException?.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error in GetAllPosts: {ex.Message}");
                throw new InvalidOperationException("Lỗi khi lấy danh sách bài viết", ex);
            }
        }

        public List<Comment> GetComments(long idPost)
        {
            try
            {
                return _forumDAL.GetCommentsByPostId(idPost);
            }
            catch (InvalidOperationException iex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL Error in GetComments: {iex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error in GetComments: {ex.Message}");
                throw new InvalidOperationException("Lỗi khi lấy bình luận", ex);
            }
        }

        public bool PostComment(long idPost, long idAcc, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            try
            {
                return _forumDAL.AddComment(idPost, idAcc, content);
            }
            catch (InvalidOperationException iex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL Error in PostComment: {iex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error in PostComment: {ex.Message}");
                throw new InvalidOperationException("Lỗi khi đăng bình luận", ex);
            }
        }

        public bool ToggleLike(long idAcc, long idPost)
        {
            if (idAcc <= 0 || idPost <= 0) return false;
            try
            {
                return _forumDAL.ToggleLike(idAcc, idPost);
            }
            catch (InvalidOperationException iex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL Error in ToggleLike: {iex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL ToggleLike Error: {ex.Message}");
                throw new InvalidOperationException("Lỗi khi toggle like", ex);
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
                        try
                        {
                            if (File.Exists(originalPath))
                            {
                                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(originalPath);
                                string destPath = Path.Combine(folderPath, fileName);

                                File.Copy(originalPath, destPath, true);

                                _forumDAL.AddDocument(newPostId, fileName, fileName);
                            }
                        }
                        catch (IOException ioEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"File operation error: {ioEx.Message}");
                            throw new InvalidOperationException($"Lỗi xử lý file {originalPath}", ioEx);
                        }
                    }
                    return true;
                }
                return false;
            }
            catch (InvalidOperationException iex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL Error in CreatePost: {iex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu bài viết: {ex.Message}");
                throw new InvalidOperationException("Lỗi khi tạo bài viết", ex);
            }
        }

        public bool SharePost(long idPostGoc, long idAccNguoiChiaSe, string noiDungThem, bool laCongKhai)
        {
            if (idPostGoc <= 0 || idAccNguoiChiaSe <= 0) return false;

            try
            {
                
                return CreatePost(idAccNguoiChiaSe, "Chia sẻ bài viết", noiDungThem, false, null, idPostGoc, "Transparent");
            }
            catch (InvalidOperationException iex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL Error in SharePost: {iex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL SharePost Error: {ex.Message}");
                throw new InvalidOperationException("Lỗi khi chia sẻ bài viết", ex);
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
            catch (InvalidOperationException iex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL Error in RemovePost: {iex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL RemovePost Error: {ex.Message}");
                throw new InvalidOperationException("Lỗi khi xóa bài viết", ex);
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
            catch (InvalidOperationException iex)
            {
                System.Diagnostics.Debug.WriteLine($"BLL Error in DeleteComment: {iex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi BLL DeleteComment: {ex.Message}");
                throw new InvalidOperationException("Lỗi khi xóa bình luận", ex);
            }
        }
    }
}