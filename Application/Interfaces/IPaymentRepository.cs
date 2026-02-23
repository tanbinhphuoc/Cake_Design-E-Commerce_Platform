using Domain.Entities;

namespace Application.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetByOrderIdAsync(Guid orderId);
    }
}
