-- =============================================
-- Script cập nhật CSDL cho chức năng Danh mục phân cấp
-- Chạy script này trong SQL Server Management Studio
-- Database: PBL3
-- =============================================
USE PBL3;
GO

-- 1. Xóa khóa ngoại id_lop trong bảng [USER] (nếu có)
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_User_LopSinhVien]') AND parent_object_id = OBJECT_ID(N'[dbo].[USER]'))
BEGIN
    ALTER TABLE [USER] DROP CONSTRAINT FK_User_LopSinhVien;
END
GO

-- Xóa liên kết id_lop trong LOP_SINH_VIEN (có thể có trigger hoặc ràng buộc khác, nhưng mặc định SSMS không tạo FK nếu không có trong script cũ, có thể bạn đã tạo thủ công)
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[dbo].[USER]') AND referenced_object_id = OBJECT_ID(N'[dbo].[LOP_SINH_VIEN]'))
BEGIN
    DECLARE @fkName NVARCHAR(200);
    SELECT @fkName = name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[dbo].[USER]') AND referenced_object_id = OBJECT_ID(N'[dbo].[LOP_SINH_VIEN]');
    DECLARE @sql NVARCHAR(MAX) = 'ALTER TABLE [USER] DROP CONSTRAINT ' + @fkName;
    EXEC sp_executesql @sql;
END
GO

-- 2. Đổi id_lop sang ten_lop
-- Vì cột id_lop đang chứa kiểu INT, ta sẽ thêm cột ten_lop NVARCHAR(100) và ánh xạ dữ liệu cũ sang (nếu có)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[USER]') AND name = 'ten_lop')
BEGIN
    ALTER TABLE [USER] ADD ten_lop NVARCHAR(100);
    
    -- Ánh xạ dữ liệu cũ (nếu có dữ liệu)
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[USER]') AND name = 'id_lop')
    BEGIN
        EXEC('UPDATE u SET u.ten_lop = l.ten_lop FROM [USER] u INNER JOIN LOP_SINH_VIEN l ON u.id_lop = l.id_lop');
        
        -- Sau khi ánh xạ, xóa cột id_lop
        ALTER TABLE [USER] DROP COLUMN id_lop;
    END
END
GO

-- Hoàn tất!
