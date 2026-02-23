using Domain.Entities;

namespace Application.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category?> GetByNameAsync(string name);
    }
}
