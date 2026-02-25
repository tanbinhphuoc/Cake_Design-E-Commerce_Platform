# 🍰 Cake Design & E-Commerce Platform

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean_Architecture-ff69b4?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-blue.svg?style=for-the-badge)

Hệ thống Quản lý và Thiết kế Bánh trực tuyến toàn diện. Dự án cung cấp một nền tảng thương mại điện tử mạnh mẽ kết hợp với công cụ cho phép khách hàng cá nhân hóa chiếc bánh của mình. Được xây dựng dựa trên nguyên lý **Clean Architecture** và áp dụng các **Design Pattern** chuẩn mực trong C#, hệ thống đảm bảo tính mở rộng cao, dễ dàng bảo trì và kiểm thử.

## ✨ Tính năng nổi bật

* **🎨 Custom Cake Design:** Khách hàng có thể tự do tùy biến thiết kế bánh (hương vị, kích thước, màu sắc, phụ kiện trang trí).
* **🛒 E-Commerce Core:** Quản lý giỏ hàng, quy trình thanh toán và theo dõi tiến độ đơn hàng.
* **⚙️ Bakery Management:** Bảng điều khiển (Dashboard) dành cho cửa hàng để quản lý danh mục sản phẩm, xử lý đơn đặt bánh custom và quản lý người dùng.
* **🔒 Bảo mật & Xác thực:** Quản lý phân quyền chặt chẽ giữa Khách hàng, Nhân viên và Quản trị viên.

## 🏛 Kiến trúc Hệ thống (Clean Architecture)

Dự án được phân tách nghiêm ngặt thành các layer để đảm bảo Separation of Concerns (SoC):

📦 Cake_Design-E-Commerce_Platform
 ┣ 📂 Domain           # Chứa Entities, Value Objects, Domain Exceptions và Interfaces cốt lõi
 ┣ 📂 Application      # Chứa Use Cases (CQRS), DTOs, và Business Logic
 ┣ 📂 Infrastructure   # Triển khai Data Access (Entity Framework Core), Identity, External Services
 ┗ 📂 Presentation     # (API/Web) Chứa Controllers, Middleware và Routing


## 🚀 Công nghệ sử dụng

* **Backend:** C# & .NET 8
* **Kiến trúc:** Clean Architecture, CQRS, Repository Pattern
* **Cơ sở dữ liệu:** Entity Framework Core (MySQL Server)
* **API:** RESTful API, Swagger/OpenAPI

## 🛠 Hướng dẫn Cài đặt & Chạy dự án local

Làm theo các bước sau để thiết lập môi trường phát triển trên máy của bạn:

### Yêu cầu hệ thống

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* IDE: Visual Studio 2022 / Rider / VS Code
* SQL Server (hoặc Database Engine tương ứng được cấu hình trong `appsettings.json`)

### Các bước cài đặt

1. **Clone repository:**
git clone [https://github.com/tanbinhphuoc/Cake_Design-E-Commerce_Platform.git](https://github.com/tanbinhphuoc/Cake_Design-E-Commerce_Platform.git)
cd Cake_Design-E-Commerce_Platform



2. **Khôi phục các packages (Restore):** 
dotnet restore "Cake_Design&E-Commerce_Platform.sln"



3. **Cập nhật Database (Migration):**
*(Đảm bảo bạn đã thay đổi chuỗi kết nối - Connection String trong file `appsettings.json` ở project Presentation)*
dotnet ef database update --project Infrastructure --startup-project Presentation



4. **Chạy ứng dụng:**
dotnet run --project Cake_Design&E-Commerce_Platform



5. **Truy cập Swagger:** Mở trình duyệt và truy cập `https://localhost:<port>/swagger` để kiểm thử các API endpoints.

## 🤝 Đóng góp (Contributing)

Mọi đóng góp (Pull Requests, Issues) đều được chào đón nhằm hoàn thiện hệ thống quản lý tiệm bánh này. Vui lòng mở một Issue để thảo luận về thay đổi lớn trước khi tạo Pull Request.

## 📄 Giấy phép (License)

Dự án được phân phối dưới giấy phép MIT. Xem file `LICENSE` để biết thêm chi tiết.



