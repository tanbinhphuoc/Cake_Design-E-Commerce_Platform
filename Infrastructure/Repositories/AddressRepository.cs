using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AddressRepository : GenericRepository<Address>, IAddressRepository
    {
        public AddressRepository(AppDbContext context) : base(context) { }

        public async Task<List<Address>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Address>> GetDefaultAddressesByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();
        }
    }
}
