using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using StudentReminderApp.Models;

namespace StudentReminderApp.DAL
{
    public class CourseDAL
    {
        public async Task<List<LopHocPhan>> GetAvailableAsync(int hocKy, string namHoc, long idSv)
        {
            var list = new List<LopHocPhan>();
            string sql = @"
                SELECT lhp.id_lop_hp, mh.ma_mon_hoc, mh.ten_mon_hoc, mh.so_tin_chi, gv.ten_giang_vien, p.ten_phong,
                       lich.thu_trong_tuan, lich.start_time as tiet_bat_dau, lich.end_time as tiet_ket_thuc,
                       (SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END 
                        FROM DANG_KY_HOC_PHAN dk 
                        WHERE dk.id_lop_hp = lhp.id_lop_hp AND dk.id_sv = @idSv) as da_dang_ky
                FROM LOP_HOC_PHAN lhp
                JOIN MON_HOC mh ON lhp.id_mon_hoc = mh.id_mon_hoc
                JOIN GIANG_VIEN gv ON lhp.id_giang_vien = gv.id_giang_vien
                JOIN DANH_MUC_PHONG p ON lhp.id_phong = p.id_phong
                OUTER APPLY (
                    SELECT TOP 1 thu_trong_tuan, start_time, end_time
                    FROM LICH_CHI_TIET
                    WHERE id_lop_hp = lhp.id_lop_hp
                    ORDER BY thu_trong_tuan, start_time
                ) lich
                WHERE lhp.hoc_ky = @hk AND lhp.nam_hoc = @nam";

            using (var conn = new SqlConnection(AppConfig.ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@hk", hocKy);
                    cmd.Parameters.AddWithValue("@nam", namHoc);
                    cmd.Parameters.AddWithValue("@idSv", idSv);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            bool daDangKy = Convert.ToInt32(reader["da_dang_ky"]) == 1;
                            list.Add(new LopHocPhan
                            {
                                IdLopHp = Convert.ToInt64(reader["id_lop_hp"]),
                                MaMonHoc = reader["ma_mon_hoc"].ToString(),
                                TenMonHoc = reader["ten_mon_hoc"].ToString(),
                                SoTinChi = Convert.ToInt32(reader["so_tin_chi"]),
                                TenGiangVien = reader["ten_giang_vien"].ToString(),
                                TenPhong = reader["ten_phong"].ToString(),
                                ThuTrongTuan = reader["thu_trong_tuan"] == DBNull.Value ? 0 : Convert.ToInt32(reader["thu_trong_tuan"]),
                                TietBatDau = reader["tiet_bat_dau"] == DBNull.Value ? 0 : Convert.ToInt32(reader["tiet_bat_dau"]),
                                TietKetThuc = reader["tiet_ket_thuc"] == DBNull.Value ? 0 : Convert.ToInt32(reader["tiet_ket_thuc"]),
                                DaDangKy = daDangKy,
                                TrangThaiText = daDangKy ? "Đã đăng ký" : "Chưa đăng ký"
                            });
                        }
                    }
                }
            }
            return list;
        }

        public async Task<bool> RegisterAsync(long idSv, long idLopHp)
        {
            try
            {
                string sql = "INSERT INTO DANG_KY_HOC_PHAN(id_sv, id_lop_hp, status) VALUES(@sv, @lhp, 'Scheduled')";
                using var conn = new SqlConnection(AppConfig.ConnectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@sv", idSv);
                cmd.Parameters.AddWithValue("@lhp", idLopHp);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task UnregisterAsync(long idSv, long idLopHp)
        {
            string sql = "DELETE FROM DANG_KY_HOC_PHAN WHERE id_sv = @sv AND id_lop_hp = @lhp";
            using var conn = new SqlConnection(AppConfig.ConnectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sv", idSv);
            cmd.Parameters.AddWithValue("@lhp", idLopHp);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}