﻿-- =============================================
-- StudentReminderApp - Full Database Setup
-- Chạy script này trong SQL Server Management Studio
-- Database: PBL3
-- =============================================

CREATE DATABASE PBL3;
GO
USE PBL3;
GO

-- ROLES
CREATE TABLE ROLES
(
    id_role BIGINT PRIMARY KEY IDENTITY(1,1),
    role_name NVARCHAR(50) NOT NULL
);

-- ACCOUNT
CREATE TABLE ACCOUNT
(
    id_acc BIGINT PRIMARY KEY IDENTITY(1,1),
    username NVARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    id_role BIGINT,
    status NVARCHAR(20) DEFAULT 'Active' CHECK (status IN ('Active','Banned')),
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Account_Role FOREIGN KEY (id_role) REFERENCES ROLES(id_role)
);
CREATE INDEX idx_role   ON ACCOUNT(id_role);
CREATE INDEX idx_status ON ACCOUNT(status);

-- USER
CREATE TABLE [USER]
(
    id_acc BIGINT PRIMARY KEY,
    ho_ten NVARCHAR(100) NOT NULL,
    ngay_sinh DATETIME,
    sdt VARCHAR(15),
    email VARCHAR(100),
    CONSTRAINT FK_User_Account FOREIGN KEY (id_acc) REFERENCES ACCOUNT(id_acc)
);

-- USER_LOG
CREATE TABLE USER_LOG
(
    id_log BIGINT PRIMARY KEY IDENTITY(1,1),
    hanh_dong NVARCHAR(255),
    thoi_gian DATETIME DEFAULT GETDATE(),
    ip_address NVARCHAR(45),
    id_acc BIGINT,
    CONSTRAINT FK_Log_User FOREIGN KEY (id_acc) REFERENCES [USER](id_acc)
);
CREATE INDEX idx_user_activity ON USER_LOG(id_acc, thoi_gian);

-- USER_DEVICE
CREATE TABLE USER_DEVICE
(
    id_device BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT NOT NULL,
    fcm_token VARCHAR(255) NOT NULL,
    device_type NVARCHAR(20) CHECK (device_type IN ('IOS','Android','Web')),
    last_login DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Device_UserAcc FOREIGN KEY (id_acc) REFERENCES [USER](id_acc)
);

-- MON_HOC
CREATE TABLE MON_HOC
(
    id_mon_hoc BIGINT PRIMARY KEY IDENTITY(1,1),
    ma_mon_hoc VARCHAR(20) NOT NULL,
    ten_mon_hoc NVARCHAR(255) NOT NULL,
    so_tin_chi INT DEFAULT 3
);
CREATE INDEX idx_ma_mon ON MON_HOC(ma_mon_hoc);

-- GIANG_VIEN
CREATE TABLE GIANG_VIEN
(
    id_giang_vien BIGINT PRIMARY KEY IDENTITY(1,1),
    ten_giang_vien NVARCHAR(100) NOT NULL,
    email VARCHAR(100),
    sdt VARCHAR(15),
    khoa NVARCHAR(100),
    rating FLOAT DEFAULT 5.0
);

-- DANH_MUC_PHONG
CREATE TABLE DANH_MUC_PHONG
(
    id_phong BIGINT PRIMARY KEY IDENTITY(1,1),
    ten_phong VARCHAR(20) NOT NULL,
    toa_nha NVARCHAR(50),
    loai_phong VARCHAR(10) CHECK (loai_phong IN ('LT','TH')),
    thiet_bi NVARCHAR(255),
    suc_chua INT DEFAULT 50
);

-- LOP_HOC_PHAN
CREATE TABLE LOP_HOC_PHAN
(
    id_lop_hp BIGINT PRIMARY KEY IDENTITY(1,1),
    id_mon_hoc BIGINT,
    id_giang_vien BIGINT,
    id_phong BIGINT,
    hoc_ky INT,
    nam_hoc VARCHAR(20),
    CONSTRAINT FK_LHP_MonHoc    FOREIGN KEY (id_mon_hoc)    REFERENCES MON_HOC(id_mon_hoc),
    CONSTRAINT FK_LHP_GiangVien FOREIGN KEY (id_giang_vien) REFERENCES GIANG_VIEN(id_giang_vien),
    CONSTRAINT FK_LHP_Phong     FOREIGN KEY (id_phong)      REFERENCES DANH_MUC_PHONG(id_phong)
);
CREATE INDEX idx_lhp_hoc_ky    ON LOP_HOC_PHAN(hoc_ky, nam_hoc);
CREATE INDEX idx_lhp_giang_vien ON LOP_HOC_PHAN(id_giang_vien);

-- LICH_CHI_TIET
CREATE TABLE LICH_CHI_TIET
(
    id_lich BIGINT PRIMARY KEY IDENTITY(1,1),
    id_lop_hp BIGINT NOT NULL,
    id_phong BIGINT NOT NULL,
    ngay_hoc DATETIME,
    start_time INT,
    end_time INT,
    so_tiet INT,
    thu_trong_tuan INT,
    tuan_bat_dau INT,
    tuan_ket_thuc INT,
    hinh_thuc NVARCHAR(20) CHECK (hinh_thuc IN ('Online','Offline')),
    CONSTRAINT FK_Lich_LHP   FOREIGN KEY (id_lop_hp) REFERENCES LOP_HOC_PHAN(id_lop_hp),
    CONSTRAINT FK_Lich_Phong FOREIGN KEY (id_phong)  REFERENCES DANH_MUC_PHONG(id_phong)
);
CREATE INDEX idx_classtime      ON LICH_CHI_TIET(id_lop_hp, start_time);
CREATE INDEX idx_room_schedule  ON LICH_CHI_TIET(id_phong, thu_trong_tuan, start_time);

-- DANG_KY_HOC_PHAN
CREATE TABLE DANG_KY_HOC_PHAN
(
    id_dang_ky BIGINT PRIMARY KEY IDENTITY(1,1),
    id_sv BIGINT NOT NULL,
    id_lop_hp BIGINT NOT NULL,
    ngay_dang_ky DATETIME DEFAULT GETDATE(),
    status NVARCHAR(20) CHECK (status IN ('Scheduled','Completed')),
    CONSTRAINT FK_DK_User FOREIGN KEY (id_sv)     REFERENCES [USER](id_acc),
    CONSTRAINT FK_DK_LHP  FOREIGN KEY (id_lop_hp) REFERENCES LOP_HOC_PHAN(id_lop_hp)
);
CREATE UNIQUE INDEX idx_unique_dki ON DANG_KY_HOC_PHAN(id_sv, id_lop_hp);
CREATE INDEX idx_sv_dki           ON DANG_KY_HOC_PHAN(id_sv, ngay_dang_ky);

