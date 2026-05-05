using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using StudentReminderApp.Models;
using System.Threading.Tasks;

namespace StudentReminderApp.DAL
{
    public class AdvisorDAL
    {
        public async Task<AdvisorSummary> GetSummaryAsync(long idSv, int hocKy, string namHoc)
        {
            var summary = new AdvisorSummary { RemainingCredits = 130, GPAFormatted = "0.0", GPALevel = "Chưa có", MaxCreditsAllowed = 25 };

            using (var conn = new SqlConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                // Gộp 2 câu lệnh SQL thành 1 để tránh lỗi "DataReader is already open"
                string sql = @"
                    SELECT 
                        (SELECT ISNULL(SUM(mh.so_tin_chi), 0) FROM TICH_LUY_TIN_CHI tl JOIN MON_HOC mh ON tl.id_mon_hoc = mh.id_mon_hoc WHERE tl.id_sv = @idSv AND tl.is_passed = 1) as total_tc,
                        (SELECT ISNULL(AVG(tl.diem_so), 0) FROM TICH_LUY_TIN_CHI tl WHERE tl.id_sv = @idSv AND tl.is_passed = 1) as gpa,
                        (SELECT ISNULL(SUM(mh.so_tin_chi), 0) FROM DANG_KY_HOC_PHAN dk JOIN LOP_HOC_PHAN lhp ON dk.id_lop_hp = lhp.id_lop_hp JOIN MON_HOC mh ON lhp.id_mon_hoc = mh.id_mon_hoc WHERE dk.id_sv = @idSv AND lhp.hoc_ky = @hk AND lhp.nam_hoc = @nam) as registered_tc,
                        (SELECT manual_gpa FROM PREFERENCE WHERE id_acc = @idSv) as manual_gpa,
                        (SELECT manual_credits FROM PREFERENCE WHERE id_acc = @idSv) as manual_credits
                ";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@idSv", idSv);
                    cmd.Parameters.AddWithValue("@hk", hocKy);
                    cmd.Parameters.AddWithValue("@nam", namHoc);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        summary.TotalAccumulatedCredits = Convert.ToInt32(reader["total_tc"]);
                        double gpa = Convert.ToDouble(reader["gpa"]);

                        // Ưu tiên dữ liệu thủ công nếu có
                        if (reader["manual_credits"] != DBNull.Value) summary.TotalAccumulatedCredits = Convert.ToInt32(reader["manual_credits"]);
                        if (reader["manual_gpa"] != DBNull.Value) gpa = Convert.ToDouble(reader["manual_gpa"]);

                        summary.GPAFormatted = gpa.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                        if (gpa >= 3.6) summary.GPALevel = "Xuất sắc";
                        else if (gpa >= 3.2) summary.GPALevel = "Giỏi";
                        else if (gpa >= 2.5) summary.GPALevel = "Khá";
                        else if (gpa >= 2.0) summary.GPALevel = "Trung bình";
                        else if (gpa > 0) summary.GPALevel = "Yếu";
                        summary.RemainingCredits = Math.Max(0, 130 - summary.TotalAccumulatedCredits);
                        summary.RegisteredCreditsThisTerm = Convert.ToInt32(reader["registered_tc"]);
                    }
                }
            }
            return summary;
        }

        public async Task UpdateManualStatsAsync(long idSv, double gpa, int credits)
        {
            string sql = @"
                IF EXISTS (SELECT 1 FROM PREFERENCE WHERE id_acc = @idSv)
                    UPDATE PREFERENCE SET manual_gpa = @gpa, manual_credits = @cre WHERE id_acc = @idSv
                ELSE
                    INSERT INTO PREFERENCE (id_acc, manual_gpa, manual_credits) VALUES (@idSv, @gpa, @cre)";
            using (var conn = new SqlConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idSv", idSv);
                cmd.Parameters.AddWithValue("@gpa", gpa);
                cmd.Parameters.AddWithValue("@cre", credits);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<LopHocPhan>> GetSuggestedCoursesAsync(long idSv, int hocKy, string namHoc)
        {
            var list = new List<LopHocPhan>();
            string sql = @"
                SELECT TOP 10 lhp.id_lop_hp, mh.ma_mon_hoc, mh.ten_mon_hoc, mh.so_tin_chi, gv.ten_giang_vien, p.ten_phong, ISNULL(gv.rating, 0) as rating
                FROM LOP_HOC_PHAN lhp
                JOIN MON_HOC mh ON lhp.id_mon_hoc = mh.id_mon_hoc
                JOIN GIANG_VIEN gv ON lhp.id_giang_vien = gv.id_giang_vien
                JOIN DANH_MUC_PHONG p ON lhp.id_phong = p.id_phong
                WHERE lhp.hoc_ky = @hk AND lhp.nam_hoc = @nam
                  -- 1. Chưa học hoặc học rớt
                  AND lhp.id_mon_hoc NOT IN (SELECT id_mon_hoc FROM TICH_LUY_TIN_CHI WHERE id_sv = @idSv AND is_passed = 1)
                  -- 2. Chưa đăng ký trong kỳ này
                  AND lhp.id_lop_hp NOT IN (SELECT id_lop_hp FROM DANG_KY_HOC_PHAN WHERE id_sv = @idSv)
                  -- 3. THUẬT TOÁN: Ràng buộc Tiên quyết (Đảm bảo mọi môn tiên quyết đều đã pass)
                  AND NOT EXISTS (
                      SELECT 1 FROM DIEU_KIEN_TIEN_QUYET dktq
                      WHERE dktq.id_mon_hoc = mh.id_mon_hoc
                        AND dktq.id_mon_tq NOT IN (SELECT id_mon_hoc FROM TICH_LUY_TIN_CHI WHERE id_sv = @idSv AND is_passed = 1)
                  )
                -- 4. THUẬT TOÁN: Heuristic ưu tiên Giảng viên tốt & Môn cốt lõi
                ORDER BY rating DESC, mh.so_tin_chi DESC";
            using (var conn = new SqlConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync(); // Giữ nguyên, đã đúng từ trước
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@hk", hocKy);
                cmd.Parameters.AddWithValue("@nam", namHoc);
                cmd.Parameters.AddWithValue("@idSv", idSv);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new LopHocPhan
                    {
                        IdLopHp = Convert.ToInt64(reader["id_lop_hp"]),
                        MaMonHoc = reader["ma_mon_hoc"].ToString(),
                        TenMonHoc = reader["ten_mon_hoc"].ToString(),
                        SoTinChi = Convert.ToInt32(reader["so_tin_chi"]),
                        TenGiangVien = reader["ten_giang_vien"].ToString(),
                        TenPhong = reader["ten_phong"].ToString()
                    });
                }
            }
            return list;
        }

        public async Task<List<LopHocPhan>> GetRegisteredCoursesAsync(long idSv, int hocKy, string namHoc)
        {
            var list = new List<LopHocPhan>();
            string sql = @"
                SELECT lhp.id_lop_hp, mh.ma_mon_hoc, mh.ten_mon_hoc, mh.so_tin_chi, gv.ten_giang_vien, p.ten_phong
                FROM DANG_KY_HOC_PHAN dk
                JOIN LOP_HOC_PHAN lhp ON dk.id_lop_hp = lhp.id_lop_hp
                JOIN MON_HOC mh ON lhp.id_mon_hoc = mh.id_mon_hoc
                JOIN GIANG_VIEN gv ON lhp.id_giang_vien = gv.id_giang_vien
                JOIN DANH_MUC_PHONG p ON lhp.id_phong = p.id_phong
                WHERE dk.id_sv = @idSv AND lhp.hoc_ky = @hk AND lhp.nam_hoc = @nam";
            using (var conn = new SqlConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idSv", idSv);
                cmd.Parameters.AddWithValue("@hk", hocKy);
                cmd.Parameters.AddWithValue("@nam", namHoc);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new LopHocPhan
                    {
                        IdLopHp = Convert.ToInt64(reader["id_lop_hp"]),
                        MaMonHoc = reader["ma_mon_hoc"].ToString(),
                        TenMonHoc = reader["ten_mon_hoc"].ToString(),
                        SoTinChi = Convert.ToInt32(reader["so_tin_chi"]),
                        TenGiangVien = reader["ten_giang_vien"].ToString(),
                        TenPhong = reader["ten_phong"].ToString()
                    });
                }
            }
            return list;
        }
    }
}