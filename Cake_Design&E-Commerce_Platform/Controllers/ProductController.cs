using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService) { _productService = productService; }

        [HttpGet("products"), AllowAnonymous]
        public async Task<IActionResult> GetAllProducts() => Ok(await _productService.GetAllProductsAsync());

        [HttpGet("products/{id:guid}"), AllowAnonymous]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            return product != null ? Ok(product) : NotFound(new { Message = "Product not found." });
        }

        [HttpGet("products/search"), AllowAnonymous]
        public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchDto search)
            => Ok(await _productService.SearchProductsAsync(search));

        [HttpGet("products/{id:guid}/reviews"), AllowAnonymous]
        public async Task<IActionResult> GetReviews(Guid id)
        {
            try { return Ok(await _productService.GetReviewsAsync(id)); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpPost("products/{id:guid}/reviews"), Authorize]
        public async Task<IActionResult> CreateReview(Guid id, [FromBody] CreateReviewDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { var reviewId = await _productService.CreateReviewAsync(userId.Value, id, dto); return Ok(new { Message = "Review created.", ReviewId = reviewId }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
