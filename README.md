# BMovie WebXemPhim

Ứng dụng xem phim trực tuyến xây dựng trên ASP.NET Core 8 kết hợp Razor Pages, MVC và Blazor Server. Hệ thống cung cấp trải nghiệm người dùng hiện đại (GSAP animations, dark/light mode) và một trang quản trị đầy đủ cho việc nhập phim, theo dõi lượt xem, phần thưởng và điều phối nội dung.

![Home](docs/screens/home.png) <!-- cập nhật sau nếu cần -->

## Kiến trúc & Công nghệ

- **ASP.NET Core 8** với Razor Pages + MVC + Blazor components
- **Entity Framework Core + SQL Server** (migrations trong `Data/Migrations`)
- **SignalR**, **gRPC**, **OpenTelemetry**, **Azure Search** và **Azure Blob Storage**
- **Bootstrap 5**, **Font Awesome**, **GSAP** cho phần giao diện
- **ASP.NET Identity** với social login (Google, Facebook, Microsoft) + JWT cho API

## Yêu cầu hệ thống

- .NET 8 SDK
- SQL Server (Express/Azure SQL đều được)
- Node.js 18+ (chỉ cần nếu build asset front-end nâng cao)

## Cấu trúc thư mục chính

| Thư mục / file | Nội dung |
| --- | --- |
| `Controllers/` | MVC + API controllers (`Movies`, `Upload`, `Comments`, …) |
| `Areas/Admin/` | Razor views/layout trang quản trị |
| `Components/` | Blazor components dùng chung cho giao diện |
| `Services/` | Business services (Recommendation, Coin, Subtitle, Azure, TMDb, …) |
| `Data/` | `ApplicationDbContext`, migrations, seed helper |
| `Views/` | Razor views cho người dùng cuối |
| `wwwroot/` | CSS/JS tĩnh, plugin GSAP, assets upload |
| `wwwroot/js/theme-toggle-enhanced.js` | Điều khiển dark/light mode + GSAP animation |
| `wwwroot/css/navbar-fix.css`, `wwwroot/css/modern.css` | Bộ style đồng bộ theo theme |

## Bắt đầu nhanh

1. **Clone và vào thư mục dự án**
   ```bash
   git clone <remote-url-cua-ban>/BMovie-WebXemPhim.git
   cd BMovie-WebXemPhim
   ```

