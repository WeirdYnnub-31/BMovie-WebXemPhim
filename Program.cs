using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Enable static web assets for Blazor framework files
builder.WebHost.UseStaticWebAssets();

const long MaxUploadBytes = 2L * 1024 * 1024 * 1024; // 2GB

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
// Add Blazor Server support for interactive components (Hybrid approach)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// Add Server-side Blazor authentication support
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, 
    Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("AzureOpenAI", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>().GetSection("AzureOpenAI");
    var endpoint = cfg["Endpoint"] ?? string.Empty;
    var apiKey = cfg["ApiKey"] ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(endpoint)) client.BaseAddress = new Uri(endpoint);
    if (!string.IsNullOrWhiteSpace(apiKey)) client.DefaultRequestHeaders.Add("api-key", apiKey);
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxUploadBytes;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxUploadBytes;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = MaxUploadBytes;
});
builder.Services.AddSignalR();
builder.Services.AddGrpc();
builder.Services.AddSingleton<webxemphim.Services.AzureSearchService>();
builder.Services.AddSingleton<webxemphim.Services.CdnService>();
builder.Services.AddSingleton<webxemphim.Services.BlobStorageService>();
builder.Services.AddSingleton<webxemphim.Services.AI.OpenAIService>();
builder.Services.AddSingleton<webxemphim.Services.AI.ModerationService>();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache(); // For RecommendationService caching
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("api-default", p => p.Expire(TimeSpan.FromSeconds(30)).SetVaryByQuery("page", "pageSize", "genre", "search", "sortBy"));
    options.AddPolicy("movies-list", p => p.Expire(TimeSpan.FromMinutes(5)).SetVaryByQuery("page", "genre", "search", "sortBy"));
    options.AddPolicy("movie-detail", p => p.Expire(TimeSpan.FromMinutes(10)).SetVaryByRouteValue("slug"));
    options.AddPolicy("home-page", p => p.Expire(TimeSpan.FromMinutes(5)));
});
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", o =>
    {
        o.Window = TimeSpan.FromSeconds(1);
        o.PermitLimit = 20; // 20 req/s per client IP
        o.QueueLimit = 0;
    });
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<webxemphim.Data.ApplicationDbContext>(name: "db");

// OpenTelemetry + Azure Monitor (App Insights) - optional if connection string provided
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: "webxemphim", serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService("webxemphim"))
    .WithTracing(tracer => tracer
        .SetResourceBuilder(resourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(resourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

var aiConn = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConn))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor(o => o.ConnectionString = aiConn);
}

// API Configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "WebXemPhim API", 
        Version = "v1",
        Description = "API for webxemphim movie streaming platform"
    });
});

// CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
builder.Services.AddDbContext<webxemphim.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql =>
    {
        sql.EnableRetryOnFailure(5);
    }));
