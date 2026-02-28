using Domain.Entities;

namespace Application.Interfaces
{
    public interface IRefundRequestRepository : IGenericRepository<RefundRequest>
    {
        Task<RefundRequest?> GetByOrderIdAsync(Guid orderId);
        Task<List<RefundRequest>> GetPendingAsync();
        Task<RefundRequest?> GetByIdWithDetailsAsync(Guid id);
    }
}