-- BUOI_PHAT_SINH
CREATE TABLE BUOI_PHAT_SINH
(
    id_buoi_hoc BIGINT PRIMARY KEY IDENTITY(1,1),
    id_lich BIGINT,
    id_phong BIGINT NOT NULL,
    lop_hoc_phan_id BIGINT NOT NULL,
    ngay_hoc DATETIME NOT NULL,
    tiet_bat_dau INT,
    tiet_ket_thuc INT,
    trang_thai NVARCHAR(20) CHECK (trang_thai IN ('ChuaBatDau','DangDien','DaKetThuc','Huy')),
    CONSTRAINT FK_PS_LichChitiet FOREIGN KEY (id_lich)          REFERENCES LICH_CHI_TIET(id_lich),
    CONSTRAINT FK_PS_Phong       FOREIGN KEY (id_phong)         REFERENCES DANH_MUC_PHONG(id_phong),
    CONSTRAINT FK_PS_LHP         FOREIGN KEY (lop_hoc_phan_id)  REFERENCES LOP_HOC_PHAN(id_lop_hp)
);

-- PERSONAL_EVENT
CREATE TABLE PERSONAL_EVENT
(
    id_event BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT NOT NULL,
    external_sync_id VARCHAR(255),
    sync_version VARCHAR(50),
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    location NVARCHAR(255),
    start_time DATETIME NOT NULL,
    end_time DATETIME NOT NULL,
    recurrence_rule NVARCHAR(255),
    event_type NVARCHAR(50) CHECK (event_type IN ('ACADEMIC','PERSONAL','DEADLINE')),
    CONSTRAINT FK_Event_User FOREIGN KEY (id_acc) REFERENCES [USER](id_acc)
);
CREATE INDEX idx_user_events ON PERSONAL_EVENT(id_acc, start_time);
CREATE INDEX idx_event_type  ON PERSONAL_EVENT(id_acc, event_type, start_time);

-- NOTIFICATION_QUEUE
CREATE TABLE NOTIFICATION_QUEUE
(
    id_queue BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT NOT NULL,
    title NVARCHAR(255),
    content NVARCHAR(MAX),
    scheduled_at DATETIME,
    id_buoi_hoc BIGINT NULL,
    id_event BIGINT NULL,
    sent_at DATETIME,
    status NVARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING','SENT','FAILED')),
    CONSTRAINT FK_Notif_User    FOREIGN KEY (id_acc)      REFERENCES [USER](id_acc),
    CONSTRAINT FK_Notif_BuoiHoc FOREIGN KEY (id_buoi_hoc) REFERENCES BUOI_PHAT_SINH(id_buoi_hoc),
    CONSTRAINT FK_Notif_Event   FOREIGN KEY (id_event)    REFERENCES PERSONAL_EVENT(id_event)
);

-- REMINDER_CONFIG
CREATE TABLE REMINDER_CONFIG
(
    id_config BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT NOT NULL,
    mins_before INT DEFAULT 15,
    is_enabled BIT DEFAULT 1,
    quiet_hours DATETIME,
    channel NVARCHAR(20) CHECK (channel IN ('PUSH','EMAIL')),
    is_active BIT DEFAULT 1,
    CONSTRAINT FK_Remind_User FOREIGN KEY (id_acc) REFERENCES [USER](id_acc)
);

-- PREFERENCE
CREATE TABLE PREFERENCE
(
    id_preference BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT NOT NULL,
    giang_vien_weight FLOAT DEFAULT 0.5,
    thoi_gian_weight FLOAT DEFAULT 0.5,
    ua_tien_buoi NVARCHAR(20),
    ngay_nghi_mong_muon DATETIME,
    max_tin_chi INT DEFAULT 25,
    manual_gpa FLOAT NULL,
    manual_credits INT NULL,
    CONSTRAINT FK_Pref_User FOREIGN KEY (id_acc) REFERENCES [USER](id_acc)
);

-- DANH_GIA
CREATE TABLE DANH_GIA
(
    id_danh_gia BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT NOT NULL,
    id_giang_vien BIGINT NOT NULL,
    so_sao INT,
    noi_dung NVARCHAR(MAX),
    is_anonymous BIT DEFAULT 0,
    status NVARCHAR(20) CHECK (status IN ('Approved','Pending')),
    created_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_DG_User      FOREIGN KEY (id_acc)        REFERENCES [USER](id_acc),
    CONSTRAINT FK_DG_GiangVien FOREIGN KEY (id_giang_vien) REFERENCES GIANG_VIEN(id_giang_vien)
);

-- BAI_VIET
CREATE TABLE BAI_VIET
(
    id_bai_viet BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT NOT NULL,
    tieu_de NVARCHAR(255) NOT NULL,
    noi_dung NVARCHAR(MAX),
    ngay_dang DATETIME DEFAULT GETDATE(),
    so_luot_thich INT,
    status        NVARCHAR(50),
    background_color NVARCHAR(20) DEFAULT 'Transparent',
    status NVARCHAR(50),
    CONSTRAINT FK_BV_User FOREIGN KEY (id_acc) REFERENCES [USER](id_acc)

);

ALTER TABLE BAI_VIET ALTER COLUMN IdPostGoc BIGINT;
GO

ALTER TABLE BAI_VIET 
ADD is_anonymous BIT DEFAULT 0;
GO

-- Cập nhật các bài viết cũ mặc định là không ẩn danh
UPDATE BAI_VIET SET is_anonymous = 0 WHERE is_anonymous IS NULL;
GO

-- Thêm cột để nhận biết đây là bài chia sẻ (nếu chưa có)
-- IdPostGoc: ID của bài gốc, nếu = 0 hoặc NULL thì là bài viết tự đăng
-- NoiDungShare: Lời nhắn của người chia sẻ (UserComment trong code của bạn)
ALTER TABLE BAI_VIET 
ADD IdPostGoc INT NULL,
    NoiDungShare NVARCHAR(MAX) NULL,
    IsPublic BIT DEFAULT 1;
GO

CREATE PROCEDURE sp_SharePost
    @IdPostGoc BIGINT,
    -- Đổi sang BIGINT cho khớp với PRIMARY KEY của BAI_VIET
    @IdAcc BIGINT,
    -- Đổi sang BIGINT
    @NoiDungShare NVARCHAR(MAX),
    @IsPublic BIT
