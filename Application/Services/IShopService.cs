using Application.DTOs;

namespace Application.Services
{
    public interface IShopService
    {
        Task<object> RequestShopAsync(Guid userId, UpdateShopDto dto);
        Task<string> ApproveShopAsync(Guid userId);
        Task<ShopProfileDto?> GetMyShopAsync(Guid userId);
        Task<string> UpdateMyShopAsync(Guid userId, UpdateShopDto dto);
        Task<ShopPublicDto?> GetShopByIdAsync(Guid shopId);

        // Staff
        Task<Guid> AddStaffAsync(Guid ownerId, AddStaffDto dto);
        Task<List<StaffDto>> GetStaffAsync(Guid ownerId);
        Task<string> RemoveStaffAsync(Guid ownerId, Guid staffId);

        // Products
        Task<List<ProductDetailDto>> GetMyProductsAsync(Guid userId);
        Task<object> CreateProductAsync(Guid userId, CreateProductExtendedDto dto);
        Task<string> UpdateProductAsync(Guid userId, Guid productId, UpdateProductDto dto);
        Task<string> DeleteProductAsync(Guid userId, Guid productId);
        Task<List<ProductDetailDto>> GetProductsByShopAsync(Guid shopId);

        // Helper
        Task<Guid?> GetShopIdForUserAsync(Guid userId);
    }
}
