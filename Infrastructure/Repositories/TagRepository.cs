using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(AppDbContext context) : base(context) { }

        public async Task<Tag?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
        }
    }
}