AS
BEGIN
    SET NOCOUNT ON;

    -- Chèn vào bảng BAI_VIET (không phải bảng Post)
    INSERT INTO BAI_VIET
        (
        id_acc,
        tieu_de,
        noi_dung,
        ngay_dang,
        so_luot_thich,
        status,
        IdPostGoc,
        NoiDungShare,
        IsPublic
        )
    VALUES
        (
            @IdAcc,
            N'Chia sẻ bài viết', -- Tiêu đề mặc định
            N'Đã chia sẻ một bài viết', -- Nội dung mặc định
            GETDATE(),
            0, -- Lượt thích ban đầu là 0
            N'Active', -- Trạng thái mặc định
            @IdPostGoc,
            @NoiDungShare,
            @IsPublic
    );

    -- Trả về kết quả để C# nhận diện success
    IF @@ROWCOUNT > 0 
        SELECT CAST(1 AS BIT) AS Success;
    ELSE 
        SELECT CAST(0 AS BIT) AS Success;
END
GO

-- Thêm cột lưu màu nền vào bảng bài viết
ALTER TABLE BAI_VIET 
ADD background_color NVARCHAR(20) DEFAULT 'Transparent';
GO

-- Cập nhật các bài cũ về mặc định là không màu (Transparent)
UPDATE BAI_VIET SET background_color = 'Transparent' WHERE background_color IS NULL;
GO

-- Likes
CREATE TABLE YEU_THICH
(
    id_yeu_thich BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT NOT NULL,
    id_bai_viet BIGINT NOT NULL,
    ngay_thich DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Like_User FOREIGN KEY (id_acc) REFERENCES [USER](id_acc),
    CONSTRAINT FK_Like_BaiViet FOREIGN KEY (id_bai_viet) REFERENCES BAI_VIET(id_bai_viet),
    -- Ràng buộc UNIQUE để 1 người chỉ like 1 bài 1 lần
    CONSTRAINT UC_User_Post UNIQUE (id_acc, id_bai_viet)
);

USE PBL3;

-- Gán DEFAULT 0 cho so_luot_thich
ALTER TABLE BAI_VIET 
ADD CONSTRAINT DF_so_luot_thich DEFAULT 0 FOR so_luot_thich;

-- Gán DEFAULT 'Active' cho status
ALTER TABLE BAI_VIET 
ADD CONSTRAINT DF_status DEFAULT 'Active' FOR status;

-- Cập nhật các bài cũ nếu đang NULL
UPDATE BAI_VIET SET so_luot_thich = 0 WHERE so_luot_thich IS NULL;
UPDATE BAI_VIET SET status = 'Active' WHERE status IS NULL;

USE PBL3;
GO

UPDATE BAI_VIET
SET so_luot_thich = (
    SELECT COUNT(*) 
    FROM YEU_THICH 
    WHERE YEU_THICH.id_bai_viet = BAI_VIET.id_bai_viet
);
GO

-- Tạo mới trigger
CREATE TRIGGER TRG_UpdateLikeCount
ON YEU_THICH
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Cập nhật cho những bài viết vừa được INSERT like
    UPDATE BAI_VIET
    SET so_luot_thich = (SELECT COUNT(*) FROM YEU_THICH WHERE id_bai_viet = b.id_bai_viet)
    FROM BAI_VIET b
    INNER JOIN inserted i ON b.id_bai_viet = i.id_bai_viet;

    -- Cập nhật cho những bài viết vừa bị DELETE like (Unlike)
    UPDATE BAI_VIET
    SET so_luot_thich = (SELECT COUNT(*) FROM YEU_THICH WHERE id_bai_viet = b.id_bai_viet)
    FROM BAI_VIET b
    INNER JOIN deleted d ON b.id_bai_viet = d.id_bai_viet;
END;
GO

ALTER TABLE BAI_VIET ADD so_luot_binh_luan INT DEFAULT 0;
ALTER TABLE BAI_VIET ADD so_luot_chia_se INT DEFAULT 0;
GO

CREATE TRIGGER TRG_UpdateCommentCount
ON BINH_LUAN
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    -- Khi thêm bình luận
    UPDATE BAI_VIET SET so_luot_binh_luan = (SELECT COUNT(*) FROM BINH_LUAN WHERE id_bai_viet = b.id_bai_viet)
    FROM BAI_VIET b INNER JOIN inserted i ON b.id_bai_viet = i.id_bai_viet;
    -- Khi xóa bình luận
    UPDATE BAI_VIET SET so_luot_binh_luan = (SELECT COUNT(*) FROM BINH_LUAN WHERE id_bai_viet = b.id_bai_viet)
    FROM BAI_VIET b INNER JOIN deleted d ON b.id_bai_viet = d.id_bai_viet;
END;
GO

CREATE TRIGGER TRG_UpdateShareCount
ON BAI_VIET
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    -- Cập nhật cho bài viết GỐC khi có bài viết con tham chiếu tới nó
    UPDATE BAI_VIET SET so_luot_chia_se = (SELECT COUNT(*) FROM BAI_VIET WHERE IdPostGoc = b.id_bai_viet)
    FROM BAI_VIET b INNER JOIN inserted i ON b.id_bai_viet = i.IdPostGoc;
    
    UPDATE BAI_VIET SET so_luot_chia_se = (SELECT COUNT(*) FROM BAI_VIET WHERE IdPostGoc = b.id_bai_viet)
    FROM BAI_VIET b INNER JOIN deleted d ON b.id_bai_viet = d.IdPostGoc;
END;
GO

-- BINH_LUAN
CREATE TABLE BINH_LUAN
(
    id_binh_luan BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT NOT NULL,
    id_bai_viet BIGINT NOT NULL,
    noi_dung NVARCHAR(MAX) NOT NULL,
    CONSTRAINT FK_BL_User    FOREIGN KEY (id_acc)       REFERENCES [USER](id_acc),
    CONSTRAINT FK_BL_BaiViet FOREIGN KEY (id_bai_viet) REFERENCES BAI_VIET(id_bai_viet)
);

-- 1. Thêm cột ngay_binh_luan vào bảng đã tồn tại
ALTER TABLE BINH_LUAN 
ADD ngay_binh_luan DATETIME DEFAULT GETDATE();

-- 2. (Tùy chọn) Cập nhật các dòng cũ đã lỡ tạo (nếu có) thành thời gian hiện tại
UPDATE BINH_LUAN SET ngay_binh_luan = GETDATE() WHERE ngay_binh_luan IS NULL;

-- DOCUMENTS
CREATE TABLE DOCUMENTS
(
    id_file BIGINT PRIMARY KEY IDENTITY(1,1),
    id_bai_viet BIGINT NOT NULL,
    ten_file NVARCHAR(255),
    duong_dan NVARCHAR(500),
    CONSTRAINT FK_Doc_BaiViet FOREIGN KEY (id_bai_viet) REFERENCES BAI_VIET(id_bai_viet)
);

