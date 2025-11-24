using Microsoft.AspNetCore.Components;
using webxemphim.Models;

namespace webxemphim.Services
{
    /// <summary>
    /// Service for managing movie state (watchlist, favorites, ratings) in Blazor components
    /// </summary>
    public class MovieStateService
    {
        private readonly List<int> _watchlist = new();
        private readonly Dictionary<int, double> _userRatings = new();
        private readonly List<int> _favorites = new();

        public event Action? OnStateChanged;

        public List<int> GetWatchlist() => new(_watchlist);
        public Dictionary<int, double> GetUserRatings() => new(_userRatings);
        public List<int> GetFavorites() => new(_favorites);

        public bool IsInWatchlist(int movieId) => _watchlist.Contains(movieId);
        public bool IsFavorite(int movieId) => _favorites.Contains(movieId);
        public double? GetRating(int movieId) => _userRatings.TryGetValue(movieId, out var rating) ? rating : null;

        public async Task AddToWatchlistAsync(int movieId)
        {
            if (!_watchlist.Contains(movieId))
            {
                _watchlist.Add(movieId);
                NotifyStateChanged();
            }
        }

        public async Task RemoveFromWatchlistAsync(int movieId)
        {
            if (_watchlist.Remove(movieId))
            {
                NotifyStateChanged();
            }
        }

        public async Task ToggleWatchlistAsync(int movieId)
        {
            if (_watchlist.Contains(movieId))
                await RemoveFromWatchlistAsync(movieId);
            else
                await AddToWatchlistAsync(movieId);
        }

        public async Task AddToFavoritesAsync(int movieId)
        {
            if (!_favorites.Contains(movieId))
            {
                _favorites.Add(movieId);
                NotifyStateChanged();
            }
        }

        public async Task RemoveFromFavoritesAsync(int movieId)
        {
            if (_favorites.Remove(movieId))
            {
                NotifyStateChanged();
            }
        }

        public async Task ToggleFavoriteAsync(int movieId)
        {
            if (_favorites.Contains(movieId))
                await RemoveFromFavoritesAsync(movieId);
            else
                await AddToFavoritesAsync(movieId);
        }

        public async Task RateMovieAsync(int movieId, double rating)
        {
            _userRatings[movieId] = rating;
            NotifyStateChanged();
        }

        public int GetWatchlistCount() => _watchlist.Count;
        public int GetFavoritesCount() => _favorites.Count;

        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}