2. **Cấu hình secrets (bảo mật)**
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=BMovie;Trusted_Connection=True;MultipleActiveResultSets=true"
   dotnet user-secrets set "TMDb:ApiKey" "<tmdb-key>"
   dotnet user-secrets set "Azure:Blob:ConnectionString" "<secure-conn-string>"
   dotnet user-secrets set "Authentication:Google:ClientId" "<id>"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "<secret>"
   ```
   > Không commit các khóa này. Sử dụng biến môi trường hoặc Azure Key Vault trong môi trường production.

3. **Khôi phục & migrate database**
   ```bash
   dotnet restore
   dotnet tool install --global dotnet-ef # nếu chưa cài
   dotnet ef database update
   ```
   Hệ thống khi chạy sẽ tự kiểm tra bảng `ViewHits` và tạo các cột (`WatchProgress`, `Duration`, `EpisodeNumber`) nếu thiếu.

4. **Chạy ứng dụng**
   ```bash
   dotnet run
   ```
   - Người dùng: http://localhost:5196/
   - Admin: http://localhost:5196/Admin (cần tài khoản role `Admin`)

## Tính năng nổi bật

- Trang chủ nhiều danh mục với hiệu ứng GSAP, hỗ trợ dark/light chuyển đổi mượt mà.
- Trang chi tiết phim (`/movie/{slug}`) gồm hero poster, tabs tập phim, gallery, diễn viên, bình luận.
- Player `/watch/{slug}` cho phép đổi server, tập, phụ đề, đề xuất tức thời.
- Trang Admin thêm/sửa phim, thể loại, nguồn phát, import dữ liệu Rophim, thống kê lượt xem.
- Quick upload video trong trang Admin (Create/Edit) có nút **Tải nhanh**, progress bar, thời gian ước tính và tự chèn URL sau khi upload.
- API nội bộ phục vụ gợi ý, điểm thưởng (CoinService), thành tựu, lịch sử xem.
- Hạ tầng quan sát với OpenTelemetry + Azure Monitor, log trung tâm.

## Chính sách bảo mật & vận hành

**Bảo vệ secrets**
- Lưu chuỗi kết nối, API key TMDb/Azure/Social login trong `dotnet user-secrets`, biến môi trường hoặc Key Vault.
- Không commit file `appsettings.*.json` chứa thông tin nhạy cảm.

**Giao thức & chứng chỉ**
- Bắt buộc HTTPS trong `appsettings.Production.json` và bật HSTS khi deploy thực tế.
- Dùng reverse proxy/ingress thiết lập TLS (Azure App Service, Nginx, IIS).

**Quản lý người dùng**
- ASP.NET Identity với xác thực đa nhà cung cấp.
- Role `Admin` bắt buộc cho khu vực `Areas/Admin`.
- Áp dụng chính sách mật khẩu mạnh + khóa tài khoản khi đăng nhập sai nhiều lần.

**Chống tấn công API**
- Middleware `ApiKeyAuthenticationMiddleware` cho các route `/api/public/*` với rate limiting.
- Controllers yêu cầu `[Authorize]` (ví dụ Upload API) để ngăn upload ẩn danh.
- Sử dụng `RequestSizeLimit`, `RequestFormLimits`, kiểm tra MIME/extension, đổi tên file bằng `Guid` khi lưu.

**Bảo vệ dữ liệu tĩnh**
- Tệp upload lưu trong `wwwroot/uploads/videos/`. Phân quyền thư mục chỉ cho process của ứng dụng.
- Có thể bật thêm virus scanning/Content Moderator trước khi public.

**Giám sát & nhật ký**
- OpenTelemetry thu thập trace/metric gửi lên Azure Monitor Application Insights.
- Health checks (`/health`) kiểm tra database và dịch vụ quan trọng.

**Sao lưu & phục hồi**
- Migrations giúp version hóa schema.
- Khuyến nghị cấu hình backup định kỳ cho SQL Server/Azure SQL và Blob Storage.

## Quy trình phát triển an toàn

1. Làm việc trên branch riêng, bật `dotnet format` và `dotnet test` trước khi merge.
2. Dùng `dotnet ef migrations add <Name>` cho mọi thay đổi database.
3. Kiểm tra lỗ hổng OWASP Top 10 (XSS, CSRF đã được ASP.NET MVC bảo vệ mặc định, nhưng cần xem lại khi thêm API mới).
4. Đối với file upload, chỉ bật MIME cần thiết và quét kích thước tối đa (hiện tại 2GB).

## Scripts & công cụ hữu ích

- `dotnet watch run` – hot reload khi phát triển.
- `dotnet ef migrations add <Name>` – tạo migration mới.
- `npm install && npm run build` – build asset nếu mở rộng pipeline front-end.
- `docs/` – nơi đặt thêm tài liệu triển khai (Docker, Azure, CI/CD…).

## Roadmap gợi ý

- Bổ sung Dockerfile + pipeline CI/CD để triển khai tự động.
- Tích hợp Azure Key Vault hoặc HashiCorp Vault cho secrets production.
- Thêm kiểm thử tải lên (chunk upload, resumable) và quét virus sau upload.

---

BMovie WebXemPhim hướng tới trải nghiệm xem phim toàn diện, có khả năng mở rộng và đảm bảo an toàn dữ liệu người dùng. Vui lòng tạo issue nếu phát hiện lỗ hổng bảo mật hoặc gửi pull request với bản vá theo quy trình bảo mật nội bộ.

