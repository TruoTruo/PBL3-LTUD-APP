# Student Reminder & Advisor App

Student Reminder & Advisor là một ứng dụng Desktop (WPF) dành cho sinh viên, giúp quản lý thời khóa biểu, sự kiện cá nhân, cài đặt nhắc nhở tự động, hỗ trợ tư vấn đăng ký học phần và tạo không gian giao lưu (Forum) trực tuyến.

## 🌟 Tính Năng Nổi Bật

### 1. Quản Lý Lịch Trình (Calendar & Schedule)
- Xem lịch linh hoạt theo nhiều chế độ: Ngày, Tuần, Tháng, Năm.
- **Tạo sự kiện thông minh:** Hỗ trợ thao tác kéo thả (Drag & Drop) trực tiếp trên lưới lịch để chọn khung giờ.
- **Nhập thời khóa biểu:** Tự động đọc và lên lịch lặp lại cho các môn học từ file JSON.
- **Sự kiện lặp lại:** Cấu hình lặp sự kiện theo ngày, tuần, tháng.
- **Quản lý Tags:** Phân loại sự kiện (Cá nhân, Học tập, Nhắc nhở) bằng nhãn dán và màu sắc trực quan.
- **Xung đột lịch:** Tự động cảnh báo nếu khách mời bị trùng lịch vào khung giờ sắp tạo.

### 2. Hệ Thống Thông Báo (Notification System)
- **Chạy ngầm (Background):** Thu nhỏ xuống khay hệ thống (System Tray) để liên tục kiểm tra sự kiện.
- **Đa kênh thông báo:** Nhắc nhở qua Popup (Toast) góc màn hình hoặc gửi Email trực tiếp đến hòm thư sinh viên (định dạng HTML đẹp mắt).
- **Tính năng Snooze:** Cho phép hẹn giờ "Nhắc lại sau X phút" ngay trên thông báo.

### 3. Tư Vấn Đăng Ký Học Phần (Schedule Advisor)
- Gợi ý môn học thông minh dựa trên: Tích lũy tín chỉ hiện tại, Môn chưa học, Điều kiện tiên quyết.
- Ưu tiên xếp lớp với các giảng viên có điểm đánh giá (Rating) cao.
- Tự động sinh ra các phương án thời khóa biểu tối ưu và không bị trùng lặp thời gian.

### 4. Quản Lý Hồ Sơ & Tài Khoản (Profile & Auth)
- **Bảo mật:** Đăng nhập, Đăng ký, Đổi mật khẩu. Mã hóa mật khẩu an toàn bằng `BCrypt`.
- **Quên mật khẩu:** Xác thực an toàn bằng mã OTP 6 số gửi trực tiếp qua Email.
- **Hồ sơ cá nhân:** Cập nhật thông tin chi tiết, cắt/đổi ảnh đại diện (Avatar), quản lý danh mục Trường/Khoa/Ngành/Lớp/Nhóm linh hoạt.

### 5. Diễn Đàn Sinh Viên (Forum)
- Đăng bài viết chia sẻ, thảo luận. Hỗ trợ đính kèm hình ảnh, tùy chỉnh màu nền và chèn Emoji.
- Chế độ đăng bài ẩn danh (Anonymous).
- Tính năng bình luận tương tác.

### 6. Phân Hệ Quản Trị (Admin Management)
- Quản lý, tìm kiếm danh sách sinh viên. Xem thẻ sinh viên (ID Card).
- Quản lý dữ liệu danh mục động (cập nhật đồng thời vào Database và file JSON nội bộ).

---

## 💻 Công Nghệ Sử Dụng
- **Framework:** .NET WPF (Windows Presentation Foundation) / C#
- **Cơ sở dữ liệu:** Microsoft SQL Server (ADO.NET)
- **Thư viện bên thứ ba nổi bật:**
  - `Newtonsoft.Json`: Xử lý và phân tích cấu trúc dữ liệu JSON.
  - `BCrypt.Net-Next`: Băm (Hash) và kiểm chứng mật khẩu.
  - `FontAwesome.WPF`: Tích hợp các bộ icon vector hiện đại.

---

## 🚀 Hướng Dẫn Cài Đặt

### Bước 1: Chuẩn Bị Cơ Sở Dữ Liệu
- Cài đặt SQL Server (khuyến nghị bản Express).
- Tạo database có tên `PBL3` và chạy script SQL để tạo bảng/dữ liệu mẫu.

### Bước 2: Cấu Hình Ứng Dụng (`AppConfig.cs`)
Mở file `AppConfig.cs` trong thư mục gốc dự án và cập nhật các thông tin sau:
1. **Chuỗi kết nối SQL (ConnectionString):** Đảm bảo `Server` và `Database` khớp với máy chủ của bạn.
2. **Gmail SMTP:** Cung cấp `SenderEmail` và `SenderAppPassword` (Mật khẩu ứng dụng 16 ký tự của Google) để kích hoạt tính năng gửi OTP và Thông báo tự động.
3. **Thư mục RENDER:** Đổi đường dẫn `RenderFolderPath` trỏ đến thư mục chứa các file dữ liệu JSON cứng trên máy bạn (`Organization.json`, `TimeWeekStart.json`, `HK2_2025.json`...).

### Bước 3: Biên Dịch & Chạy
- Mở project bằng **Visual Studio**.
- Chuột phải vào Solution -> Chọn **Restore NuGet Packages**.
- Nhấn **F5** để bắt đầu chạy ứng dụng.

---

## 💡 Lưu Ý Sử Dụng
- Khi nhấn nút **[X]** ở góc phải màn hình, phần mềm sẽ không tắt hẳn mà sẽ **thu nhỏ xuống khay hệ thống (System Tray)** để đảm bảo tính năng thông báo luôn hoạt động.
- Để thoát phần mềm hoàn toàn, bạn cần chuột phải vào biểu tượng ở khay hệ thống và chọn **"Thoát"**.