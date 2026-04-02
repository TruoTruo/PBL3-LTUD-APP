// DAL/AdvisorDAL.cs (Cải tiến)
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace StudentReminderApp.DAL
{
    public class AdvisorDAL : BaseDAL
    {
        // Lấy danh sách ID môn đã qua (có điểm >= 5)
        public List<long> GetPassedCourseIds(long idSv)
        {
            const string sql = @"
                SELECT DISTINCT tc.id_mon_hoc 
                FROM TICH_LUY_TIN_CHI tc
                WHERE tc.id_sv = @sv 
                  AND tc.is_passed = 1
                  AND tc.diem_so >= 5.0";

            var list = new List<long>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(Convert.ToInt64(r[0]));

            return list;
        }

        // Lấy số tín chỉ đã đăng ký trong học kỳ
        public int GetRegisteredCredits(long idSv, int hocKy, string namHoc)
        {
            const string sql = @"
                SELECT ISNULL(SUM(m.so_tin_chi), 0)
                FROM DANG_KY_HOC_PHAN dk
                JOIN LOP_HOC_PHAN lhp ON dk.id_lop_hp = lhp.id_lop_hp
                JOIN MON_HOC m ON lhp.id_mon_hoc = m.id_mon_hoc
                WHERE dk.id_sv = @sv 
                  AND dk.trang_thai = N'Đã đăng ký'
                  AND lhp.hoc_ky = @hk 
                  AND lhp.nam_hoc = @nh";

            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            cmd.Parameters.AddWithValue("@hk", hocKy);
            cmd.Parameters.AddWithValue("@nh", namHoc);
            conn.Open();

            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        // Lấy danh sách điều kiện tiên quyết (có phân loại)
        public List<PrerequisiteDetail> GetPrerequisitesWithType(long idMonHoc)
        {
            const string sql = @"
                SELECT id_mon_yeu_cau, loai_dieu_kien 
                FROM DIEU_KIEN_TIEN_QUYET 
                WHERE id_mon_hoc = @id";

            var list = new List<PrerequisiteDetail>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idMonHoc);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new PrerequisiteDetail
                {
                    IdMonYeuCau = Convert.ToInt64(r["id_mon_yeu_cau"]),
                    LoaiDieuKien = r["loai_dieu_kien"].ToString()
                });
            }

            return list;
        }

        // Lấy tổng tín chỉ tích lũy
        public int GetTotalAccumulatedCredits(long idSv)
        {
            const string sql = @"
                SELECT ISNULL(SUM(m.so_tin_chi), 0)
                FROM TICH_LUY_TIN_CHI tc
                JOIN MON_HOC m ON tc.id_mon_hoc = m.id_mon_hoc
                WHERE tc.id_sv = @sv 
                  AND tc.is_passed = 1
                  AND tc.diem_so >= 5.0";

            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            conn.Open();

            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }

        // Lấy GPA tích lũy
        public double GetGPA(long idSv)
        {
            const string sql = @"
                SELECT ISNULL(
                    SUM(tc.diem_so * m.so_tin_chi) / NULLIF(SUM(m.so_tin_chi), 0), 
                    0
                ) AS GPA
                FROM TICH_LUY_TIN_CHI tc
                JOIN MON_HOC m ON tc.id_mon_hoc = m.id_mon_hoc
                WHERE tc.id_sv = @sv 
                  AND tc.is_passed = 1
                  AND tc.diem_so IS NOT NULL";

            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            conn.Open();

            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0.0 : Convert.ToDouble(result);
        }

        // Lấy danh sách môn đã học chi tiết
        public List<HocPhanDaHocDetail> GetCompletedCoursesDetail(long idSv)
        {
            const string sql = @"
                SELECT 
                    tc.id_mon_hoc,
                    m.ten_mon_hoc,
                    m.so_tin_chi,
                    tc.diem_so,
                    tc.hoc_ky,
                    tc.nam_hoc
                FROM TICH_LUY_TIN_CHI tc
                JOIN MON_HOC m ON tc.id_mon_hoc = m.id_mon_hoc
                WHERE tc.id_sv = @sv 
                  AND tc.is_passed = 1
                  AND tc.diem_so >= 5.0
                ORDER BY tc.hoc_ky, tc.nam_hoc";

            var list = new List<HocPhanDaHocDetail>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new HocPhanDaHocDetail
                {
                    IdMonHoc = Convert.ToInt64(r["id_mon_hoc"]),
                    TenMonHoc = r["ten_mon_hoc"].ToString(),
                    SoTinChi = Convert.ToDecimal(r["so_tin_chi"]),
                    Diem = Convert.ToDouble(r["diem_so"]),
                    HocKy = Convert.ToInt32(r["hoc_ky"]),
                    NamHoc = r["nam_hoc"].ToString()
                });
            }

            return list;
        }

        // Kiểm tra sinh viên đã học môn nào chưa
        public bool HasCompletedCourse(long idSv, long idMonHoc)
        {
            const string sql = @"
                SELECT COUNT(1) 
                FROM TICH_LUY_TIN_CHI 
                WHERE id_sv = @sv 
                  AND id_mon_hoc = @mon 
                  AND is_passed = 1
                  AND diem_so >= 5.0";

            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            cmd.Parameters.AddWithValue("@mon", idMonHoc);
            conn.Open();

            return (int)cmd.ExecuteScalar() > 0;
        }

        // Lấy danh sách môn đang học trong học kỳ
        public List<long> GetCurrentCourses(long idSv, int hocKy, string namHoc)
        {
            const string sql = @"
                SELECT DISTINCT lhp.id_mon_hoc
                FROM DANG_KY_HOC_PHAN dk
                JOIN LOP_HOC_PHAN lhp ON dk.id_lop_hp = lhp.id_lop_hp
                WHERE dk.id_sv = @sv 
                  AND dk.trang_thai = N'Đã đăng ký'
                  AND lhp.hoc_ky = @hk 
                  AND lhp.nam_hoc = @nh";

            var list = new List<long>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            cmd.Parameters.AddWithValue("@hk", hocKy);
            cmd.Parameters.AddWithValue("@nh", namHoc);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(Convert.ToInt64(r[0]));

            return list;
        }
    }

    // Lớp hỗ trợ
    public class PrerequisiteDetail
    {
        public long IdMonYeuCau { get; set; }
        public string LoaiDieuKien { get; set; } // TIEN_QUYET, HOC_TRUOC, SONG_HANH
    }

    public class HocPhanDaHocDetail
    {
        public long IdMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public decimal SoTinChi { get; set; }
        public double Diem { get; set; }
        public int HocKy { get; set; }
        public string NamHoc { get; set; }
    }
}