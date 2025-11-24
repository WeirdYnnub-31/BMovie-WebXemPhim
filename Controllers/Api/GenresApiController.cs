using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Models.DTOs;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/v2/genres")]
    [Produces("application/json")]
    public class GenresApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public GenresApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Get all genres
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<GenreDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<GenreDto>>> GetGenres()
        {
            try
            {
                var genres = await _db.Genres
                    .AsNoTracking()
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                var result = genres.Select(g => new GenreDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Slug = g.Slug,
                    MovieCount = g.MovieGenres.Count
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        /// <summary>
        /// Get a genre by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GenreDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GenreDto>> GetGenre(int id)
        {
            try
            {
                var genre = await _db.Genres
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (genre == null)
                    return NotFound(new { error = "Genre not found" });

                var result = new GenreDto
                {
                    Id = genre.Id,
                    Name = genre.Name,
                    Slug = genre.Slug,
                    MovieCount = genre.MovieGenres.Count
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        /// <summary>
        /// Create a new genre (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(GenreDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<GenreDto>> CreateGenre([FromBody] CreateGenreDto dto)
        {
            try
            {
                // Check if genre already exists
                var exists = await _db.Genres.AnyAsync(g => g.Name == dto.Name);
                if (exists)
                    return BadRequest(new { error = "Genre already exists" });

                var genre = new Genre
                {
                    Name = dto.Name,
                    Slug = dto.Slug ?? dto.Name.ToLower().Replace(" ", "-")
                };

                _db.Genres.Add(genre);
                await _db.SaveChangesAsync();

                var result = new GenreDto
                {
                    Id = genre.Id,
                    Name = genre.Name,
                    Slug = genre.Slug,
                    MovieCount = 0
                };

                return CreatedAtAction(nameof(GetGenre), new { id = genre.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        /// <summary>
        /// Update a genre (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(GenreDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<GenreDto>> UpdateGenre(int id, [FromBody] UpdateGenreDto dto)
        {
            try
            {
                var genre = await _db.Genres.FindAsync(id);
                if (genre == null)
                    return NotFound(new { error = "Genre not found" });

                genre.Name = dto.Name;
                genre.Slug = dto.Slug ?? genre.Slug;

                await _db.SaveChangesAsync();

                var result = new GenreDto
                {
                    Id = genre.Id,
                    Name = genre.Name,
                    Slug = genre.Slug,
                    MovieCount = genre.MovieGenres.Count
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a genre (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            try
            {
                var genre = await _db.Genres.FindAsync(id);
                if (genre == null)
                    return NotFound(new { error = "Genre not found" });

                _db.Genres.Remove(genre);
                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }
    }
}

