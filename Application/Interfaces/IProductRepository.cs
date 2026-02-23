using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetByIdWithDetailsAsync(Guid id);
        Task<Product?> GetByIdWithTagsAsync(Guid id);
        Task<List<Product>> GetAllActiveWithDetailsAsync();
        Task<(List<Product> Items, int TotalCount)> SearchActiveProductsAsync(ProductSearchDto search);
    }
}