-- ANNOUNCEMENTS
CREATE TABLE ANNOUNCEMENTS
(
    id_announcement BIGINT PRIMARY KEY IDENTITY(1,1),
    id_acc BIGINT,
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    ngay_dang DATETIME DEFAULT GETDATE(),
    pham_vi NVARCHAR(50),
    CONSTRAINT FK_Ann_User FOREIGN KEY (id_acc) REFERENCES [USER](id_acc)
);

-- TICH_LUY_TIN_CHI (bổ sung)
CREATE TABLE TICH_LUY_TIN_CHI
(
    id_tich_luy BIGINT PRIMARY KEY IDENTITY(1,1),
    id_sv BIGINT NOT NULL,
    id_mon_hoc BIGINT NOT NULL,
    diem_chu VARCHAR(2),
    diem_so FLOAT,
    hoc_ky INT,
    nam_hoc VARCHAR(20),
    is_passed BIT DEFAULT 0,
    CONSTRAINT FK_TLTC_User   FOREIGN KEY (id_sv)      REFERENCES [USER](id_acc),
    CONSTRAINT FK_TLTC_MonHoc FOREIGN KEY (id_mon_hoc) REFERENCES MON_HOC(id_mon_hoc)
);
CREATE INDEX idx_sv_tltc ON TICH_LUY_TIN_CHI(id_sv, is_passed);

-- DIEU_KIEN_TIEN_QUYET (bổ sung)
CREATE TABLE DIEU_KIEN_TIEN_QUYET
(
    id BIGINT PRIMARY KEY IDENTITY(1,1),
    id_mon_hoc BIGINT NOT NULL,
    id_mon_tq BIGINT NOT NULL,
    CONSTRAINT FK_DKTQ_Mon   FOREIGN KEY (id_mon_hoc) REFERENCES MON_HOC(id_mon_hoc),
    CONSTRAINT FK_DKTQ_MonTQ FOREIGN KEY (id_mon_tq)  REFERENCES MON_HOC(id_mon_hoc)
);
GO

-- =============================================
-- MIGRATION: Hệ thống phân quyền Admin Diễn đàn
-- Chạy script này trên database PBL3 đang có sẵn
-- =============================================

USE PBL3;
GO

-- BƯỚC 1: Thêm cột status vào BAI_VIET (nếu chưa có)
-- status: 0 = Chờ duyệt, 1 = Đã duyệt, 2 = Từ chối
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'BAI_VIET' AND COLUMN_NAME = 'approval_status'
)
BEGIN
    ALTER TABLE BAI_VIET ADD approval_status INT NOT NULL DEFAULT 1;
    -- Mặc định là 1 (Đã duyệt) để bài cũ vẫn hiển thị bình thường
    PRINT N'Đã thêm cột approval_status vào BAI_VIET';
END
ELSE
BEGIN
    PRINT N'Cột approval_status đã tồn tại, bỏ qua.';
END
GO

-- BƯỚC 2: Thêm cột rejected_reason (lý do từ chối) - tuỳ chọn nhưng rất hữu ích
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'BAI_VIET' AND COLUMN_NAME = 'rejected_reason'
)
BEGIN
    ALTER TABLE BAI_VIET ADD rejected_reason NVARCHAR(500) NULL;
    PRINT N'Đã thêm cột rejected_reason vào BAI_VIET';
END
GO

-- BƯỚC 3: Kiểm tra và đảm bảo data ROLES có 'Admin'
IF NOT EXISTS (SELECT 1 FROM ROLES WHERE role_name = N'Admin')
BEGIN
    INSERT INTO ROLES (role_name) VALUES (N'Admin');
    PRINT N'Đã thêm role Admin';
END
GO

IF NOT EXISTS (SELECT 1 FROM ROLES WHERE role_name = N'Student')
BEGIN
    INSERT INTO ROLES (role_name) VALUES (N'Student');
    PRINT N'Đã thêm role Student';
END
GO

-- BƯỚC 4: Stored Procedure để lấy bài chờ duyệt (status = 0)
CREATE OR ALTER PROCEDURE sp_GetPendingPosts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        bv.*,
        u.ho_ten,
        (SELECT COUNT(*) FROM YEU_THICH WHERE id_bai_viet = bv.id_bai_viet) AS TotalLikes,
        (SELECT COUNT(*) FROM BINH_LUAN WHERE id_bai_viet = bv.id_bai_viet) AS TotalComments,
        (SELECT COUNT(*) FROM BAI_VIET WHERE IdPostGoc = bv.id_bai_viet)    AS TotalShares,
        0 AS IsLikedByMe
    FROM BAI_VIET bv
    LEFT JOIN [USER] u ON bv.id_acc = u.id_acc
    WHERE bv.approval_status = 0
    ORDER BY bv.ngay_dang ASC; -- Bài cũ nhất duyệt trước
END
GO

-- BƯỚC 5: Stored Procedure cập nhật trạng thái duyệt
CREATE OR ALTER PROCEDURE sp_UpdatePostStatus
    @idPost       BIGINT,
    @newStatus    INT,        -- 0=Chờ, 1=Duyệt, 2=Từ chối
    @adminIdAcc   BIGINT,
    @reason       NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra quyền Admin
    IF NOT EXISTS (
        SELECT 1 FROM ACCOUNT a
        JOIN ROLES r ON a.id_role = r.id_role
        WHERE a.id_acc = @adminIdAcc AND r.role_name = N'Admin'
    )
    BEGIN
        RAISERROR(N'Tài khoản không có quyền Admin!', 16, 1);
        RETURN;
    END

    UPDATE BAI_VIET
    SET 
        approval_status  = @newStatus,
        rejected_reason  = CASE WHEN @newStatus = 2 THEN @reason ELSE NULL END
    WHERE id_bai_viet = @idPost;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- BƯỚC 6: Stored Procedure Admin xóa bài bất kỳ (có ghi log)
