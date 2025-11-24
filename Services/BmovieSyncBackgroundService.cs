using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    public class BmovieSyncOptions
    {
        public bool Enabled { get; set; } = false;
        public int IntervalMinutes { get; set; } = 360; // 6h
        public int Pages { get; set; } = 2; // number of listing pages to crawl
    }

    public class BmovieSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<BmovieSyncBackgroundService> _logger;
        private readonly BmovieSyncOptions _options;

        public BmovieSyncBackgroundService(IServiceProvider sp, IConfiguration cfg, ILogger<BmovieSyncBackgroundService> logger)
        {
            _sp = sp;
            _logger = logger;
            _options = cfg.GetSection("BmovieSync").Get<BmovieSyncOptions>() ?? new BmovieSyncOptions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Bmovie sync disabled.");
                return;
            }

            _logger.LogInformation("Bmovie sync started. Interval {Interval} min, Pages {Pages}", _options.IntervalMinutes, _options.Pages);
            var delay = TimeSpan.FromMinutes(Math.Max(5, _options.IntervalMinutes));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncOnce(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Bmovie sync failed");
                }
                try { await Task.Delay(delay, stoppingToken); } catch { }
            }
        }

        public async Task SyncOnce(CancellationToken ct)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var scraper = scope.ServiceProvider.GetRequiredService<RophimScraperService>();

            int imported = 0, skipped = 0, sourcesAdded = 0;
            var pages = Math.Max(1, _options.Pages);

            for (int p = 1; p <= pages && !ct.IsCancellationRequested; p++)
            {
                var list = await scraper.ListMovies(p);
                foreach (var item in list)
                {
                    if (ct.IsCancellationRequested) break;
                    if (string.IsNullOrWhiteSpace(item.Slug)) continue;

                    var existing = await db.Movies.FirstOrDefaultAsync(m => m.Slug == item.Slug, ct);
                    if (existing == null)
                    {
                        var details = await scraper.ScrapeMovie(item.Slug);
                        if (details == null) { skipped++; continue; }

                        var movie = new Movie
                        {
                            Title = details.Title,
                            Slug = details.Slug,
                            PosterUrl = details.PosterUrl,
                            Description = details.Description,
                            Year = details.Year,
                            IsSeries = true
                        };
                        db.Movies.Add(movie);
                        await db.SaveChangesAsync(ct);

                        if (details.Sources.Count > 0)
                        {
                            foreach (var s in details.Sources)
                            {
                                db.MovieSources.Add(new MovieSource
                                {
                                    MovieId = movie.Id,
                                    ServerName = s.ServerName,
                                    Url = s.Url,
                                    Quality = "1080p",
                                    Language = "Vietsub",
                                    IsDefault = s.IsDefault
                                });
                            }
                            sourcesAdded += details.Sources.Count;
                            await db.SaveChangesAsync(ct);
                        }
                        imported++;
                    }
                    else
                    {
                        // try to add sources if movie exists but has none
                        var hasSources = await db.MovieSources.AnyAsync(ms => ms.MovieId == existing.Id, ct);
                        if (!hasSources)
                        {
                            var details = await scraper.ScrapeMovie(item.Slug);
                            if (details?.Sources != null && details.Sources.Count > 0)
                            {
                                foreach (var s in details.Sources)
                                {
                                    db.MovieSources.Add(new MovieSource
                                    {
                                        MovieId = existing.Id,
                                        ServerName = s.ServerName,
                                        Url = s.Url,
                                        Quality = "1080p",
                                        Language = "Vietsub",
                                        IsDefault = s.IsDefault
                                    });
                                }
                                sourcesAdded += details.Sources.Count;
                                await db.SaveChangesAsync(ct);
                            }
                        }
                        skipped++;
                    }
                }
            }

            _logger.LogInformation("Bmovie sync done. Imported {Imported}, Skipped {Skipped}, SourcesAdded {Sources}", imported, skipped, sourcesAdded);
        }
    }
}


