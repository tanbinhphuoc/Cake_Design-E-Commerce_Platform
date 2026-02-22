using Application.DTOs;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    public class CategoryTagController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryTagController(AppDbContext context)
        {
            _context = context;
        }

        // ===== Categories (Public) =====

        /// <summary>
        /// Get all categories (public).
        /// </summary>
        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(categories);
        }

        // ===== Categories (Admin) =====

        /// <summary>
        /// Create a category (Admin only).
        /// </summary>
        [HttpPost("categories")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { Message = "Category name is required." });

            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower());
            if (existing != null)
                return BadRequest(new { Message = "Category with this name already exists." });

            var category = new Domain.Entities.Category
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                ImageUrl = dto.ImageUrl ?? string.Empty,
                SortOrder = dto.SortOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Category created.", CategoryId = category.Id });
        }

        /// <summary>
        /// Update a category (Admin only).
        /// </summary>
        [HttpPut("categories/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { Message = "Category not found." });

            if (dto.Name != null) category.Name = dto.Name;
            if (dto.Description != null) category.Description = dto.Description;
            if (dto.ImageUrl != null) category.ImageUrl = dto.ImageUrl;
            if (dto.SortOrder.HasValue) category.SortOrder = dto.SortOrder.Value;
            if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Category updated." });
        }

        /// <summary>
        /// Delete a category (Admin only).
        /// </summary>
        [HttpDelete("categories/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { Message = "Category not found." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Category deleted." });
        }

        // ===== Tags (Public) =====

        /// <summary>
        /// Get all tags (public).
        /// </summary>
        [HttpGet("tags")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTags()
        {
            var tags = await _context.Tags
                .OrderBy(t => t.Name)
                .Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();

            return Ok(tags);
        }

        // ===== Tags (Admin) =====

        /// <summary>
        /// Create a tag (Admin only).
        /// </summary>
        [HttpPost("tags")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { Message = "Tag name is required." });

            var existing = await _context.Tags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == dto.Name.ToLower());
            if (existing != null)
                return BadRequest(new { Message = "Tag with this name already exists." });

            var tag = new Domain.Entities.Tag
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tag created.", TagId = tag.Id });
        }

        /// <summary>
        /// Update a tag (Admin only).
        /// </summary>
        [HttpPut("tags/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTag(Guid id, [FromBody] CreateTagDto dto)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
                return NotFound(new { Message = "Tag not found." });

            if (!string.IsNullOrWhiteSpace(dto.Name))
                tag.Name = dto.Name;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tag updated." });
        }

        /// <summary>
        /// Delete a tag (Admin only).
        /// </summary>
        [HttpDelete("tags/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTag(Guid id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
                return NotFound(new { Message = "Tag not found." });

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tag deleted." });
        }
    }
}
