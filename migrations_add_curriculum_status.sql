-- =============================================
-- Migration: Thêm cột trạng thái học cho khung chương trình
-- Database: PBL3
-- =============================================
USE PBL3;
GO

-- Thêm cột trang_thai_hoc vào bảng TICH_LUY_TIN_CHI
-- Giá trị: 'DaHoc' (Đã học), 'DangHoc' (Đang học), 'ChuaHoc' (Chưa học)
ALTER TABLE TICH_LUY_TIN_CHI 
ADD trang_thai_hoc NVARCHAR(20) DEFAULT 'ChuaHoc' 
CHECK (trang_thai_hoc IN ('DaHoc', 'DangHoc', 'ChuaHoc'));

-- Tạo index để tối ưu query
CREATE INDEX idx_sv_trang_thai ON TICH_LUY_TIN_CHI(id_sv, trang_thai_hoc);

GO

-- Cập nhật dữ liệu mặc định cho các bản ghi cũ (nếu cần)
-- Những môn học kỳ < 4 đánh dấu là "Đã học"
-- UPDATE TICH_LUY_TIN_CHI SET trang_thai_hoc = 'DaHoc' WHERE hoc_ky < 4;
-- UPDATE TICH_LUY_TIN_CHI SET trang_thai_hoc = 'DangHoc' WHERE hoc_ky = 4;
-- UPDATE TICH_LUY_TIN_CHI SET trang_thai_hoc = 'ChuaHoc' WHERE hoc_ky > 4;

GO

PRINT 'Migration completed successfully!';
