using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context) { }

        public async Task<Product?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product?> GetByIdWithTagsAsync(Guid id)
        {
            return await _dbSet
                .Include(p => p.ProductTags)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Product>> GetAllActiveWithDetailsAsync()
        {
            return await _dbSet
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<(List<Product> Items, int TotalCount)> SearchActiveProductsAsync(ProductSearchDto search)
        {
            var query = _dbSet
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                var kw = search.Keyword.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(kw) || p.Description.ToLower().Contains(kw));
            }
            if (search.CategoryId.HasValue) query = query.Where(p => p.CategoryId == search.CategoryId);
            if (search.ShopId.HasValue) query = query.Where(p => p.ShopId == search.ShopId);
            if (search.TagId.HasValue) query = query.Where(p => p.ProductTags.Any(pt => pt.TagId == search.TagId));
            if (search.MinPrice.HasValue) query = query.Where(p => p.Price >= search.MinPrice);
            if (search.MaxPrice.HasValue) query = query.Where(p => p.Price <= search.MaxPrice);

            query = search.Sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.AverageRating),
                "popular" => query.OrderByDescending(p => p.ReviewCount),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var page = Math.Max(1, search.Page);
            var pageSize = Math.Clamp(search.PageSize, 1, 100);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
