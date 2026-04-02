// DAL/DatabaseSetup.cs
using System;
using System.Data.SqlClient;
namespace StudentReminderApp.DAL
{
    public class DatabaseSetup
    {
        private readonly string _connectionString;

        public DatabaseSetup(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void CreateStoredProcedures()
        {
            // Đọc file SQL chứa stored procedure
            string sqlScript = @"
            CREATE PROCEDURE sp_GetSuggestedCourses
                @id_acc INT,
                @hoc_ky INT,
                @nam_hoc VARCHAR(20)
            AS
            BEGIN
                SET NOCOUNT ON;
                -- Nội dung stored procedure...
            END
        ";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Kiểm tra nếu stored procedure đã tồn tại thì xóa
                string dropSql = "IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetSuggestedCourses') DROP PROCEDURE sp_GetSuggestedCourses";
                using (var cmd = new SqlCommand(dropSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Tạo mới
                using (var cmd = new SqlCommand(sqlScript, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Tạo nhiều stored procedure cùng lúc
        public void CreateAllStoredProcedures()
        {
            string[] procedures = {
            GetSuggestedCoursesScript(),
            GetStudentSummaryScript(),
            GetAvailableCoursesScript()  // Đã thêm phương thức này
        };

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                foreach (var script in procedures)
                {
                    using (var cmd = new SqlCommand(script, conn))
                    {
                        try
                        {
                            cmd.ExecuteNonQuery();
                            Console.WriteLine("Created stored procedure successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                    }
                }
            }
        }

        private string GetSuggestedCoursesScript()
        {
            return @"
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetSuggestedCourses')
    DROP PROCEDURE sp_GetSuggestedCourses

CREATE PROCEDURE sp_GetSuggestedCourses
    @id_acc INT,
    @hoc_ky INT,
    @nam_hoc VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH CompletedCourses AS (
        SELECT DISTINCT lhp.ma_hp
        FROM DANG_KY_HOC_PHAN dk
        JOIN LOP_HOC_PHAN lhp ON dk.id_lop_hp = lhp.id_lop_hp
        WHERE dk.id_acc = @id_acc 
          AND dk.trang_thai = N'Hoàn thành'
          AND dk.diem_thi >= 5.0
    ),
    CurrentCourses AS (
        SELECT DISTINCT lhp.ma_hp
        FROM DANG_KY_HOC_PHAN dk
        JOIN LOP_HOC_PHAN lhp ON dk.id_lop_hp = lhp.id_lop_hp
        WHERE dk.id_acc = @id_acc 
          AND dk.trang_thai = N'Đã đăng ký'
          AND lhp.hoc_ky = @hoc_ky
          AND lhp.nam_hoc = @nam_hoc
    )
    
    SELECT 
        lhp.id_lop_hp,
        lhp.ma_hp,
        hp.ten_hp,
        hp.so_tin_chi,
        lhp.ten_giang_vien,
        lhp.ten_phong,
        lhp.hoc_ky,
        lhp.nam_hoc
    FROM LOP_HOC_PHAN lhp
    INNER JOIN HOC_PHAN hp ON lhp.ma_hp = hp.ma_hp
    WHERE lhp.hoc_ky = @hoc_ky 
      AND lhp.nam_hoc = @nam_hoc
      AND lhp.is_open = 1
      AND hp.ma_hp NOT IN (SELECT ma_hp FROM CompletedCourses)
      AND hp.ma_hp NOT IN (SELECT ma_hp FROM CurrentCourses)
    ORDER BY hp.hoc_ky_khuyen_nghi ASC;
END";
        }

        private string GetStudentSummaryScript()
        {
            return @"
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetStudentSummary')
    DROP PROCEDURE sp_GetStudentSummary

CREATE PROCEDURE sp_GetStudentSummary
    @id_acc INT,
    @hoc_ky INT,
    @nam_hoc VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        ISNULL(SUM(CASE WHEN dk.trang_thai = N'Hoàn thành' AND dk.diem_thi >= 5.0 THEN hp.so_tin_chi END), 0) AS TotalCredits,
        ISNULL(SUM(CASE WHEN dk.trang_thai = N'Đã đăng ký' AND lhp.hoc_ky = @hoc_ky AND lhp.nam_hoc = @nam_hoc THEN hp.so_tin_chi END), 0) AS RegisteredCredits
    FROM DANG_KY_HOC_PHAN dk
    JOIN LOP_HOC_PHAN lhp ON dk.id_lop_hp = lhp.id_lop_hp
    JOIN HOC_PHAN hp ON lhp.ma_hp = hp.ma_hp
    WHERE dk.id_acc = @id_acc;
END";
        }

        private string GetAvailableCoursesScript()
        {
            return @"
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetAvailableCourses')
    DROP PROCEDURE sp_GetAvailableCourses

CREATE PROCEDURE sp_GetAvailableCourses
    @id_acc INT,
    @hoc_ky INT,
    @nam_hoc VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Lấy danh sách khóa học có sẵn trong học kỳ hiện tại
    SELECT 
        lhp.id_lop_hp,
        lhp.ma_hp,
        hp.ten_hp,
        hp.so_tin_chi,
        lhp.ten_giang_vien,
        lhp.ten_phong,
        lhp.si_so_toi_da,
        lhp.si_so_hien_tai,
        lhp.hoc_ky,
        lhp.nam_hoc,
        lhp.thoi_gian_hoc,
        (lhp.si_so_toi_da - lhp.si_so_hien_tai) AS con_trong,
        CASE 
            WHEN EXISTS (
                SELECT 1 FROM DANG_KY_HOC_PHAN dk 
                WHERE dk.id_lop_hp = lhp.id_lop_hp 
                AND dk.id_acc = @id_acc
                AND dk.trang_thai IN (N'Đã đăng ký', N'Chờ duyệt')
            ) THEN 1 
            ELSE 0 
        END AS da_dang_ky
    FROM LOP_HOC_PHAN lhp
    INNER JOIN HOC_PHAN hp ON lhp.ma_hp = hp.ma_hp
    WHERE lhp.hoc_ky = @hoc_ky 
      AND lhp.nam_hoc = @nam_hoc
      AND lhp.is_open = 1
      AND lhp.si_so_hien_tai < lhp.si_so_toi_da  -- Còn chỗ trống
    ORDER BY hp.ten_hp ASC;
END";
        }
    }
}