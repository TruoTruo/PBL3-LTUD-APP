# Student Reminder & Advisor — WPF App

## Cấu trúc project
```
StudentReminderApp/
├── database_setup.sql          ← Chạy cái này trước
└── StudentReminderApp/
    ├── StudentReminderApp.csproj
    ├── App.xaml / App.xaml.cs
    ├── AppConfig.cs            ← SỬA CONNECTION STRING Ở ĐÂY
    ├── Models/
    ├── Helpers/
    ├── Converters/
    ├── DAL/
    ├── BLL/
    ├── Services/
    ├── Resources/
    └── Views/
        ├── Auth/      (Login, Register)
        ├── Main/      (MainWindow - Shell)
        ├── Pages/     (Dashboard, Calendar, Course, Advisor, Profile)
        └── Dialogs/   (EventDialog, NotificationPopup)
```

## Hướng dẫn chạy

### Bước 1 — Tạo Database
1. Mở **SQL Server Management Studio**
2. Mở file `database_setup.sql`
3. Nhấn **F5** để chạy toàn bộ script
4. Kiểm tra database **PBL3** đã được tạo

### Bước 2 — Sửa Connection String
Mở file `AppConfig.cs` và sửa:
```csharp
"Server=TÊN_MÁY_BẠN;Database=PBL3;Trusted_Connection=True;..."
```
Ví dụ: `Server=DESKTOP-ABC123\SQLEXPRESS` hoặc `Server=localhost`

### Bước 3 — Mở project trong Visual Studio
1. **Visual Studio 2022** → Open → Project/Solution
2. Chọn file `StudentReminderApp.csproj`
3. Chờ NuGet restore tự động (BCrypt.Net-Next + System.Data.SqlClient)

### Bước 4 — Build & Run
- Nhấn **F5** hoặc **Ctrl+F5**
- Màn hình Login sẽ hiện ra
- Nhấn "Đăng ký ngay" để tạo tài khoản đầu tiên

## Công nghệ sử dụng
- **WPF** (.NET Framework 4.8 / .NET 6+)
- **SQL Server** (Express hoặc Full)
- **BCrypt.Net-Next** — mã hóa mật khẩu
- **System.Data.SqlClient** — kết nối database

## Tài khoản test
Đăng ký qua giao diện. Không có tài khoản mặc định vì mật khẩu được hash BCrypt.

## Lưu ý
- Thay `Server=localhost` thành tên SQL Server instance của bạn
- Nếu dùng SQL Server Express: `Server=.\SQLEXPRESS`
- Project yêu cầu Visual Studio 2019+ hoặc VS Code với C# extension