builder.Services.AddIdentity<webxemphim.Data.ApplicationUser, IdentityRole>(options =>
{
    // Password settings - giảm yêu cầu để dễ đăng ký
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 0;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<webxemphim.Data.ApplicationDbContext>()
    .AddDefaultTokenProviders();
// External authentication providers (read from configuration)
builder.Services.AddAuthentication()
    .AddJwtBearer("JwtBearer", options =>
    {
        var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-super-secret-key-change-this-in-production-minimum-32-characters";
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "webxemphim";
        var audience = builder.Configuration["Jwt:Audience"] ?? "webxemphim-users";

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? string.Empty;
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? string.Empty;
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? string.Empty;
    });
builder.Services.Configure<webxemphim.Models.TMDbOptions>(builder.Configuration.GetSection("TMDb"));
builder.Services.AddHttpClient("TMDbClient", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<webxemphim.Models.TMDbOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
});
builder.Services.AddHttpClient("RophimScraper", client =>
{
    client.BaseAddress = new Uri("https://www.rophim.li/");
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
});
builder.Services.AddScoped<webxemphim.Services.TMDbService>();
builder.Services.AddScoped<webxemphim.Services.RecommendationService>();
builder.Services.AddScoped<webxemphim.Services.AIRecommendationService>();
builder.Services.AddScoped<webxemphim.Services.RatingService>();
builder.Services.AddScoped<webxemphim.Services.MovieStateService>();
builder.Services.AddScoped<webxemphim.Services.RophimScraperService>();
builder.Services.AddScoped<webxemphim.Services.JwtService>();
builder.Services.AddScoped<webxemphim.Services.StreamProtectionService>();
builder.Services.AddScoped<webxemphim.Services.AuditLogService>();
builder.Services.AddScoped<webxemphim.Services.BackupService>();
builder.Services.AddScoped<webxemphim.Services.NotificationService>();
builder.Services.AddScoped<webxemphim.Services.WatchPartyService>();
builder.Services.AddScoped<webxemphim.Services.CoinService>();
builder.Services.AddScoped<webxemphim.Services.AchievementService>();
builder.Services.AddScoped<webxemphim.Services.SubtitleService>();
builder.Services.AddScoped<webxemphim.Services.SocialService>();
builder.Services.AddScoped<webxemphim.Services.DownloadProtectionService>();
builder.Services.AddScoped<webxemphim.Services.ApiKeyService>();
builder.Services.AddScoped<webxemphim.Services.WatchProgressService>();
builder.Services.AddScoped<webxemphim.Services.SessionManagementService>();
builder.Services.AddHostedService<webxemphim.Services.TrendingMoviesBackgroundService>();
builder.Services.AddHostedService<webxemphim.Services.BmovieSyncBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebXemPhim API V1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseResponseCompression();

// CORS must come after UseRouting, before UseAuthentication
app.UseCors("AngularApp");

app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCaching(); // Enable response caching for ResponseCache attributes
app.UseOutputCache(); // Enable output caching
app.UseRateLimiter();

// API Key Authentication Middleware (must be after UseAuthentication and UseAuthorization)
app.UseMiddleware<webxemphim.Middleware.ApiKeyAuthenticationMiddleware>();

app.MapHub<webxemphim.Hubs.NotificationsHub>("/hubs/notifications");
app.MapHub<webxemphim.Hubs.ChatHub>("/hubs/chat");
app.MapHub<webxemphim.Hubs.WatchPartyHub>("/hubs/watchparty");

// Map Razor Pages FIRST to avoid routing conflicts
app.MapRazorPages();

// MVC Controller routes (after Razor Pages)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// Map Blazor Hub for interactive components (hybrid approach - components can be embedded in Razor Pages using RenderComponentAsync)
app.MapBlazorHub();
app.MapGrpcService<webxemphim.Services.Grpc.MoviesGrpcService>();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Minimal APIs v2
var apiV2 = app.MapGroup("/api/v2").WithTags("ApiV2").CacheOutput("api-default").RequireRateLimiting("api");

apiV2.MapGet("/genres", async (webxemphim.Data.ApplicationDbContext db) =>
{
    var genres = await db.Genres
        .AsNoTracking()
        .OrderBy(g => g.Name)
        .Select(g => new webxemphim.Models.DTOs.GenreDto
        {
            Id = g.Id,
            Name = g.Name,
            Slug = g.Slug,
            MovieCount = g.MovieGenres.Count
        })
        .ToListAsync();
    return Results.Ok(genres);
})
.Produces<List<webxemphim.Models.DTOs.GenreDto>>(StatusCodes.Status200OK)
.WithOpenApi(op =>
{
    op.Summary = "Get all genres";
    op.Description = "Returns the list of available genres with movie counts.";
    return op;
});

// Movies endpoints are handled by MoviesApiController to avoid route conflicts
// Removed duplicate Minimal API endpoints for /api/v2/movies and /api/v2/movies/{slug}
// These are now handled by MoviesApiController.GetMovies() and GetMovieBySlug()

apiV2.MapGet("/movies/{id:int}/similar", async (
    int id,
    int limit,
    webxemphim.Services.RecommendationService rec) =>
{
    if (limit <= 0 || limit > 24) limit = 8;
    var sims = await rec.GetSimilarMoviesAsync(id, limit);
    var items = sims.Select(m => new webxemphim.Models.DTOs.MovieListItemDto
    {
        Id = m.Id,
        Title = m.Title,
        Slug = m.Slug,
        PosterUrl = m.PosterUrl,
        Imdb = m.Imdb,
        Year = m.Year,
        AgeRating = m.AgeRating,
        IsSeries = m.IsSeries,
        ViewCount = m.ViewCount,
        AverageRating = m.AverageRating
    }).ToList();
    return Results.Ok(items);
})
.Produces<List<webxemphim.Models.DTOs.MovieListItemDto>>(StatusCodes.Status200OK)
.WithOpenApi(op =>
{
    op.Summary = "Similar movies";
    op.Description = "Returns similar movies based on a movie id. Use 'limit' to bound results.";
    return op;
});

// Ratings Minimal API (requires auth)
var ratingsV2 = apiV2.MapGroup("/ratings").RequireAuthorization().WithTags("RatingsV2");

ratingsV2.MapGet("/user", async (HttpContext ctx, int movieId, webxemphim.Services.RatingService ratingSvc) =>
{
    var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    var rating = await ratingSvc.GetUserRatingAsync(userId, movieId);
    return Results.Ok(new { rating = rating ?? 0 });
})
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status200OK)
.WithOpenApi(op => { op.Summary = "Get user's rating for a movie"; return op; });

