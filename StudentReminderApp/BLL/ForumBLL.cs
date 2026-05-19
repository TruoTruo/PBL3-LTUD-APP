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

            if (newStatus != PostStatus.Pending &&
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
                // ✅ Kiểm tra role để quyết định approval_status
                bool isAdmin = SessionManager.IsAdmin;

                long newPostId = _forumDAL.InsertPost(
                    idAcc, title, content, isPublic,
                    idPostGoc, theme,
                    isAdmin); // ✅ truyền isAdmin xuống DAL

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
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi lưu bài viết (BLL): " + ex.Message);
                return false;
            }
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

        // ===============================================================
        //  HOT POSTS — Quick Sort theo TrendingScore (giảm dần)
        //  Độ phức tạp: O(N log N) trung bình, O(N²) worst-case
        //  Pivot: phần tử giữa (giảm xác suất worst-case với dữ liệu đã sắp)
        // ===============================================================
        public List<Post> GetHotPosts()
        {
            try
            {
                List<Post> posts = _forumDAL.GetHotPosts();
                QuickSortByTrending(posts, 0, posts.Count - 1);
                return posts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BLL GetHotPosts Error: " + ex.Message);
                return new List<Post>();
            }
        }

        /// <summary>
        /// Quick Sort đệ quy — sắp xếp List&lt;Post&gt; theo TrendingScore GIẢM DẦN.
        /// Gọi ngoài: QuickSortByTrending(list, 0, list.Count - 1)
        /// </summary>
        private void QuickSortByTrending(List<Post> posts, int low, int high)
        {
            if (low >= high) return;                          // base case
            int pivotIndex = PartitionByTrending(posts, low, high);
            QuickSortByTrending(posts, low, pivotIndex - 1);  // sắp nửa trái
            QuickSortByTrending(posts, pivotIndex + 1, high); // sắp nửa phải
        }

        /// <summary>
        /// Partition: chọn pivot là phần tử GIỮA, đưa về đúng vị trí.
        /// Trả về chỉ số pivot sau khi partition xong.
        /// </summary>
        private int PartitionByTrending(List<Post> posts, int low, int high)
        {
            // Chọn pivot là phần tử giữa (median-of-3 đơn giản)
            int mid = low + (high - low) / 2;
            double pivotScore = posts[mid].TrendingScore;

            // Đưa pivot về cuối để tránh tính toán thừa
            Swap(posts, mid, high);

            int i = low - 1; // con trỏ vùng "lớn hơn pivot"

            for (int j = low; j < high; j++)
            {
                // Sắp xếp GIẢM DẦN → đưa phần tử LỚN HƠN pivot lên trước
                if (posts[j].TrendingScore > pivotScore)
                {
                    i++;
                    Swap(posts, i, j);
                }
            }

            // Đưa pivot về đúng vị trí
            Swap(posts, i + 1, high);
            return i + 1;
        }

        private void Swap(List<Post> posts, int a, int b)
        {
            Post temp = posts[a];
            posts[a] = posts[b];
            posts[b] = temp;
        }

        // ===============================================================
        //  KMP — Knuth-Morris-Pratt String Matching
        //  Độ phức tạp: O(N + M)  với N = len(text), M = len(pattern)
        //  Tiền xử lý pattern: O(M) — xây LPS array (Longest Proper Prefix-Suffix)
        //  Tìm kiếm:           O(N) — con trỏ text không bao giờ lùi
        // ===============================================================

        /// <summary>
        /// Xây dựng mảng LPS (Longest Proper Prefix which is also Suffix).
        /// lps[i] = độ dài prefix dài nhất của pattern[0..i] cũng là suffix của nó.
        /// Dùng để bỏ qua ký tự trùng khi mismatch — tránh so sánh lại từ đầu.
        /// </summary>
        private int[] ComputeLPSArray(string pattern)
        {
            int m = pattern.Length;
            int[] lps = new int[m];
            lps[0] = 0;         // prefix dài nhất của pattern[0..0] = 0

            int len = 0;        // độ dài prefix-suffix hiện tại
            int i = 1;

            while (i < m)
            {
                if (pattern[i] == pattern[len])
                {
                    len++;
                    lps[i] = len;
                    i++;
                }
                else
                {
                    if (len != 0)
                        len = lps[len - 1]; // nhảy về prefix-suffix ngắn hơn
                    else
                    {
                        lps[i] = 0;
                        i++;
                    }
                }
            }
            return lps;
        }

        /// <summary>
        /// Tìm kiếm pattern trong text bằng KMP. Trả về true nếu tìm thấy.
        /// So sánh case-insensitive (chuyển về chữ thường trước).
        /// </summary>
        private bool KmpSearch(string text, string pattern)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) return false;

            text = text.ToLower();
            pattern = pattern.ToLower();

            int n = text.Length;
            int m = pattern.Length;
            int[] lps = ComputeLPSArray(pattern);

            int i = 0; // con trỏ text
            int j = 0; // con trỏ pattern

            while (i < n)
            {
                if (text[i] == pattern[j])
                {
                    i++; j++;
                }

                if (j == m)
                    return true;            // khớp hoàn toàn
                else if (i < n && text[i] != pattern[j])
                {
                    if (j != 0)
                        j = lps[j - 1];    // bỏ qua ký tự đã khớp nhờ LPS
                    else
                        i++;
                }
            }
            return false;
        }

        /// <summary>
        /// Kiểm tra title + content có chứa từ cấm không (dùng KMP).
        /// Trả về true và từ vi phạm qua out parameter nếu phát hiện.
        /// </summary>
        public bool IsContentToxic(string title, string content, out string matchedWord)
        {
            // ── Danh sách từ cấm — bổ sung tùy dự án ──────────────────
            string[] bannedWords =
            {
        "bậy", "chửi", "toxic", "ngu", "đần", "khùng",
        "điên", "phản động", "hack", "spam", "scam"
    };

            string combined = (title ?? "") + " " + (content ?? "");

            foreach (string word in bannedWords)
            {
                if (KmpSearch(combined, word))
                {
                    matchedWord = word;
                    return true;
                }
            }

            matchedWord = string.Empty;
            return false;
        }

        // -------------------------------------------------------
        // GetStudentPosts: Bài từ sinh viên (không phải Admin)
        // -------------------------------------------------------
        public List<Post> GetStudentPosts()
        {
            try
            {
                return _forumDAL.GetStudentPosts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BLL GetStudentPosts Error: " + ex.Message);
                return new List<Post>();
            }
        }

        // -------------------------------------------------------
        // GetAnnouncementPosts: Bảng tin chính thống từ Admin
        // -------------------------------------------------------
        public List<Post> GetAnnouncementPosts()
        {
            try
            {
                return _forumDAL.GetAnnouncementPosts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BLL GetAnnouncementPosts Error: " + ex.Message);
                return new List<Post>();
            }
        }

    }
}