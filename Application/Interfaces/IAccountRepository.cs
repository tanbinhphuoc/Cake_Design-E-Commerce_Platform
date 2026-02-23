using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAccountRepository : IGenericRepository<Account>
    {
        Task<Account?> GetByUsernameAsync(string username);
        Task<Account?> GetByIdWithShopAsync(Guid id);
    }
}