ratingsV2.MapPost("/rate", async (HttpContext ctx, webxemphim.Services.RatingService ratingSvc, [Microsoft.AspNetCore.Mvc.FromBody] webxemphim.Models.Requests.RateRequest body) =>
{
    var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    var ok = await ratingSvc.RateMovieAsync(userId, body.MovieId, body.Score);
    if (!ok) return Results.BadRequest(new { success = false, message = "Không thể đánh giá." });
    var avg = await ratingSvc.GetMovieAverageRatingAsync(body.MovieId);
    var total = await ratingSvc.GetMovieTotalRatingsAsync(body.MovieId);
    return Results.Ok(new { success = true, averageRating = avg, totalRatings = total });
})
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status200OK)
.WithOpenApi(op => { op.Summary = "Rate a movie (1-5)"; return op; });

ratingsV2.MapDelete("/", async (HttpContext ctx, int movieId, webxemphim.Services.RatingService ratingSvc) =>
{
    var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    var ok = await ratingSvc.RemoveRatingAsync(userId, movieId);
    if (!ok) return Results.BadRequest(new { success = false });
    var avg = await ratingSvc.GetMovieAverageRatingAsync(movieId);
    var total = await ratingSvc.GetMovieTotalRatingsAsync(movieId);
    return Results.Ok(new { success = true, averageRating = avg, totalRatings = total });
})
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status200OK)
.WithOpenApi(op => { op.Summary = "Remove user's rating for a movie"; return op; });

// Movie Sources API
apiV2.MapGet("/movies/{id:int}/sources", async (int id, webxemphim.Data.ApplicationDbContext db) =>
{
    var sources = await db.MovieSources
        .AsNoTracking()
        .Where(s => s.MovieId == id)
        .OrderByDescending(s => s.IsDefault)
        .ThenBy(s => s.ServerName)
        .Select(s => new webxemphim.Models.DTOs.MovieSourceDto
        {
            Id = s.Id,
            MovieId = s.MovieId,
            ServerName = s.ServerName,
            Quality = s.Quality,
            Language = s.Language,
            Url = s.Url,
            IsDefault = s.IsDefault
        })
        .ToListAsync();
    return Results.Ok(sources);
})
.Produces<List<webxemphim.Models.DTOs.MovieSourceDto>>(StatusCodes.Status200OK)
.WithOpenApi(op =>
{
    op.Summary = "Get movie video sources";
    op.Description = "Returns all video sources for a movie by ID.";
    return op;
});

// Comments API
var commentsV2 = apiV2.MapGroup("/comments").WithTags("CommentsV2");

commentsV2.MapGet("/", async (int movieId, webxemphim.Data.ApplicationDbContext db) =>
{
    var comments = await db.Comments
        .AsNoTracking()
        .Include(c => c.User)
        .Include(c => c.Replies)
        .Where(c => c.MovieId == movieId && c.IsApproved && c.ParentCommentId == null)
        .OrderByDescending(c => c.CreatedAt)
        .Select(c => new webxemphim.Models.DTOs.CommentDto
        {
            Id = c.Id,
            Content = c.Content,
            UserName = c.User != null ? (c.User.Email ?? "Anonymous") : "Anonymous",
            UserAvatar = null,
            CreatedAt = c.CreatedAt,
            ParentCommentId = c.ParentCommentId,
            Replies = c.Replies.Where(r => r.IsApproved).OrderBy(r => r.CreatedAt).Select(r => new webxemphim.Models.DTOs.CommentDto
            {
                Id = r.Id,
                Content = r.Content,
                UserName = r.User != null ? (r.User.Email ?? "Anonymous") : "Anonymous",
                UserAvatar = null,
                CreatedAt = r.CreatedAt,
                ParentCommentId = r.ParentCommentId,
                LikeCount = r.Likes,
                DislikeCount = r.Dislikes,
                Replies = new List<webxemphim.Models.DTOs.CommentDto>()
            }).ToList(),
            LikeCount = c.Likes,
            DislikeCount = c.Dislikes
        })
        .ToListAsync();
    return Results.Ok(comments);
})
.Produces<List<webxemphim.Models.DTOs.CommentDto>>(StatusCodes.Status200OK)
.WithOpenApi(op => { op.Summary = "Get comments for a movie"; return op; });

