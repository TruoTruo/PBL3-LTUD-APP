using System.Collections.Generic;
using System.Data.SqlClient;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public class DanhMucItem
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string Value { get; set; }
        public string NienKhoa { get; set; } // Dùng cho bảng LOP_SINH_VIEN
    }

    public class DanhMucDAL : BaseDAL
    {
        // ── Lấy danh sách danh mục chung ──────────────────────────────
        public List<DanhMucItem> GetByCategory(string category)
        {
            var list = new List<DanhMucItem>();
            string sql = "SELECT Id, Category, Value FROM DANH_MUC_CHUNG WHERE Category = @cat ORDER BY Value ASC";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cat", category);
            using var rs = cmd.ExecuteReader();
            while (rs.Read())
            {
                list.Add(new DanhMucItem
                {
                    Id = rs.GetInt32(0),
                    Category = rs.GetString(1),
                    Value = rs.GetString(2)
                });
            }
            return list;
        }

        public void AddDanhMucChung(string category, string value)
        {
            string sql = "INSERT INTO DANH_MUC_CHUNG (Category, Value) VALUES (@cat, @val)";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cat", category);
            cmd.Parameters.AddWithValue("@val", value);
            cmd.ExecuteNonQuery();
        }

        public void UpdateDanhMucChung(int id, string value)
        {
            string sql = "UPDATE DANH_MUC_CHUNG SET Value = @val WHERE Id = @id";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@val", value);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteDanhMucChung(int id)
        {
            string sql = "DELETE FROM DANH_MUC_CHUNG WHERE Id = @id";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ── Quản lý riêng cho LỚP SINH HOẠT (bảng LOP_SINH_VIEN) ─────
        public List<DanhMucItem> GetAllClasses()
        {
            var list = new List<DanhMucItem>();
            string sql = "SELECT id_lop, ten_lop, nien_khoa FROM LOP_SINH_VIEN ORDER BY ten_lop ASC";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            using var rs = cmd.ExecuteReader();
            while (rs.Read())
            {
                list.Add(new DanhMucItem
                {
                    Id = rs.GetInt32(0),
                    Category = "LOP",
                    Value = rs.GetString(1),
                    NienKhoa = rs.IsDBNull(2) ? "" : rs.GetString(2)
                });
            }
            return list;
        }

        public void AddClass(string tenLop, string nienKhoa)
        {
            string sql = "INSERT INTO LOP_SINH_VIEN (ten_lop, nien_khoa) VALUES (@ten, @nk)";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ten", tenLop);
            cmd.Parameters.AddWithValue("@nk", (object)nienKhoa ?? System.DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void UpdateClass(int id, string tenLop, string nienKhoa)
        {
            string sql = "UPDATE LOP_SINH_VIEN SET ten_lop = @ten, nien_khoa = @nk WHERE id_lop = @id";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ten", tenLop);
            cmd.Parameters.AddWithValue("@nk", (object)nienKhoa ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void DeleteClass(int id)
        {
            // Set id_lop = NULL for users in this class before deleting to avoid constraint error
            string sql1 = "UPDATE [USER] SET id_lop = NULL WHERE id_lop = @id";
            string sql2 = "DELETE FROM LOP_SINH_VIEN WHERE id_lop = @id";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try {
                using (var cmd1 = new SqlCommand(sql1, conn, trans)) {
                    cmd1.Parameters.AddWithValue("@id", id);
                    cmd1.ExecuteNonQuery();
                }
                using (var cmd2 = new SqlCommand(sql2, conn, trans)) {
                    cmd2.Parameters.AddWithValue("@id", id);
                    cmd2.ExecuteNonQuery();
                }
                trans.Commit();
            }
            catch {
                trans.Rollback();
                throw;
            }
        }
    }
}
