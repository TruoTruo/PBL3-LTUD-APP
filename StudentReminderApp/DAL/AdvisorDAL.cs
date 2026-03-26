using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace StudentReminderApp.DAL
{
    public class AdvisorDAL : BaseDAL
    {
        public List<long> GetPassedCourseIds(long idSv)
        {
            const string sql = @"
                SELECT DISTINCT id_mon_hoc FROM TICH_LUY_TIN_CHI
                WHERE id_sv=@sv AND is_passed=1";
            var list = new List<long>();
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add((long)r[0]);
            return list;
        }

        public int GetRegisteredCredits(long idSv, int hocKy, string namHoc)
        {
            const string sql = @"
                SELECT ISNULL(SUM(m.so_tin_chi),0)
                FROM   DANG_KY_HOC_PHAN dk
                JOIN   LOP_HOC_PHAN lhp ON dk.id_lop_hp  = lhp.id_lop_hp
                JOIN   MON_HOC m        ON lhp.id_mon_hoc = m.id_mon_hoc
                WHERE  dk.id_sv=@sv AND lhp.hoc_ky=@hk AND lhp.nam_hoc=@nh";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            cmd.Parameters.AddWithValue("@hk", hocKy);
            cmd.Parameters.AddWithValue("@nh", namHoc);
            return (int)cmd.ExecuteScalar();
        }

        public List<long> GetPrerequisites(long idMonHoc)
        {
            const string sql =
                "SELECT id_mon_tq FROM DIEU_KIEN_TIEN_QUYET WHERE id_mon_hoc=@id";
            var list = new List<long>();
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idMonHoc);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add((long)r[0]);
            return list;
        }

        public int GetTotalAccumulatedCredits(long idSv)
        {
            const string sql = @"
                SELECT ISNULL(SUM(m.so_tin_chi),0)
                FROM   TICH_LUY_TIN_CHI tc
                JOIN   MON_HOC m ON tc.id_mon_hoc=m.id_mon_hoc
                WHERE  tc.id_sv=@sv AND tc.is_passed=1";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            return (int)cmd.ExecuteScalar();
        }

        public double GetGPA(long idSv)
        {
            const string sql = @"
                SELECT ISNULL(
                    SUM(tc.diem_so*m.so_tin_chi)/NULLIF(SUM(m.so_tin_chi),0),0)
                FROM   TICH_LUY_TIN_CHI tc
                JOIN   MON_HOC m ON tc.id_mon_hoc=m.id_mon_hoc
                WHERE  tc.id_sv=@sv AND tc.diem_so IS NOT NULL";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            var res = cmd.ExecuteScalar();
            return res == DBNull.Value ? 0.0 : Convert.ToDouble(res);
        }
    }
}
