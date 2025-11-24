using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Grpc;

namespace webxemphim.Services.Grpc
{
    public class MoviesGrpcService : Movies.MoviesBase
    {
        private readonly ApplicationDbContext _db;
        public MoviesGrpcService(ApplicationDbContext db) { _db = db; }

        public override async Task<ListResponse> List(ListRequest request, ServerCallContext context)
        {
            int page = request.Page <= 0 ? 1 : request.Page;
            int pageSize = request.PageSize <= 0 || request.PageSize > 100 ? 20 : request.PageSize;

            var query = _db.Movies.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Genre))
            {
                query = query.Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                    .Where(m => m.MovieGenres.Any(mg => mg.Genre.Name == request.Genre || mg.Genre.Slug == request.Genre));
            }
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(m => m.Title.Contains(request.Search));
            }
            switch ((request.SortBy ?? "viewcount").ToLower())
            {
                case "rating": query = query.OrderByDescending(m => m.AverageRating); break;
                case "date": query = query.OrderByDescending(m => m.ReleaseDate ?? m.CreatedAt); break;
                default: query = query.OrderByDescending(m => m.ViewCount); break;
            }

            var total = await query.CountAsync();
            var movies = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var resp = new ListResponse { Page = page, PageSize = pageSize, TotalCount = total };
            resp.Data.AddRange(movies.Select(m => new MovieItem
            {
                Id = m.Id,
                Title = m.Title ?? string.Empty,
                Slug = m.Slug ?? string.Empty,
                PosterUrl = m.PosterUrl ?? string.Empty,
                Imdb = m.Imdb ?? 0,
                Year = m.Year ?? 0,
                AgeRating = m.AgeRating ?? string.Empty,
                IsSeries = m.IsSeries,
                AverageRating = m.AverageRating ?? 0
            }));
            return resp;
        }

        public override async Task<MovieItem> GetBySlug(GetBySlugRequest request, ServerCallContext context)
        {
            var m = await _db.Movies.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == request.Slug);
            if (m == null) throw new RpcException(new Status(StatusCode.NotFound, "Movie not found"));
            return new MovieItem
            {
                Id = m.Id,
                Title = m.Title ?? string.Empty,
                Slug = m.Slug ?? string.Empty,
                PosterUrl = m.PosterUrl ?? string.Empty,
                Imdb = m.Imdb ?? 0,
                Year = m.Year ?? 0,
                AgeRating = m.AgeRating ?? string.Empty,
                IsSeries = m.IsSeries,
                AverageRating = m.AverageRating ?? 0
            };
        }
    }
}


