using Application.DTOs;

namespace Application.Services
{
    public interface IShipperService
    {
        Task<List<ShipperOrderDto>> GetAvailableOrdersAsync();
        Task<List<ShipperOrderDto>> GetMyOrdersAsync(Guid shipperId);
        Task<string> PickupOrderAsync(Guid shipperId, Guid orderId);
        Task<string> DeliverOrderAsync(Guid shipperId, Guid orderId);
    }
}
