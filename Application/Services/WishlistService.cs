using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IUnitOfWork _uow;
        public WishlistService(IUnitOfWork uow) { _uow = uow; }

        public async Task<List<WishlistItemDto>> GetWishlistAsync(Guid userId)
        {
            var items = await _uow.Wishlists.GetByUserIdAsync(userId);
            return items.Select(w => new WishlistItemDto
            {
                ProductId = w.ProductId, ProductName = w.Product.Name,
                Price = w.Product.Price, ImageUrl = w.Product.ImageUrl, AddedAt = w.CreatedAt
            }).ToList();
        }

        public async Task<string> AddToWishlistAsync(Guid userId, Guid productId)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null) throw new ArgumentException("Product not found.");
            var existing = await _uow.Wishlists.GetByUserAndProductAsync(userId, productId);
            if (existing != null) throw new InvalidOperationException("Product already in wishlist.");

            await _uow.Wishlists.AddAsync(new WishlistItem
            { Id = Guid.NewGuid(), UserId = userId, ProductId = productId, CreatedAt = DateTime.UtcNow });
            await _uow.SaveChangesAsync();
            return "Product added to wishlist.";
        }

        public async Task<string> RemoveFromWishlistAsync(Guid userId, Guid productId)
        {
            var item = await _uow.Wishlists.GetByUserAndProductAsync(userId, productId);
            if (item == null) throw new ArgumentException("Product not in wishlist.");
            _uow.Wishlists.Remove(item);
            await _uow.SaveChangesAsync();
            return "Product removed from wishlist.";
        }
    }
}
