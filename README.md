# BMovie WebXemPhim

> Nền tảng xem phim trực tuyến hiện đại xây dựng trên ASP.NET Core 8, kết hợp Razor Pages, MVC và Blazor Server để mang lại trải nghiệm mượt mà cho người dùng cuối lẫn quản trị viên.

![Home](docs/screens/home.png)

## Mục lục

1. [Tổng quan](#tổng-quan)
2. [Kiến trúc & Công nghệ](#kiến-trúc--công-nghệ)
3. [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
4. [Cấu trúc thư mục](#cấu-trúc-thư-mục)
5. [Bắt đầu nhanh](#bắt-đầu-nhanh)
6. [Quản lý secrets & biến môi trường](#quản-lý-secrets--biến-môi-trường)
7. [Tính năng nổi bật](#tính-năng-nổi-bật)
8. [Bảo mật & vận hành](#bảo-mật--vận-hành)
9. [Quy trình phát triển](#quy-trình-phát-triển)
10. [Scripts & công cụ](#scripts--công-cụ)
11. [Roadmap gợi ý](#roadmap-gợi-ý)

## Tổng quan

BMovie WebXemPhim cung cấp trải nghiệm xem phim toàn diện với giao diện hiện đại, bộ player linh hoạt, hệ thống đề xuất và trang quản trị mạnh mẽ. Dự án được thiết kế hướng tới khả năng mở rộng, dễ quan sát và an toàn dữ liệu người dùng.

## Kiến trúc & Công nghệ

- **ASP.NET Core 8** với Razor Pages, MVC và Blazor components tái sử dụng.
- **Entity Framework Core + SQL Server** (migrations nằm trong `Data/Migrations`).
- **SignalR**, **gRPC**, **OpenTelemetry**, **Azure Search**, **Azure Blob Storage**.
- **Bootstrap 5**, **Font Awesome**, **GSAP** cho giao diện responsive và giàu hiệu ứng.
- **ASP.NET Identity** + OAuth (Google, Facebook, Microsoft) + JWT cho API riêng.

## Yêu cầu hệ thống

- .NET 8 SDK.
- SQL Server (Express hoặc Azure SQL).
- Node.js 18+ (chỉ cần khi build asset front-end nâng cao).

## Cấu trúc thư mục

| Thư mục / file | Nội dung |
| --- | --- |
| `Controllers/` | MVC + API controllers (`Movies`, `Upload`, `Comments`, …). |
| `Areas/Admin/` | Razor views/layout cho trang quản trị. |
| `Components/` | Thư viện Blazor components dùng chung. |
| `Services/` | Business services (Recommendation, Coin, Subtitle, Azure, TMDb, …). |
| `Data/` | `ApplicationDbContext`, migrations, helper seed data. |
| `Views/` | Razor views cho người dùng cuối. |
| `wwwroot/` | CSS/JS tĩnh, plugin GSAP, assets được upload. |
| `wwwroot/js/theme-toggle-enhanced.js` | Điều khiển dark/light mode kết hợp GSAP. |
| `wwwroot/css/navbar-fix.css`, `wwwroot/css/modern.css` | Bộ style thống nhất theo theme. |

## Bắt đầu nhanh

1. **Clone dự án**

   ```bash
   git clone <remote-url>/BMovie-WebXemPhim.git
   cd BMovie-WebXemPhim
   ```

2. **Khởi tạo secrets**

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=BMovie;Trusted_Connection=True;MultipleActiveResultSets=true"
   dotnet user-secrets set "TMDb:ApiKey" "<tmdb-key>"
   dotnet user-secrets set "Azure:Blob:ConnectionString" "<secure-conn-string>"
   dotnet user-secrets set "Authentication:Google:ClientId" "<id>"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "<secret>"
   ```

3. **Khôi phục & migrate database**

   ```bash
   dotnet restore
   dotnet tool install --global dotnet-ef # nếu chưa có
   dotnet ef database update
   ```

   > Khi khởi chạy, hệ thống cũng tự đồng bộ bảng `ViewHits` (các cột `WatchProgress`, `Duration`, `EpisodeNumber`).

4. **Chạy ứng dụng**

   ```bash
   dotnet run
   ```

   - Người dùng: [http://localhost:5196/](http://localhost:5196/)
   - Admin: [http://localhost:5196/Admin](http://localhost:5196/Admin) (cần tài khoản role `Admin`)

## Quản lý secrets & biến môi trường

- Luôn sử dụng `dotnet user-secrets`, biến môi trường hoặc Azure Key Vault/HashiCorp Vault cho môi trường production.
- Không commit `appsettings.*.json` chứa thông tin nhạy cảm.
- Với CI/CD, ưu tiên inject secrets thông qua dịch vụ secret manager thay vì file.

## Tính năng nổi bật

- Trang chủ đa danh mục với hiệu ứng GSAP, chuyển dark/light mode mượt mà.
- Trang chi tiết phim (`/movie/{slug}`) gồm hero poster, tabs tập phim, gallery, diễn viên, bình luận.
- Player `/watch/{slug}` chọn server, tập, phụ đề, kèm đề xuất tự động theo hành vi.
- Trang Admin quản lý phim, thể loại, nguồn phát, import dữ liệu Rophim, dashboard thống kê lượt xem.
- Quick upload trong trang Admin với **Tải nhanh**, progress bar, ước lượng thời gian và auto-fill URL sau khi tải xong.
- CoinService, hệ thống thành tựu, lịch sử xem và API nội bộ phục vụ gợi ý.
- OpenTelemetry + Azure Monitor giúp theo dõi log, metric, trace tập trung.

## Bảo mật & vận hành

- **HTTPS & TLS**: bật HSTS trong `appsettings.Production.json`, triển khai qua reverse proxy/ingress (Azure App Service, Nginx, IIS).
- **Kiểm soát truy cập**: ASP.NET Identity, chính sách mật khẩu mạnh, khóa tài khoản khi đăng nhập sai nhiều lần, role `Admin` cho `Areas/Admin`.
- **API protection**: `ApiKeyAuthenticationMiddleware` cho `/api/public/*`, Rate limiting, `[Authorize]` trên các controller nhạy cảm như Upload.
- **Upload an toàn**: `RequestSizeLimit`, `RequestFormLimits`, kiểm tra MIME/extension, đổi tên file bằng `Guid`, lưu tại `wwwroot/uploads/videos/` với quyền hạn chế; có thể tích hợp virus scanning/Content Moderator.
- **Quan sát hệ thống**: OpenTelemetry, Application Insights, health checks tại `/health`.
- **Sao lưu**: migrations version hóa schema, khuyến nghị backup định kỳ cho SQL Server/Azure SQL và Blob Storage.

## Quy trình phát triển

1. Làm việc trên branch feature, bật `dotnet format` và `dotnet test` trước khi mở PR.
2. Tạo migration cho mọi thay đổi database: `dotnet ef migrations add <Name>`.
3. Rà soát bảo mật theo OWASP Top 10, đặc biệt khi mở rộng API.
4. Kiểm thử upload (kích thước tối đa hiện tại 2GB), hạn chế MIME và đánh giá nhu cầu chunk/resumable upload.

## Scripts & công cụ

- `dotnet watch run` – hot reload khi phát triển.
- `dotnet ef migrations add <Name>` – tạo migration mới.
- `npm install && npm run build` – build assets khi cần mở rộng front-end pipeline.
- `docs/` – bổ sung tài liệu triển khai (Docker, Azure, CI/CD…).

## Roadmap gợi ý

- Thêm Dockerfile + pipeline CI/CD tự động (GitHub Actions/Azure DevOps).
- Tích hợp Azure Key Vault hoặc HashiCorp Vault cho secrets production.
- Bổ sung upload dạng chunk/resumable, quét virus sau upload và cảnh báo nội dung.
- Hoàn thiện bộ kiểm thử (unit, integration) và performance test player.

---

BMovie WebXemPhim hướng tới trải nghiệm xem phim hiện đại, an toàn và dễ mở rộng. Vui lòng tạo issue nếu phát hiện lỗ hổng bảo mật hoặc gửi pull request theo quy trình nội bộ.
