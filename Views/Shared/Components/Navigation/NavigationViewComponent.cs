using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using webxemphim.Data;
using webxemphim.Views.Shared.Components.Navigation;

namespace webxemphim.Views.Shared.Components.Navigation
{
    public class NavigationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public NavigationViewComponent(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Cache genres and countries for 10 minutes to improve performance
            const string genresCacheKey = "Navigation_Genres";
            const string countriesCacheKey = "Navigation_Countries";

            var genres = await _cache.GetOrCreateAsync(genresCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await _db.Genres
                    .AsNoTracking()
                    .OrderBy(g => g.Name)
                    .ToListAsync();
            });

            var uniqueCountries = await _cache.GetOrCreateAsync(countriesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                var countries = await _db.Movies
                    .AsNoTracking()
                    .Where(m => !string.IsNullOrWhiteSpace(m.Country))
                    .Select(m => m.Country!)
                    .Distinct()
                    .ToListAsync();

                // Split comma-separated countries and get unique values
                return countries
                    .SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            });

            var model = new NavigationViewModel
            {
                Genres = genres ?? new List<webxemphim.Models.Genre>(),
                Countries = uniqueCountries ?? new List<string>()
            };

            return View(model);
        }
    }
}

