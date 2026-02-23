using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        public AccountRepository(AppDbContext context) : base(context) { }

        public async Task<Account?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Username == username);
        }

        public async Task<Account?> GetByIdWithShopAsync(Guid id)
        {
            return await _dbSet
                .Include(a => a.Shop)
                .FirstOrDefaultAsync(a => a.Id == id);
        }
    }
}
