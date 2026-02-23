using Application.DTOs;

namespace Application.Services
{
    public interface IProductService
    {
        Task<List<ProductDetailDto>> GetAllProductsAsync();
        Task<ProductDetailDto?> GetProductByIdAsync(Guid id);
        Task<PaginatedResultDto<ProductDetailDto>> SearchProductsAsync(ProductSearchDto search);
        Task<List<ReviewDto>> GetReviewsAsync(Guid productId);
        Task<Guid> CreateReviewAsync(Guid userId, Guid productId, CreateReviewDto dto);
    }
}
