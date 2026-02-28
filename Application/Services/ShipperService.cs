using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class ShipperService : IShipperService
    {
        private readonly IUnitOfWork _uow;

        public ShipperService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<ShipperOrderDto>> GetAvailableOrdersAsync()
        {
            var orders = await _uow.Orders.FindAsync(o => o.Status == "ReadyForPickup" && o.ShipperId == null);
            var result = new List<ShipperOrderDto>();
            foreach (var o in orders.OrderBy(o => o.CreatedAt))
            {
                var full = await _uow.Orders.GetByIdWithDetailsAsync(o.Id);
                if (full != null) result.Add(MapShipperOrder(full));
            }
            return result;
        }

        public async Task<List<ShipperOrderDto>> GetMyOrdersAsync(Guid shipperId)
        {
            var orders = await _uow.Orders.FindAsync(o => o.ShipperId == shipperId && (o.Status == "Shipping" || o.Status == "Delivered"));
            var result = new List<ShipperOrderDto>();
            foreach (var o in orders.OrderByDescending(o => o.UpdatedAt))
            {
                var full = await _uow.Orders.GetByIdWithDetailsAsync(o.Id);
                if (full != null) result.Add(MapShipperOrder(full));
            }
            return result;
        }

        public async Task<string> PickupOrderAsync(Guid shipperId, Guid orderId)
        {
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId);
            if (order == null) throw new ArgumentException("Order not found.");
            if (order.Status != "ReadyForPickup") throw new InvalidOperationException("Order is not ready for pickup.");
            if (order.ShipperId != null) throw new InvalidOperationException("Order has already been picked up by another shipper.");

            order.ShipperId = shipperId;
            order.Status = "Shipping";
            order.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return "Order picked up successfully. Start delivering!";
        }

        public async Task<string> DeliverOrderAsync(Guid shipperId, Guid orderId)
        {
            var order = await _uow.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.ShipperId == shipperId);
            if (order == null) throw new ArgumentException("Order not found or not assigned to you.");
            if (order.Status != "Shipping") throw new InvalidOperationException("Order is not in shipping status.");

            order.Status = "Delivered";
            order.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return "Order marked as delivered. Waiting for customer confirmation.";
        }

        private static ShipperOrderDto MapShipperOrder(Order o) => new()
        {
            Id = o.Id,
            ShopId = o.ShopId,
            ShopName = o.Shop.ShopName,
            CustomerName = o.User.FullName != "" ? o.User.FullName : o.User.Username,
            TotalAmount = o.TotalAmount,
            ShippingFee = o.ShippingFee,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            ShippingAddress = o.ShippingAddress == null ? null : new AddressDto
            {
                Id = o.ShippingAddress.Id, ReceiverName = o.ShippingAddress.ReceiverName,
                Phone = o.ShippingAddress.Phone, Street = o.ShippingAddress.Street,
                Ward = o.ShippingAddress.Ward, District = o.ShippingAddress.District,
                City = o.ShippingAddress.City, IsDefault = o.ShippingAddress.IsDefault
            },
            Items = o.Items.Select(oi => new OrderItemDto
            {
                ProductId = oi.ProductId, ProductName = oi.Product.Name,
                ProductImageUrl = oi.Product.ImageUrl, Quantity = oi.Quantity,
                PriceAtPurchase = oi.PriceAtPurchase
            }).ToList(),
            CreatedAt = o.CreatedAt
        };
    }
}
