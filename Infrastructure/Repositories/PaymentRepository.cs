using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context) : base(context) { }

        public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
        {
            return await _dbSet.FirstOrDefaultAsync(p => p.OrderId == orderId);
        }
    }
}
