using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RefundRequestRepository : GenericRepository<RefundRequest>, IRefundRequestRepository
    {
        public RefundRequestRepository(AppDbContext context) : base(context) { }

        public async Task<RefundRequest?> GetByOrderIdAsync(Guid orderId)
        {
            return await _dbSet
                .Include(r => r.Customer)
                .Include(r => r.Order).ThenInclude(o => o.Shop)
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
        }

        public async Task<List<RefundRequest>> GetPendingAsync()
        {
            return await _dbSet
                .Where(r => r.Status == "Pending")
                .Include(r => r.Customer)
                .Include(r => r.Order).ThenInclude(o => o.Shop)
                .Include(r => r.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<RefundRequest?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(r => r.Customer)
                .Include(r => r.Order).ThenInclude(o => o.Shop)
                .Include(r => r.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
    }
}
