-- =============================================
-- Migration: Fix lỗi thiếu cột và lỗi Check Constraint (Reminder)
-- Database: PBL3
-- =============================================
USE PBL3;
GO

-- 1. Thêm cột color_category (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PERSONAL_EVENT' AND COLUMN_NAME = 'color_category')
BEGIN
    ALTER TABLE PERSONAL_EVENT ADD color_category VARCHAR(20) DEFAULT '#1A73E8';
END
GO

-- 2. Thêm cột is_completed (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PERSONAL_EVENT' AND COLUMN_NAME = 'is_completed')
BEGIN
    ALTER TABLE PERSONAL_EVENT ADD is_completed BIT DEFAULT 0;
END
GO

-- 3. Thêm cột is_all_day (nếu chưa có)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'PERSONAL_EVENT' AND COLUMN_NAME = 'is_all_day')
BEGIN
    ALTER TABLE PERSONAL_EVENT ADD is_all_day BIT DEFAULT 0;
END
GO

-- 4. Sửa constraint để cho phép lưu sự kiện loại REMINDER
DECLARE @ConstraintName nvarchar(200)
SELECT @ConstraintName = Name FROM sys.check_constraints WHERE parent_object_id = Object_ID('PERSONAL_EVENT') AND definition LIKE '%event_type%'
IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE PERSONAL_EVENT DROP CONSTRAINT ' + @ConstraintName)
END

ALTER TABLE PERSONAL_EVENT ADD CONSTRAINT CK_PERSONAL_EVENT_type CHECK (event_type IN ('ACADEMIC','PERSONAL','DEADLINE','REMINDER'));
GO

PRINT 'Fix Event Migration completed successfully!';