CREATE OR ALTER PROCEDURE sp_AdminDeletePost
    @idPost     BIGINT,
    @adminIdAcc BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra quyền Admin
    IF NOT EXISTS (
        SELECT 1 FROM ACCOUNT a
        JOIN ROLES r ON a.id_role = r.id_role
        WHERE a.id_acc = @adminIdAcc AND r.role_name = N'Admin'
    )
    BEGIN
        RAISERROR(N'Không có quyền Admin!', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        -- Xóa các bảng con trước (tránh lỗi FK)
        DELETE FROM DOCUMENTS  WHERE id_bai_viet = @idPost;
        DELETE FROM YEU_THICH  WHERE id_bai_viet = @idPost;
        DELETE FROM BINH_LUAN  WHERE id_bai_viet = @idPost;
        -- Nullify các bài share tham chiếu tới bài này
        UPDATE BAI_VIET SET IdPostGoc = NULL WHERE IdPostGoc = @idPost;
        -- Xóa bài chính
        DELETE FROM BAI_VIET WHERE id_bai_viet = @idPost;

        -- Ghi log hành động Admin (nếu bảng USER_LOG hỗ trợ)
        INSERT INTO USER_LOG (hanh_dong, id_acc)
        VALUES (CONCAT(N'Admin xóa bài viết ID: ', @idPost), @adminIdAcc);

        COMMIT TRANSACTION;
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

PRINT N'Migration hoàn tất! Hệ thống Admin đã sẵn sàng.';
GO


-- =============================================
-- DỮ LIỆU MẪU
-- =============================================

-- Roles
INSERT INTO ROLES
    (role_name)
VALUES
    (N'Admin'),
    (N'Student');

-- Môn học
INSERT INTO MON_HOC
    (ma_mon_hoc, ten_mon_hoc, so_tin_chi)
VALUES
    ('INT1001', N'Lập trình cơ bản', 3),
    ('INT1002', N'Toán rời rạc', 3),
    ('INT1003', N'Nhập môn cơ sở dữ liệu', 3),
    ('INT2001', N'Lập trình hướng đối tượng', 3),
    ('INT2002', N'Cơ sở dữ liệu nâng cao', 3),
    ('INT2003', N'Mạng máy tính', 3),
    ('INT3001', N'Lập trình ứng dụng', 3),
    ('INT3002', N'Trí tuệ nhân tạo', 3);

-- Điều kiện tiên quyết
INSERT INTO DIEU_KIEN_TIEN_QUYET
    (id_mon_hoc, id_mon_tq)
SELECT a.id_mon_hoc, b.id_mon_hoc
FROM MON_HOC a, MON_HOC b
WHERE  a.ma_mon_hoc='INT2001' AND b.ma_mon_hoc='INT1001';

INSERT INTO DIEU_KIEN_TIEN_QUYET
    (id_mon_hoc, id_mon_tq)
SELECT a.id_mon_hoc, b.id_mon_hoc
FROM MON_HOC a, MON_HOC b
WHERE  a.ma_mon_hoc='INT2002' AND b.ma_mon_hoc='INT1003';

-- Giảng viên
INSERT INTO GIANG_VIEN
    (ten_giang_vien, email, khoa, rating)
VALUES
    (N'TS. Nguyễn Văn Hùng', 'hung@dut.udn.vn', N'CNTT', 4.8),
    (N'Th.S Trần Thị Lan', 'lan@dut.udn.vn', N'CNTT', 4.6),
    (N'Th.S Lê Văn Minh', 'minh@dut.udn.vn', N'CNTT', 4.5),
    (N'PGS.TS Phạm Thị Hoa', 'hoa@dut.udn.vn', N'CNTT', 4.9);

-- Phòng học
INSERT INTO DANH_MUC_PHONG
    (ten_phong, toa_nha, loai_phong, suc_chua)
VALUES
    ('A101', N'Nhà A', 'LT', 60),
    ('A102', N'Nhà A', 'LT', 60),
    ('B201', N'Nhà B', 'TH', 40),
    ('B202', N'Nhà B', 'TH', 40),
    ('C301', N'Nhà C', 'LT', 80),
    ('C302', N'Nhà C', 'TH', 40);

-- Lớp học phần HK1 2024-2025
DECLARE @gv1 BIGINT = (SELECT TOP 1
    id_giang_vien
FROM GIANG_VIEN
WHERE ten_giang_vien LIKE N'%Hùng%');
DECLARE @gv2 BIGINT = (SELECT TOP 1
    id_giang_vien
FROM GIANG_VIEN
WHERE ten_giang_vien LIKE N'%Lan%');
DECLARE @gv3 BIGINT = (SELECT TOP 1
    id_giang_vien
FROM GIANG_VIEN
WHERE ten_giang_vien LIKE N'%Minh%');
DECLARE @gv4 BIGINT = (SELECT TOP 1
    id_giang_vien
FROM GIANG_VIEN
WHERE ten_giang_vien LIKE N'%Hoa%');
DECLARE @p1  BIGINT = (SELECT TOP 1
    id_phong
FROM DANH_MUC_PHONG
WHERE ten_phong='A101');
DECLARE @p2  BIGINT = (SELECT TOP 1
    id_phong
FROM DANH_MUC_PHONG
WHERE ten_phong='B201');
DECLARE @p3  BIGINT = (SELECT TOP 1
    id_phong
FROM DANH_MUC_PHONG
WHERE ten_phong='C301');

INSERT INTO LOP_HOC_PHAN
    (id_mon_hoc, id_giang_vien, id_phong, hoc_ky, nam_hoc)
SELECT id_mon_hoc,
    CASE WHEN ROW_NUMBER() OVER (ORDER BY id_mon_hoc) % 4 = 1 THEN @gv1
            WHEN ROW_NUMBER() OVER (ORDER BY id_mon_hoc) % 4 = 2 THEN @gv2
            WHEN ROW_NUMBER() OVER (ORDER BY id_mon_hoc) % 4 = 3 THEN @gv3
            ELSE @gv4 END,
    CASE WHEN ROW_NUMBER() OVER (ORDER BY id_mon_hoc) % 3 = 1 THEN @p1
            WHEN ROW_NUMBER() OVER (ORDER BY id_mon_hoc) % 3 = 2 THEN @p2
            ELSE @p3 END,
    1, '2024-2025'
FROM MON_HOC;
GO

-- Lịch chi tiết
INSERT INTO LICH_CHI_TIET
    (id_lop_hp, id_phong, thu_trong_tuan, start_time, end_time, so_tiet, tuan_bat_dau, tuan_ket_thuc, hinh_thuc)
SELECT
    lhp.id_lop_hp,
    lhp.id_phong,
    (ROW_NUMBER() OVER (ORDER BY lhp.id_lop_hp) % 5) + 2, -- Thứ 2 -> 6
    (ROW_NUMBER() OVER (ORDER BY lhp.id_lop_hp) % 3) * 3 + 1, -- Tiết 1, 4, 7
    (ROW_NUMBER() OVER (ORDER BY lhp.id_lop_hp) % 3) * 3 + 3, -- Tiết 3, 6, 9
    3,
    1,
    15,
    'Offline'
FROM LOP_HOC_PHAN lhp
WHERE lhp.hoc_ky = 1 AND lhp.nam_hoc = '2024-2025';
GO

PRINT N'Setup hoàn tất! Bây giờ hãy đăng ký tài khoản qua giao diện ứng dụng.';
GO

-- ================================================================
--  StudentReminderApp — FULL DATABASE SETUP SCRIPT
--  Chạy file này MỘT LẦN DUY NHẤT để khởi tạo toàn bộ database.
--
--  Bao gồm:
--    1. Tạo / cập nhật Schema (bảng, cột, FK)
--    2. Chèn 20 lớp học
--    3. Chèn 200 tài khoản sinh viên mẫu + phân lớp
--    4. Fix password_hash sang BCrypt (mật khẩu: 123456)
--    5. Stored Procedures
--    6. Kiểm tra kết quả
--
--  Yêu cầu:
--    • SQL Server 2016+ (hoặc Azure SQL)
--    • Database PBL3 đã tồn tại (tạo thủ công hoặc bỏ comment dòng CREATE DATABASE bên dưới)
--    • Các bảng ACCOUNT, [USER], ROLES đã có sẵn (tạo bởi migration khác)
-- ================================================================

USE PBL3;
GO

PRINT '============================================================';
PRINT ' StudentReminderApp — FULL SETUP';
PRINT '============================================================';
GO


-- ================================================================
-- PHẦN 1: CẬP NHẬT SCHEMA
-- ================================================================
PRINT '';
PRINT '--- PHẦN 1: Schema ---';
GO

-- 1.1 Bảng LOP_SINH_VIEN
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LOP_SINH_VIEN')
BEGIN
    CREATE TABLE LOP_SINH_VIEN (
        id_lop    INT           IDENTITY(1,1) PRIMARY KEY,
        ten_lop   NVARCHAR(50)  NOT NULL,
        nien_khoa NVARCHAR(20)  NOT NULL
    );
    PRINT '✔ Tạo bảng LOP_SINH_VIEN.';
END
ELSE
    PRINT 'ℹ LOP_SINH_VIEN đã tồn tại, bỏ qua.';
GO

-- 1.2 Cột id_lop trong [USER]
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'USER' AND COLUMN_NAME = 'id_lop')
BEGIN
    ALTER TABLE [USER] ADD id_lop INT NULL;
    PRINT '✔ Thêm cột id_lop vào [USER].';
