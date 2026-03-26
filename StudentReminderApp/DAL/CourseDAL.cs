using System.Collections.Generic;
using System.Data.SqlClient;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public class CourseDAL : BaseDAL
    {
        public List<LopHocPhan> GetAvailable(int hocKy, string namHoc, long idSv)
        {
            const string sql = @"
                SELECT lhp.id_lop_hp, lhp.ma_lop_hp, lhp.hoc_ky, lhp.nam_hoc,
                       m.id_mon_hoc, m.ma_mon_hoc, m.ten_mon_hoc, m.so_tin_chi,
                       g.id_giang_vien, g.ten_giang_vien,
                       p.id_phong, p.ten_phong,
                       CASE WHEN dk.id_dang_ky IS NOT NULL THEN 1 ELSE 0 END AS da_dang_ky
                FROM   LOP_HOC_PHAN lhp
                JOIN   MON_HOC m        ON lhp.id_mon_hoc    = m.id_mon_hoc
                JOIN   GIANG_VIEN g     ON lhp.id_giang_vien = g.id_giang_vien
                JOIN   DANH_MUC_PHONG p ON lhp.id_phong      = p.id_phong
                LEFT JOIN DANG_KY_HOC_PHAN dk
                       ON dk.id_lop_hp = lhp.id_lop_hp AND dk.id_sv = @sv
                WHERE  lhp.hoc_ky=@hk AND lhp.nam_hoc=@nh";
            var list = new List<LopHocPhan>();
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hk", hocKy);
            cmd.Parameters.AddWithValue("@nh", namHoc);
            cmd.Parameters.AddWithValue("@sv", idSv);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new LopHocPhan
                {
                    IdLopHp      = (long)r["id_lop_hp"],
                    MaLopHp      = r["ma_lop_hp"]?.ToString(),
                    IdMonHoc     = (long)r["id_mon_hoc"],
                    MaMonHoc     = r["ma_mon_hoc"].ToString(),
                    TenMonHoc    = r["ten_mon_hoc"].ToString(),
                    SoTinChi     = (int)r["so_tin_chi"],
                    IdGiangVien  = (long)r["id_giang_vien"],
                    TenGiangVien = r["ten_giang_vien"].ToString(),
                    IdPhong      = (long)r["id_phong"],
                    TenPhong     = r["ten_phong"].ToString(),
                    HocKy        = (int)r["hoc_ky"],
                    NamHoc       = r["nam_hoc"].ToString(),
                    DaDangKy     = (int)r["da_dang_ky"] == 1
                });
            return list;
        }

        public void Register(long idSv, long idLopHp)
        {
            const string sql = @"
                INSERT INTO DANG_KY_HOC_PHAN(id_sv,id_lop_hp,status)
                VALUES(@sv,@lhp,'Scheduled')";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv",  idSv);
            cmd.Parameters.AddWithValue("@lhp", idLopHp);
            cmd.ExecuteNonQuery();
        }

        public void Unregister(long idSv, long idLopHp)
        {
            const string sql =
                "DELETE FROM DANG_KY_HOC_PHAN WHERE id_sv=@sv AND id_lop_hp=@lhp";
            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv",  idSv);
            cmd.Parameters.AddWithValue("@lhp", idLopHp);
            cmd.ExecuteNonQuery();
        }
    }
}
