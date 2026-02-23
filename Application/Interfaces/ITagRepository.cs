using Domain.Entities;

namespace Application.Interfaces
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<Tag?> GetByNameAsync(string name);
    }
}