END
ELSE
    PRINT 'ℹ Cột id_lop đã tồn tại, bỏ qua.';
GO

-- 1.3 Foreign Key USER.id_lop → LOP_SINH_VIEN.id_lop
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
               WHERE CONSTRAINT_NAME = 'FK_USER_LOP')
BEGIN
    ALTER TABLE [USER]
        ADD CONSTRAINT FK_USER_LOP
        FOREIGN KEY (id_lop) REFERENCES LOP_SINH_VIEN(id_lop)
        ON DELETE SET NULL ON UPDATE CASCADE;
    PRINT '✔ Thêm FK_USER_LOP.';
END
ELSE
    PRINT 'ℹ FK_USER_LOP đã tồn tại, bỏ qua.';
GO

-- 1.4 Cột is_verified trong ACCOUNT
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ACCOUNT' AND COLUMN_NAME = 'is_verified')
BEGIN
    ALTER TABLE ACCOUNT ADD is_verified BIT NOT NULL DEFAULT 0;
    PRINT '✔ Thêm cột is_verified vào ACCOUNT.';
END
ELSE
    PRINT 'ℹ is_verified đã tồn tại, bỏ qua.';
GO

-- 1.5 Cột lock_until trong ACCOUNT
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ACCOUNT' AND COLUMN_NAME = 'lock_until')
BEGIN
    ALTER TABLE ACCOUNT ADD lock_until DATETIME NULL;
    PRINT '✔ Thêm cột lock_until vào ACCOUNT.';
END
ELSE
    PRINT 'ℹ lock_until đã tồn tại, bỏ qua.';
GO


-- ================================================================
-- PHẦN 2: DỌN DẸP ROLES TRÙNG (giữ lại chỉ Admin=1, Student=2)
-- ================================================================
PRINT '';
PRINT '--- PHẦN 2: Chuẩn hoá ROLES ---';
GO

-- Đảm bảo role Admin (id=1) và Student (id=2) tồn tại
IF NOT EXISTS (SELECT 1 FROM ROLES WHERE id_role = 1)
    INSERT INTO ROLES (id_role, role_name) VALUES (1, N'Admin');

IF NOT EXISTS (SELECT 1 FROM ROLES WHERE id_role = 2)
    INSERT INTO ROLES (id_role, role_name) VALUES (2, N'Student');

-- Gán lại id_role=2 cho mọi tài khoản MSSV bị NULL hoặc role thừa
UPDATE ACCOUNT
SET    id_role = 2
WHERE  username LIKE '102%'
  AND  id_role NOT IN (SELECT id_role FROM ROLES WHERE role_name = N'Student');

-- Xoá role thừa (chỉ xoá nếu không còn tài khoản nào tham chiếu)
DELETE FROM ROLES
WHERE id_role NOT IN (SELECT DISTINCT id_role FROM ACCOUNT)
  AND id_role NOT IN (1, 2);

PRINT '✔ ROLES đã chuẩn hoá.';
GO


-- ================================================================
-- PHẦN 3: CHÈN 20 LỚP HỌC
-- ================================================================
PRINT '';
PRINT '--- PHẦN 3: 20 Lớp học ---';
GO

