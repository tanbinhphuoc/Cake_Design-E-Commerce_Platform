using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Repositories
{
    public class CartItemRepository : GenericRepository<CartItem>, ICartItemRepository
    {
        public CartItemRepository(AppDbContext context) : base(context) { }
    }
}
