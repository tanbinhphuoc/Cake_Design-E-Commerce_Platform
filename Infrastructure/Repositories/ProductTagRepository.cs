using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Repositories
{
    public class ProductTagRepository : GenericRepository<ProductTag>, IProductTagRepository
    {
        public ProductTagRepository(AppDbContext context) : base(context) { }
    }
}