commentsV2.MapPost("/", async (HttpContext ctx, webxemphim.Data.ApplicationDbContext db, Microsoft.AspNetCore.Identity.UserManager<webxemphim.Data.ApplicationUser> userManager, webxemphim.Services.AI.ModerationService moderation, [Microsoft.AspNetCore.Mvc.FromBody] webxemphim.Models.DTOs.CreateCommentDto body) =>
{
    var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(body.Content)) return Results.BadRequest(new { error = "Content is required" });

    var allowed = await moderation.IsAllowedAsync(body.Content, ctx.RequestAborted);
    var isApproved = allowed && moderation.IsConfigured;

    var comment = new webxemphim.Models.Comment
    {
        MovieId = body.MovieId,
        UserId = userId,
        Content = body.Content,
        IsApproved = isApproved,
        ParentCommentId = body.ParentCommentId
    };

    db.Comments.Add(comment);
    await db.SaveChangesAsync();

    return Results.Ok(new { success = true, message = isApproved ? "Bình luận đã được đăng." : "Bình luận đã gửi, chờ duyệt.", commentId = comment.Id });
})
.RequireAuthorization()
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status200OK)
.WithOpenApi(op => { op.Summary = "Create a new comment"; return op; });

commentsV2.MapPost("/{id:int}/like", async (HttpContext ctx, int id, webxemphim.Data.ApplicationDbContext db) =>
{
    var comment = await db.Comments.FindAsync(id);
    if (comment == null) return Results.NotFound(new { error = "Comment not found" });
    comment.Likes++;
    await db.SaveChangesAsync();
    return Results.Ok(new { likes = comment.Likes });
})
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status200OK)
.WithOpenApi(op => { op.Summary = "Like a comment"; return op; });

commentsV2.MapPost("/{id:int}/dislike", async (HttpContext ctx, int id, webxemphim.Data.ApplicationDbContext db) =>
{
    var comment = await db.Comments.FindAsync(id);
    if (comment == null) return Results.NotFound(new { error = "Comment not found" });
    comment.Dislikes++;
    await db.SaveChangesAsync();
    return Results.Ok(new { dislikes = comment.Dislikes });
})
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status200OK)
.WithOpenApi(op => { op.Summary = "Dislike a comment"; return op; });


