using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAddressRepository : IGenericRepository<Address>
    {
        Task<List<Address>> GetByUserIdAsync(Guid userId);
        Task<List<Address>> GetDefaultAddressesByUserIdAsync(Guid userId);
    }
}
