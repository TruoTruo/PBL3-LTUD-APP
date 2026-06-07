# 🎓 Student Reminder & Advisor App

![.NET Framework](https://img.shields.io/badge/.NET_Framework-4.8-512BD4?style=for-the-badge&logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Desktop_App-blue?style=for-the-badge&logo=windows)
![SQL Server](https://img.shields.io/badge/SQL_Server-Database-CC292B?style=for-the-badge&logo=microsoft-sql-server)

**Student Reminder & Advisor** là một ứng dụng Desktop (WPF) toàn diện dành cho sinh viên. Ứng dụng giúp quản lý thời khóa biểu, nhắc nhở sự kiện, hỗ trợ tư vấn xếp lịch đăng ký học phần thông minh và tạo không gian giao lưu trực tuyến (Forum).

---

## 📑 Mục lục
- 🌟 Tính Năng Nổi Bật
- 💻 Công Nghệ Sử Dụng
- 🚀 Hướng Dẫn Cài Đặt Chi Tiết
- 📖 Hướng Dẫn Sử Dụng
- 🛡️ Xử Lý Lỗi Hệ Thống (Error Handling)
- Cấu Trúc Thư Mục
- 🤝 Hướng Dẫn Đóng Góp (Contributing)

---

## 🌟 Tính Năng Nổi Bật

### 1. Quản Lý Lịch Trình (Calendar & Schedule)
- Xem lịch linh hoạt: Chế độ Ngày (Time-grid), Tuần, Tháng, Năm.
- **Thao tác thông minh:** Kéo thả (Drag & Drop) chuột trực tiếp trên lưới lịch để tạo nhanh sự kiện theo khung giờ.
- **Nhập thời khóa biểu (Import):** Tự động đọc và lên lịch lặp lại cho các môn học từ file JSON.
- **Sự kiện lặp lại:** Hỗ trợ lặp sự kiện theo chu kỳ (Hằng ngày, Hằng tuần, Hằng tháng).
- **Quản lý phân loại (Tags):** Phân loại sự kiện bằng nhãn dán tùy chỉnh và màu sắc trực quan.
- **Cảnh báo xung đột:** Tự động phát hiện trùng lịch khi thêm khách mời vào sự kiện.

### 2. Hệ Thống Thông Báo Nhắc Nhở (Notification System)
- **Hoạt động ngầm:** Khi đóng cửa sổ, ứng dụng tự động thu nhỏ xuống khay hệ thống (System Tray) để duy trì việc quét sự kiện.
- **Đa kênh:** Nhắc nhở qua Popup ngay trên màn hình máy tính hoặc tự động gửi **Email định dạng HTML** chi tiết đến hòm thư sinh viên.
- **Tùy biến:** Tính năng "Báo lại" (Snooze) linh hoạt từ 5, 10 đến 15 phút.

### 3. Tư Vấn Đăng Ký Học Phần (Schedule Advisor)
- Tự động gợi ý môn học tiếp theo dựa trên: Môn đã tích lũy, điều kiện tiên quyết và học trước.
- Cho phép "săn" Giảng viên yêu thích.
- Thuật toán (Backtracking kết hợp Bitmask) sinh ra các phương án thời khóa biểu không trùng lặp, tối ưu theo các profile cá nhân hóa: *Chim sớm, Cú đêm, Dồn ngày nghỉ*.

### 4. Quản Lý Hồ Sơ & Diễn Đàn (Profile & Forum)
- Đăng nhập, đăng ký an toàn. Khôi phục mật khẩu thông qua mã OTP gửi tới Email.
- Diễn đàn sinh viên: Đăng bài, bình luận, ẩn danh, hỗ trợ Emoji và đính kèm ảnh.
- Quản lý danh mục động (Trường, Khoa, Ngành, Lớp, Nhóm) đồng bộ cả file JSON và Database.

---

## 💻 Công Nghệ Sử Dụng
- **Nền tảng:** .NET Framework 4.8 / C# WPF (Windows Presentation Foundation)
- **Cơ sở dữ liệu:** Microsoft SQL Server (ADO.NET)
- **Thư viện bên thứ ba:**
  - `Newtonsoft.Json`: Phân tích và xử lý dữ liệu JSON (Import TKB, Khung chương trình).
  - `BCrypt.Net-Next`: Băm và bảo mật mật khẩu.
  - `FontAwesome.WPF`: Giao diện icon vector hiện đại.

---

## ⚙️ Yêu Cầu Hệ Thống (Prerequisites)
Trước khi cài đặt, hãy đảm bảo máy tính của bạn đã cài đặt các phần mềm sau:
- **IDE:** [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) (Trong quá trình cài đặt cần check chọn workload **.NET desktop development**).
- **Framework:** .NET Framework 4.8.
- **Cơ sở dữ liệu:** [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) và [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).
- **Khác:** [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (Thường đã được cài sẵn mặc định trên Windows 10 và 11).

---

## 🚀 Hướng Dẫn Cài Đặt Chi Tiết

### Bước 1: Khởi tạo Cơ sở dữ liệu (Database)
1. Cài đặt và mở phần mềm **SQL Server Management Studio (SSMS)**.
2. Tạo một Database mới với tên là `PBL3` (hoặc tên tùy ý).
3. Mở file script SQL đính kèm trong dự án và chạy (`Execute` / `F5`) để tạo toàn bộ các bảng và dữ liệu mẫu.

### Bước 2: Chuẩn bị thư mục Dữ liệu (RENDER)
1. Ứng dụng cần đọc các file cấu hình cứng như `Organization.json`, `TimeWeekStart.json` và các file khung chương trình đào tạo.
2. Hãy copy thư mục `RENDER` đi kèm mã nguồn và lưu nó vào một vị trí cố định trên máy tính của bạn (Ví dụ: `D:\DoAn\RENDER`).

### Bước 3: Cấu hình `AppConfig.cs`
Mở file `AppConfig.cs` (nằm ở thư mục gốc của project) và điều chỉnh 3 thông số quan trọng sau:
1. **Chuỗi kết nối (ConnectionString):** Sửa lại thuộc tính `Server=...` cho khớp với SQL Server của bạn.
2. **Thư mục RENDER:** Trỏ biến `RenderFolderPath` tới chính xác đường dẫn thư mục RENDER ở Bước 2.
3. **Cấu hình Email (Gửi OTP & Nhắc nhở):** 
   - Nhập địa chỉ Gmail của bạn vào `SenderEmail`.
   - Truy cập trang *Tài khoản Google > Bảo mật > Xác minh 2 bước > Mật khẩu ứng dụng* để tạo mật khẩu 16 ký tự và dán nó vào `SenderAppPassword`.

### Bước 4: Biên dịch và Chạy
1. Mở file `StudentReminderApp.sln` bằng phần mềm **Visual Studio**.
2. Chuột phải vào *Solution 'StudentReminderApp'* trong cửa sổ *Solution Explorer* -> Chọn **Restore NuGet Packages** để tải các thư viện liên quan. (Các thư viện chính bao gồm: `Newtonsoft.Json`, `BCrypt.Net-Next`, `FontAwesome.WPF` và `Microsoft.Web.WebView2`).
3. Chọn menu **Build** -> **Build Solution** (Phím tắt `Ctrl + Shift + B`) để đảm bảo hệ thống đã biên dịch không có lỗi.
4. Nhấn `F5` hoặc click vào nút **Start** (Biểu tượng ▷ ở thanh công cụ) để khởi chạy phần mềm.

---

## 📖 Hướng Dẫn Sử Dụng

### 1. Cách Import Thời Khóa Biểu tự động
- Tại màn hình **Lịch học (Calendar)**, nhấn nút **"Nhập TKB"** ở góc trên cùng bên phải.
- Chọn file `HK2_2025.json` trong thư mục `RENDER`.
- Ứng dụng sẽ tự động phân tích từ tuần bắt đầu học và rải đều lịch học chính xác cho 15 tuần tiếp theo lên lưới lịch.

### 2. Sử dụng tính năng Tư Vấn Xếp Lịch
- Chuyển sang thẻ **Tư vấn học phần**. Hệ thống sẽ truy xuất Database để loại bỏ các môn đã qua và gợi ý các môn bạn có thể đăng ký.
- Tick chọn các môn bạn muốn học trong kỳ này -> Bấm **Tiếp tục**.
- Cấu hình thêm Giảng viên yêu thích (Nếu có) và Chọn Profile xếp lịch (Ví dụ: *Ưu tiên lịch học buổi sáng*).
- Bấm **Tự động xếp lịch**. Thuật toán sẽ tính toán và đưa ra 3 phương án tốt nhất.
- Lựa chọn phương án ưng ý nhất -> Bấm **Xác nhận lưu file** để xuất ra file JSON. *(Bạn có thể dùng chính file này để Import TKB ở mục 1).*

### 3. Đảm bảo nhận Thông báo nhắc nhở
- Vào mục **Hồ sơ cá nhân > Cài đặt nhắc nhở**.
- Bật công tắc "Kích hoạt thông báo đẩy tự động".
- Chọn kênh nhận thông báo là **Cả hai (Popup & Email)**.
- Bấm **Lưu cài đặt**.
- **LƯU Ý QUAN TRỌNG:** Khi sử dụng xong, bạn chỉ cần bấm nút **[X]** đóng cửa sổ. Phần mềm sẽ **ẩn xuống khay hệ thống** (Góc dưới cùng bên phải màn hình) để làm nhiệm vụ gửi thông báo ngầm cho bạn. 
- Để tắt phần mềm hoàn toàn, hãy nhấp chuột phải vào biểu tượng ở khay hệ thống và chọn **Thoát**.

---
*Dự án được thiết kế và tối ưu với ❤️ dành cho sinh viên.*

## 🛡️ Xử Lý Lỗi Hệ Thống (Error Handling)

Ứng dụng được tích hợp cơ chế bắt lỗi toàn cục (Global Exception Handling) tại file `App.xaml.cs` thông qua sự kiện `DispatcherUnhandledException`. Khi xảy ra lỗi (Crash) ngoài ý muốn, hệ thống sẽ:
1. **Ngăn văng ứng dụng đột ngột:** Chặn ứng dụng tự động đóng băng hoặc tắt ngang.
2. **Ghi Log tự động:** Lưu lại chi tiết lỗi (Error Message & Stack Trace) vào file `StudentReminderApp_CrashLog.txt` tại thư mục `Documents` (My Documents) của máy tính.
3. **Thông báo thân thiện:** Hiển thị hộp thoại thông báo lỗi cho người dùng biết, sau đó mới tiến hành đóng ứng dụng một cách an toàn.

Cơ chế này giúp lập trình viên và người dùng dễ dàng truy vết nguyên nhân để khắc phục lỗi kịp thời, tăng tính ổn định cho ứng dụng.

---

## 📂 Cấu Trúc Thư Mục (Project Structure)

Dự án được phân chia rõ ràng theo kiến trúc **3 Lớp (3-Tier Architecture)** kết hợp với mẫu thiết kế **MVVM**, giúp mã nguồn dễ đọc, dễ bảo trì và mở rộng:

```text
StudentReminderApp/
├── BLL/                     # Business Logic Layer (Xử lý nghiệp vụ)
│   ├── Logic/               # Các file nghiệp vụ (AccountBLL, EventBLL,...)
│   └── Services/            # Các dịch vụ dùng chung (Email, Thuật toán, API...)
├── DAL/                     # Data Access Layer (Truy cập cơ sở dữ liệu)
│   ├── Data/                # Tương tác SQL Server qua ADO.NET (BaseDAL, StudentDAL,...)
│   └── Models/              # Thực thể mô tả cấu trúc dữ liệu (Models)
├── GUI/                     # Graphical User Interface (Giao diện người dùng)
│   ├── ViewModels/          # Data context kết nối với View (MVVM)
│   ├── Views/               # Giao diện XAML (Windows, Pages, Dialogs)
│   ├── Helpers/             # Lớp tiện ích giao diện (RelayCommand, Session...)
│   └── Resources/           # Tài nguyên (Icons, Images, Styles, Light/Dark Themes)
├── data/                    # Nơi chứa các file lưu trữ cục bộ (ảnh thẻ, dữ liệu tĩnh...)
├── AppConfig.cs             # Khai báo cấu hình chung (Chuỗi kết nối DB, Email OTP)
└── App.xaml                 # Điểm bắt đầu khởi chạy ứng dụng (Entry point)
```