// Ensure database is created and migrations are applied before seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<webxemphim.Data.ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // Apply pending migrations automatically
        db.Database.Migrate();
        
        // Kiểm tra và thêm các cột còn thiếu vào ViewHits (fix cho lỗi migration)
        try
        {
            var checkSql = @"
                SELECT 
                    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'WatchProgress') THEN 1 ELSE 0 END as HasWatchProgress,
                    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'Duration') THEN 1 ELSE 0 END as HasDuration,
                    CASE WHEN EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'EpisodeNumber') THEN 1 ELSE 0 END as HasEpisodeNumber
            ";
            
            var result = db.Database.SqlQueryRaw<MigrationCheckResult>(checkSql).FirstOrDefault();
            
            if (result != null)
            {
                var needsFix = result.HasWatchProgress == 0 || result.HasDuration == 0 || result.HasEpisodeNumber == 0;
                
                if (needsFix)
                {
                    logger.LogWarning("Phát hiện các cột còn thiếu trong ViewHits. Đang tự động thêm...");
                    
                    var fixSql = @"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'WatchProgress')
                        BEGIN
                            ALTER TABLE [dbo].[ViewHits] ADD [WatchProgress] [float] NULL;
                        END

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'Duration')
                        BEGIN
                            ALTER TABLE [dbo].[ViewHits] ADD [Duration] [float] NULL;
                        END

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ViewHits]') AND name = 'EpisodeNumber')
                        BEGIN
                            ALTER TABLE [dbo].[ViewHits] ADD [EpisodeNumber] [int] NULL;
                        END

                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ViewHits_UserId_MovieId_EpisodeNumber' AND object_id = OBJECT_ID(N'[dbo].[ViewHits]'))
                        BEGIN
                            CREATE INDEX [IX_ViewHits_UserId_MovieId_EpisodeNumber] 
                            ON [dbo].[ViewHits] ([UserId], [MovieId], [EpisodeNumber]);
                        END
                    ";
                    
                    db.Database.ExecuteSqlRaw(fixSql);
                    logger.LogInformation("Đã thêm các cột còn thiếu vào ViewHits thành công!");
                }
            }
        }
        catch (Exception fixEx)
        {
            logger.LogWarning(fixEx, "Không thể tự động fix ViewHits columns. Vui lòng chạy migration thủ công.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
    
    // Seed achievements
    try
    {
        var achievementService = scope.ServiceProvider.GetRequiredService<webxemphim.Services.AchievementService>();
        await achievementService.SeedDefaultAchievementsAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding achievements.");
    }
}
await webxemphim.Data.Seed.EnsureAdminAsync(app.Services);

// Temporary code to delete all movies - Đã chuyển sang API endpoint
// Để xóa phim, sử dụng:
// - UI: POST /Admin/Movies/DeleteAllMovies (cần đăng nhập Admin)
// - API: POST /api/admin/sync/delete-all-movies (cần API key hoặc Admin role)
// await DeleteAllMoviesAsync(app.Services);
// End of temporary code

app.Run();

// Helper class for migration check
internal class MigrationCheckResult
{
    public int HasWatchProgress { get; set; }
    public int HasDuration { get; set; }
    public int HasEpisodeNumber { get; set; }
}

// Marker for WebApplicationFactory in tests
public partial class Program { 
    static async Task DeleteAllMoviesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<webxemphim.Data.ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Attempting to delete all movies and related data from the database...");

        try
        {
            // Lấy danh sách tất cả Movie IDs trước
            var movieIds = await dbContext.Movies.Select(m => m.Id).ToListAsync();
            
            if (!movieIds.Any())
            {
                logger.LogInformation("No movies found to delete.");
                return;
            }

            logger.LogInformation("Found {Count} movies to delete. Starting deletion of related data...", movieIds.Count);

            // Xóa dữ liệu liên quan trước (các bảng không có cascade delete)
            // 1. Xóa MovieGenres (bảng trung gian)
            var movieGenres = await dbContext.MovieGenres.Where(mg => movieIds.Contains(mg.MovieId)).ToListAsync();
            if (movieGenres.Any())
            {
                dbContext.MovieGenres.RemoveRange(movieGenres);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Deleted {Count} movie-genre relationships.", movieGenres.Count);
            }

            // 2. Xóa ViewHits
            var viewHits = await dbContext.ViewHits.Where(vh => movieIds.Contains(vh.MovieId)).ToListAsync();
            if (viewHits.Any())
            {
                dbContext.ViewHits.RemoveRange(viewHits);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Deleted {Count} view hits.", viewHits.Count);
            }

            // 3. Xóa UserInventoryItems liên quan đến phim
            var userInventoryItems = await dbContext.UserInventoryItems.Where(ui => ui.MovieId.HasValue && movieIds.Contains(ui.MovieId.Value)).ToListAsync();
            if (userInventoryItems.Any())
            {
                dbContext.UserInventoryItems.RemoveRange(userInventoryItems);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Deleted {Count} user inventory items.", userInventoryItems.Count);
            }

            // 4. Xóa Notifications liên quan đến phim (có Restrict, cần xóa thủ công)
            var notifications = await dbContext.Notifications.Where(n => n.MovieId.HasValue && movieIds.Contains(n.MovieId.Value)).ToListAsync();
            if (notifications.Any())
            {
                dbContext.Notifications.RemoveRange(notifications);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Deleted {Count} notifications.", notifications.Count);
            }

            // Các bảng có cascade delete sẽ tự động xóa khi xóa Movies:
            // - Comments (Cascade)
            // - MovieSources (Cascade)
            // - Ratings (Cascade)
            // - WatchParties (Cascade) - sẽ tự động xóa WatchPartyParticipants và WatchPartyMessages
            // - Subtitles (Cascade)
            // - UserShares (Cascade)

            // Xóa tất cả Movies (các bảng có cascade sẽ tự động xóa)
            var allMovies = await dbContext.Movies.ToListAsync();
            dbContext.Movies.RemoveRange(allMovies);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Successfully deleted {Count} movies and all related data from the database.", allMovies.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deleting movies.");
            throw; // Re-throw để có thể thấy lỗi trong console
        }
    }
}
