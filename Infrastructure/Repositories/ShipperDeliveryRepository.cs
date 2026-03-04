using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Repositories
{
    public class ShipperDeliveryRepository : GenericRepository<ShipperDelivery>, IShipperDeliveryRepository
    {
        public ShipperDeliveryRepository(AppDbContext context) : base(context) { }
    }
}
