using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    public class CategoryTagController : ControllerBase
    {
        private readonly ICategoryTagService _service;
        public CategoryTagController(ICategoryTagService service) { _service = service; }

        [HttpGet("categories"), AllowAnonymous]
        public async Task<IActionResult> GetCategories() => Ok(await _service.GetCategoriesAsync());

        [HttpPost("categories"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            try { var id = await _service.CreateCategoryAsync(dto); return Ok(new { Message = "Category created.", CategoryId = id }); }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpPut("categories/{id:guid}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
        {
            try { return Ok(new { Message = await _service.UpdateCategoryAsync(id, dto) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpDelete("categories/{id:guid}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try { return Ok(new { Message = await _service.DeleteCategoryAsync(id) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpGet("tags"), AllowAnonymous]
        public async Task<IActionResult> GetTags() => Ok(await _service.GetTagsAsync());

        [HttpPost("tags"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagDto dto)
        {
            try { var id = await _service.CreateTagAsync(dto); return Ok(new { Message = "Tag created.", TagId = id }); }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpPut("tags/{id:guid}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTag(Guid id, [FromBody] CreateTagDto dto)
        {
            try { return Ok(new { Message = await _service.UpdateTagAsync(id, dto) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpDelete("tags/{id:guid}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTag(Guid id)
        {
            try { return Ok(new { Message = await _service.DeleteTagAsync(id) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }
    }
}
