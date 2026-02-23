using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _uow;
        public CartService(IUnitOfWork uow) { _uow = uow; }

        public async Task<object> GetCartAsync(Guid userId)
        {
            var cart = await _uow.Carts.GetCartWithItemsAndProductsAsync(userId);
            if (cart == null)
                return new { Items = new List<object>(), TotalPrice = 0m };

            return new
            {
                CartId = cart.Id,
                Items = cart.Items.Select(ci => new
                {
                    ci.Id, ci.ProductId, ProductName = ci.Product.Name,
                    ProductPrice = ci.Product.Price, ProductImageUrl = ci.Product.ImageUrl,
                    ShopId = ci.Product.ShopId, ShopName = ci.Product.Shop.ShopName,
                    ci.Quantity, Subtotal = ci.Product.Price * ci.Quantity
                }),
                TotalPrice = cart.Items.Sum(ci => ci.Product.Price * ci.Quantity)
            };
        }

        public async Task<string> AddToCartAsync(Guid userId, AddToCartDto dto)
        {
            var product = await _uow.Products.GetByIdAsync(dto.ProductId);
            if (product == null) throw new ArgumentException("Product not found.");
            if (!product.IsActive) throw new ArgumentException("Product is not available.");
            if (dto.Quantity <= 0) throw new ArgumentException("Quantity must be greater than 0.");
            if (product.Stock < dto.Quantity) throw new ArgumentException($"Insufficient stock. Available: {product.Stock}");

            var cart = await _uow.Carts.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                cart = new Cart { Id = Guid.NewGuid(), UserId = userId };
                await _uow.Carts.AddAsync(cart);
                await _uow.SaveChangesAsync();
            }

            var existingItem = cart.Items.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
            if (existingItem != null)
                existingItem.Quantity += dto.Quantity;
            else
                await _uow.CartItems.AddAsync(new CartItem { Id = Guid.NewGuid(), CartId = cart.Id, ProductId = dto.ProductId, Quantity = dto.Quantity });

            await _uow.SaveChangesAsync();
            return "Product added to cart successfully.";
        }

        public async Task<string> UpdateCartItemAsync(Guid userId, UpdateCartItemDto dto)
        {
            var cart = await _uow.Carts.GetCartByUserIdAsync(userId);
            if (cart == null) throw new ArgumentException("Cart not found.");
            var cartItem = cart.Items.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
            if (cartItem == null) throw new ArgumentException("Product not in cart.");

            if (dto.Quantity <= 0) { _uow.CartItems.Remove(cartItem); }
            else
            {
                var product = await _uow.Products.GetByIdAsync(dto.ProductId);
                if (product != null && product.Stock < dto.Quantity)
                    throw new ArgumentException($"Insufficient stock. Available: {product.Stock}");
                cartItem.Quantity = dto.Quantity;
            }
            await _uow.SaveChangesAsync();
            return "Cart item updated.";
        }

        public async Task<string> RemoveCartItemAsync(Guid userId, Guid productId)
        {
            var cart = await _uow.Carts.GetCartByUserIdAsync(userId);
            if (cart == null) throw new ArgumentException("Cart not found.");
            var cartItem = cart.Items.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem == null) throw new ArgumentException("Product not in cart.");
            _uow.CartItems.Remove(cartItem);
            await _uow.SaveChangesAsync();
            return "Item removed from cart.";
        }

        public async Task<string> ClearCartAsync(Guid userId)
        {
            var cart = await _uow.Carts.GetCartByUserIdAsync(userId);
            if (cart == null) throw new ArgumentException("Cart not found.");
            _uow.CartItems.RemoveRange(cart.Items);
            await _uow.SaveChangesAsync();
            return "Cart cleared.";
        }
    }
}
