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
    status NVARCHAR(50),
    CONSTRAINT FK_BV_User FOREIGN KEY (id_acc) REFERENCES [USER](id_acc)

);

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