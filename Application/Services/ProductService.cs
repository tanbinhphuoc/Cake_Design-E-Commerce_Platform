using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        public ProductService(IUnitOfWork uow) { _uow = uow; }

        public async Task<List<ProductDetailDto>> GetAllProductsAsync()
        {
            var products = await _uow.Products.GetAllActiveWithDetailsAsync();
            return products.Select(p => MapProductDto(p)).ToList();
        }

        public async Task<ProductDetailDto?> GetProductByIdAsync(Guid id)
        {
            var p = await _uow.Products.GetByIdWithDetailsAsync(id);
            if (p == null) return null;
            return MapProductDto(p);
        }

        public async Task<PaginatedResultDto<ProductDetailDto>> SearchProductsAsync(ProductSearchDto search)
        {
            var (items, totalCount) = await _uow.Products.SearchActiveProductsAsync(search);
            var page = Math.Max(1, search.Page);
            var pageSize = Math.Clamp(search.PageSize, 1, 100);
            return new PaginatedResultDto<ProductDetailDto>
            {
                Items = items.Select(p => MapProductDto(p)).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<ReviewDto>> GetReviewsAsync(Guid productId)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null) throw new ArgumentException("Product not found.");
            var reviews = await _uow.Reviews.GetReviewsByProductIdAsync(productId);
            return reviews.Select(r => new ReviewDto
            {
                Id = r.Id, UserId = r.UserId, Username = r.User.Username, AvatarUrl = r.User.AvatarUrl,
                Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<Guid> CreateReviewAsync(Guid userId, Guid productId, CreateReviewDto dto)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null) throw new ArgumentException("Product not found.");
            var existing = await _uow.Reviews.GetUserReviewForProductAsync(userId, productId);
            if (existing != null) throw new InvalidOperationException("You have already reviewed this product.");
            var hasPurchased = await _uow.Orders.HasUserPurchasedProductAsync(userId, productId);
            if (!hasPurchased) throw new InvalidOperationException("You can only review products you have purchased and received.");
            if (dto.Rating < 1 || dto.Rating > 5) throw new ArgumentException("Rating must be between 1 and 5.");

            var review = new Review
            {
                Id = Guid.NewGuid(), ProductId = productId, UserId = userId, Rating = dto.Rating,
                Comment = dto.Comment ?? string.Empty, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            await _uow.Reviews.AddAsync(review);

            var allReviews = await _uow.Reviews.GetReviewsByProductIdAsync(productId);
            var ratings = allReviews.Select(r => r.Rating).ToList();
            ratings.Add(dto.Rating);
            product.AverageRating = ratings.Average();
            product.ReviewCount = ratings.Count;
            product.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return review.Id;
        }

        private static ProductDetailDto MapProductDto(Product p) => new()
        {
            Id = p.Id, ShopId = p.ShopId, ShopName = p.Shop.ShopName, CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name, Name = p.Name, Price = p.Price,
            Description = p.Description, ImageUrl = p.ImageUrl, Stock = p.Stock, IsActive = p.IsActive,
            AverageRating = p.AverageRating, ReviewCount = p.ReviewCount,
            Tags = p.ProductTags.Select(pt => pt.Tag.Name).ToList(), CreatedAt = p.CreatedAt
        };
    }
}
