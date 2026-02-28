# Cấu Hình Biến Môi Trường (User Secrets)

Dự án này sử dụng tính năng **User Secrets** của .NET (Secret Manager) để quản lý các thông tin nhạy cảm như chuỗi kết nối Database, mật khẩu, và JWT Key. 

Các thông tin này **chỉ tồn tại cục bộ trên máy lập trình viên** và **không bao giờ bị đẩy lên GitHub** hay bất cứ hệ thống quản lý mã nguồn nào. Cơ chế này an toàn hơn rất nhiều so với việc để thông tin trong `appsettings.json`.

---

## 🚀 Cách Cài Đặt Ban Đầu Cho Máy Mới (Hoặc Sau Khi Clone Code)

Mở **Terminal / Command Prompt** tại thư mục chứa file `.csproj` (ví dụ: `Cake_Design&E-Commerce_Platform/Cake_Design&E-Commerce_Platform`), và chạy lần lượt các lệnh sau để nạp dữ liệu vào máy của bạn:

### 1. Chuỗi kết nối Database (MySQL)
Cập nhật username/password cho đúng với DB MySQL trên máy của bạn:
```bash
dotnet user-secrets set "ConnectionStrings:MySqlConnection" "Server=localhost;Port=3306;Database=quanlybanhang_db;Uid=root;Pwd=12345;Charset=utf8mb4;"
```

### 2. JWT Secret Key (Dùng để mã hoá Token)
Thay đổi đoạn key này nếu cần, tối thiểu phải 32 ký tự:
```bash
dotnet user-secrets set "Jwt:SecretKey" "CakeDesignAndECommerceSuperSecretKey2026!!!"
```

### 3. API ViettelPost (Lấy mã tỉnh, huyện)
Liên hệ team lead để lấy tài khoản ViettelPost:
```bash
dotnet user-secrets set "ViettelPost:Username" "<VIETTELPOST_USERNAME>"
dotnet user-secrets set "ViettelPost:Password" "<VIETTELPOST_PASSWORD>"
```

### 4. VNPay (Sandbox / Test)
Code và HashSecret môi trường Test VNPAY:
```bash
dotnet user-secrets set "Vnpay:TmnCode" "15W7TLGZ"
dotnet user-secrets set "Vnpay:HashSecret" "4J18XEN8G994B92C9W5DK5DPX0XAPB3J"
```

### 5. Seeder Default Password (Database Mock)
Mật khẩu chung cho tất cả tài khoản được tạo tự động khi khởi động app:
```bash
dotnet user-secrets set "SeedData:DefaultPassword" "123456"
```

---

## 🛠️ Một Số Câu Lệnh Hữu Ích Khác

**Liệt kê tất cả các biến đã lưu:**
```bash
dotnet user-secrets list
```

**Xóa một biến đã lưu:**
```bash
dotnet user-secrets remove "Jwt:SecretKey"
```

**Xóa TẤT CẢ các biến đã lưu (ẩn danger zone):**
```bash
dotnet user-secrets clear
```

**Lưu ý:** Nếu thiếu bất kỳ cấu hình nào ở trên, ứng dụng khi khởi chạy có thể sẽ báo lỗi `InvalidOperationException` kèm theo hướng dẫn cấu hình chi tiết ở cửa sổ Log.