-- Chỉ chèn nếu bảng đang rỗng (idempotent)
IF NOT EXISTS (SELECT 1 FROM LOP_SINH_VIEN)
BEGIN
    -- Tắt IDENTITY_INSERT để dùng id cố định 1-20
    SET IDENTITY_INSERT LOP_SINH_VIEN ON;

    INSERT INTO LOP_SINH_VIEN (id_lop, ten_lop, nien_khoa) VALUES
    -- Năm 2021 (id 1-5)
    (1,  N'21_DT1',   N'2021-2025'),
    (2,  N'21_DT2',   N'2021-2025'),
    (3,  N'21_KHDL',  N'2021-2025'),
    (4,  N'21_Nhat1', N'2021-2025'),
    (5,  N'21_Nhat2', N'2021-2025'),
    -- Năm 2022 (id 6-10)
    (6,  N'22_DT1',   N'2022-2026'),
    (7,  N'22_DT2',   N'2022-2026'),
    (8,  N'22_KHDL',  N'2022-2026'),
    (9,  N'22_Nhat1', N'2022-2026'),
    (10, N'22_Nhat2', N'2022-2026'),
    -- Năm 2023 (id 11-15)
    (11, N'23_DT1',   N'2023-2027'),
    (12, N'23_DT2',   N'2023-2027'),
    (13, N'23_KHDL',  N'2023-2027'),
    (14, N'23_Nhat1', N'2023-2027'),
    (15, N'23_Nhat2', N'2023-2027'),
    -- Năm 2024 (id 16-20)
    (16, N'24_DT1',   N'2024-2028'),
    (17, N'24_DT2',   N'2024-2028'),
    (18, N'24_KHDL',  N'2024-2028'),
    (19, N'24_Nhat1', N'2024-2028'),
    (20, N'24_Nhat2', N'2024-2028');

    SET IDENTITY_INSERT LOP_SINH_VIEN OFF;

    -- Đồng bộ IDENTITY counter sau khi insert thủ công
    DBCC CHECKIDENT ('LOP_SINH_VIEN', RESEED, 20);

    PRINT '✔ Đã chèn 20 lớp học.';
END
ELSE
    PRINT 'ℹ LOP_SINH_VIEN đã có dữ liệu, bỏ qua phần chèn lớp.';
GO


-- ================================================================
-- PHẦN 4: CHÈN 200 TÀI KHOẢN SINH VIÊN MẪU
--
-- Phân bổ: 4 năm × 5 lớp × 10 sinh viên = 200
-- MSSV:    102 + YY + NNNN  (VD: 102240001)
-- STT/lớp: 01-10 → _DT1 | 11-20 → _DT2 | 21-30 → _KHDL
--          31-40 → _Nhat1  | 41-50 → _Nhat2
--
-- Password mặc định: 123456
-- (Sẽ được hash BCrypt ở Phần 5)
-- ================================================================
PRINT '';
PRINT '--- PHẦN 4: 200 tài khoản sinh viên ---';
GO

DECLARE @studentRoleId BIGINT = 2;

-- Bảng họ (15 họ phổ biến)
DECLARE @hoList TABLE (idx INT, ho NVARCHAR(20));
INSERT INTO @hoList VALUES
(1,N'Nguyễn'),(2,N'Trần'),(3,N'Lê'),(4,N'Phạm'),(5,N'Hoàng'),
(6,N'Huỳnh'),(7,N'Phan'),(8,N'Vũ'),(9,N'Đặng'),(10,N'Bùi'),
(11,N'Đỗ'),(12,N'Hồ'),(13,N'Ngô'),(14,N'Dương'),(15,N'Lý');

-- Bảng tên (50 tên)
DECLARE @tenList TABLE (idx INT, ten NVARCHAR(40));
INSERT INTO @tenList VALUES
(1,N'Minh Tuấn'),(2,N'Thị Lan'),(3,N'Văn Hùng'),(4,N'Thị Hoa'),(5,N'Quốc Khánh'),
(6,N'Thị Thu'),(7,N'Minh Khoa'),(8,N'Thị Ngọc'),(9,N'Hoàng Nam'),(10,N'Thị Mai'),
(11,N'Văn Đức'),(12,N'Thị Linh'),(13,N'Minh Hiếu'),(14,N'Thị Yến'),(15,N'Quang Huy'),
(16,N'Thị Phương'),(17,N'Văn Tùng'),(18,N'Thị Trang'),(19,N'Minh Long'),(20,N'Thị Hương'),
(21,N'Thanh Bình'),(22,N'Thị Ngân'),(23,N'Văn Cường'),(24,N'Thị Vân'),(25,N'Minh Đức'),
(26,N'Thị Thảo'),(27,N'Quốc Huy'),(28,N'Thị Loan'),(29,N'Văn Thắng'),(30,N'Thị Nhung'),
(31,N'Minh Trí'),(32,N'Thị Diễm'),(33,N'Hoàng Anh'),(34,N'Thị Bích'),(35,N'Văn Phúc'),
(36,N'Thị Cẩm'),(37,N'Minh Quân'),(38,N'Thị Duyên'),(39,N'Quang Minh'),(40,N'Thị Hạnh'),
(41,N'Văn Bảo'),(42,N'Thị Ánh'),(43,N'Minh Nhật'),(44,N'Thị Kiều'),(45,N'Hoàng Phúc'),
(46,N'Thị Lý'),(47,N'Văn Dũng'),(48,N'Thị Nhi'),(49,N'Minh Khải'),(50,N'Thị Châu');

DECLARE @year         INT   = 21;
DECLARE @stt          INT;
DECLARE @username     NVARCHAR(20);
DECLARE @hoTen        NVARCHAR(100);
DECLARE @email        NVARCHAR(100);
DECLARE @idLop        INT;
DECLARE @lopBase      INT;   -- id_lop đầu tiên của năm (1, 6, 11, 16)
DECLARE @newAccId     BIGINT;
DECLARE @totalIns     INT   = 0;
DECLARE @ho           NVARCHAR(20);
DECLARE @ten          NVARCHAR(40);

WHILE @year <= 24
BEGIN
    -- id_lop đầu tiên của năm: 21→1, 22→6, 23→11, 24→16
    SET @lopBase = (@year - 21) * 5 + 1;

    SET @stt = 1;
    WHILE @stt <= 50
    BEGIN
        SET @username = '102'
                      + RIGHT('0'   + CAST(@year AS VARCHAR(2)), 2)
                      + RIGHT('000' + CAST(@stt  AS VARCHAR(4)), 4);

        IF NOT EXISTS (SELECT 1 FROM ACCOUNT WHERE username = @username)
        BEGIN
            -- Chọn họ (xoay vòng 15)
            DECLARE @hoIdx INT = ((@year * 7 + @stt * 3) % 15) + 1;
            SELECT @ho  = ho  FROM @hoList WHERE idx = @hoIdx;
            SELECT @ten = ten FROM @tenList WHERE idx = @stt;
            SET @hoTen = @ho + N' ' + @ten;
            SET @email = @username + N'@student.edu.vn';

            -- id_lop: nhóm 10 SV/lớp → lopIndex = 0-4
            SET @idLop = @lopBase + (@stt - 1) / 10;

            INSERT INTO ACCOUNT (username, password_hash, id_role, status, is_verified, created_at)
            VALUES (@username, N'PLACEHOLDER', @studentRoleId, N'Active', 0, GETDATE());

            SET @newAccId = SCOPE_IDENTITY();

            INSERT INTO [USER] (id_acc, ho_ten, email, sdt, id_lop)
            VALUES (@newAccId, @hoTen, @email, N'', @idLop);

            SET @totalIns = @totalIns + 1;
        END

        SET @stt = @stt + 1;
    END

    SET @year = @year + 1;
END

PRINT '✔ Đã chèn ' + CAST(@totalIns AS VARCHAR) + ' tài khoản sinh viên mới.';
GO


-- ================================================================
-- PHẦN 5: FIX PASSWORD_HASH → BCRYPT
--
-- Thay thế mọi password dạng plain text bằng BCrypt hash
-- của chuỗi "123456" (cost = 11).
--
-- Sau bước này, đăng nhập bằng mật khẩu: 123456
-- ================================================================
PRINT '';
PRINT '--- PHẦN 5: Fix password_hash → BCrypt ---';
GO

UPDATE ACCOUNT
SET    password_hash = N'$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy'
WHERE  password_hash IN (N'123456', N'PLACEHOLDER');

PRINT '✔ Đã cập nhật password_hash BCrypt cho '
      + CAST(@@ROWCOUNT AS VARCHAR) + ' tài khoản.';
GO


-- ================================================================
-- PHẦN 6: STORED PROCEDURES
-- ================================================================
PRINT '';
PRINT '--- PHẦN 6: Stored Procedures ---';
GO

-- sp_GetAllStudents
CREATE OR ALTER PROCEDURE sp_GetAllStudents
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.id_acc,
        a.username                            AS mssv,
        a.status,
        ISNULL(a.is_verified, 0)              AS is_verified,
        a.created_at,
        a.lock_until,
        ISNULL(u.ho_ten, N'Chưa có tên')      AS ho_ten,
        ISNULL(u.email, '')                   AS email,
        ISNULL(u.sdt,   '')                   AS sdt,
        u.id_lop,
        ISNULL(l.ten_lop,   N'Chưa xếp lớp') AS ten_lop,
        ISNULL(l.nien_khoa, '')               AS nien_khoa
    FROM ACCOUNT a
    LEFT JOIN [USER]        u ON u.id_acc = a.id_acc
    LEFT JOIN LOP_SINH_VIEN l ON l.id_lop = u.id_lop
    INNER JOIN ROLES         r ON r.id_role = a.id_role
    WHERE r.role_name = N'Student'
    ORDER BY a.created_at DESC, a.username ASC;
END
GO
PRINT '✔ sp_GetAllStudents';

-- sp_BanStudent (khóa kèm thời hạn)
CREATE OR ALTER PROCEDURE sp_BanStudent
    @idAcc     BIGINT,
    @lockUntil DATETIME = NULL   -- NULL = vĩnh viễn
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ACCOUNT
    SET status     = N'Banned',
        lock_until = @lockUntil
    WHERE id_acc = @idAcc;
    SELECT @@ROWCOUNT AS affected;
END
GO
PRINT '✔ sp_BanStudent';

-- sp_UnbanStudent
CREATE OR ALTER PROCEDURE sp_UnbanStudent
    @idAcc BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ACCOUNT
    SET status     = N'Active',
        lock_until = NULL
    WHERE id_acc = @idAcc;
    SELECT @@ROWCOUNT AS affected;
END
GO
PRINT '✔ sp_UnbanStudent';

-- sp_VerifyStudent
CREATE OR ALTER PROCEDURE sp_VerifyStudent
    @idAcc BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE ACCOUNT SET is_verified = 1 WHERE id_acc = @idAcc;
    SELECT @@ROWCOUNT AS affected;
END
GO
PRINT '✔ sp_VerifyStudent';

-- sp_UpdateStudentClass
CREATE OR ALTER PROCEDURE sp_UpdateStudentClass
    @idAcc BIGINT,
    @idLop INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [USER] SET id_lop = @idLop WHERE id_acc = @idAcc;
    SELECT @@ROWCOUNT AS affected;
END
GO
PRINT '✔ sp_UpdateStudentClass';
GO


-- ================================================================
-- PHẦN 7: KIỂM TRA KẾT QUẢ
-- ================================================================
PRINT '';
PRINT '--- PHẦN 7: Kiểm tra kết quả ---';
GO

-- 7.1 Số sinh viên mỗi lớp (phải là 10/lớp)
SELECT
    l.id_lop,
    l.ten_lop,
    l.nien_khoa,
    COUNT(u.id_acc) AS so_sinh_vien
FROM LOP_SINH_VIEN l
LEFT JOIN [USER] u ON u.id_lop = l.id_lop
GROUP BY l.id_lop, l.ten_lop, l.nien_khoa
ORDER BY l.id_lop;

-- 7.2 Tổng tài khoản Student
SELECT COUNT(*) AS tong_student
FROM ACCOUNT a
JOIN ROLES r ON a.id_role = r.id_role
WHERE r.role_name = N'Student';

-- 7.3 Kiểm tra password_hash đã fix chưa
SELECT
    SUM(CASE WHEN password_hash LIKE '$2a$%' THEN 1 ELSE 0 END) AS da_bcrypt,
    SUM(CASE WHEN password_hash NOT LIKE '$2a$%' THEN 1 ELSE 0 END) AS chua_bcrypt
FROM ACCOUNT
WHERE username LIKE '102%';

-- 7.4 Thử chạy stored procedure chính
EXEC sp_GetAllStudents;
GO

PRINT '';
PRINT '============================================================';
PRINT ' SETUP HOÀN TẤT!';
PRINT ' Đăng nhập sinh viên: MSSV = 102210001 … 102240050';
PRINT '                      Mật khẩu = 123456';
PRINT ' Đăng nhập admin:     Username = admin_test';
PRINT '                      Mật khẩu = (giữ nguyên)';
PRINT '============================================================';
GO

-- ================================================================
-- MIGRATION: Thêm cột OTP vào bảng ACCOUNT
-- Chạy một lần trước khi deploy tính năng Quên mật khẩu
-- ================================================================
USE PBL3;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ACCOUNT' AND COLUMN_NAME = 'otp_code')
BEGIN
    ALTER TABLE ACCOUNT ADD otp_code NVARCHAR(6) NULL;
    PRINT N'✔ Thêm cột otp_code vào ACCOUNT.';
END
ELSE
    PRINT N'ℹ otp_code đã tồn tại, bỏ qua.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ACCOUNT' AND COLUMN_NAME = 'otp_expired_at')
BEGIN
    ALTER TABLE ACCOUNT ADD otp_expired_at DATETIME NULL;
    PRINT N'✔ Thêm cột otp_expired_at vào ACCOUNT.';
END
ELSE
    PRINT N'ℹ otp_expired_at đã tồn tại, bỏ qua.';
GO
