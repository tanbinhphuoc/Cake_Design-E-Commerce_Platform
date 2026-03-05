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
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId);
            if (order == null || order.ShipperId != shipperId)
                throw new ArgumentException("Order not found or not assigned to you.");
            if (order.Status != "Shipping")
                throw new InvalidOperationException("Order is not in shipping status.");

            order.Status = "Delivered";
            order.UpdatedAt = DateTime.UtcNow;

            // === SHIPPER PAYMENT + ESCROW RELEASE ===
            if (order.PaymentStatus == "Paid")
            {
                var shipperAccount = await _uow.Accounts.GetByIdAsync(shipperId);
                var shop = await _uow.Shops.GetByIdAsync(order.ShopId);

                if (shipperAccount != null && shop != null)
                {
                    // 1. Shipper gets 50% of ShippingFee
                    var shipperPayout = Math.Round(order.ShippingFee * 0.50m, 0);
                    shipperAccount.WalletBalance += shipperPayout;
                    await _uow.WalletTransactions.AddAsync(new WalletTransaction
                    {
                        Id = Guid.NewGuid(), WalletOwnerId = shipperId, WalletType = "User",
                        Amount = shipperPayout, TransactionType = "ShippingEarning",
                        Description = $"Delivery earning for order {order.Id} (50% of {order.ShippingFee:N0}₫ shipping fee)",
                        BalanceAfter = shipperAccount.WalletBalance, ReferenceId = order.Id,
                        CreatedAt = DateTime.UtcNow
                    });

                    // 2. Release escrow
                    var escrow = await GetOrCreateSystemWallet("Escrow");
                    escrow.Balance -= order.TotalAmount;
                    await _uow.SystemWalletTransactions.AddAsync(new SystemWalletTransaction
                    {
                        Id = Guid.NewGuid(), WalletType = "Escrow", Amount = -order.TotalAmount,
                        TransactionType = "EscrowRelease",
                        Description = $"Released for delivered order {order.Id}",
                        BalanceAfter = escrow.Balance, OrderId = order.Id, RelatedUserId = shop.OwnerId,
                        CreatedAt = DateTime.UtcNow
                    });

                    // 3. Commission on product amount (TotalAmount - ShippingFee)
                    var productAmount = order.TotalAmount - order.ShippingFee;
                    var commission = Math.Round(productAmount * shop.CommissionRate / 100m, 0);
                    var systemShippingCut = order.ShippingFee - shipperPayout; // 50% shipping → system

                    // 4. Shop gets: TotalAmount - shipperPayout - systemShippingCut - commission
                    var shopPayout = order.TotalAmount - shipperPayout - systemShippingCut - commission;
                    var shopOwner = await _uow.Accounts.GetByIdAsync(shop.OwnerId);
                    if (shopOwner != null)
                    {
                        shopOwner.WalletBalance += shopPayout;
                        await _uow.WalletTransactions.AddAsync(new WalletTransaction
                        {
                            Id = Guid.NewGuid(), WalletOwnerId = shopOwner.Id, WalletType = "User",
                            Amount = shopPayout, TransactionType = "Sale",
                            Description = $"Payment for order {order.Id} (after {shop.CommissionRate}% commission + shipping)",
                            BalanceAfter = shopOwner.WalletBalance, ReferenceId = order.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // 5. System Commission wallet gets commission + shipping system cut
                    var commWallet = await GetOrCreateSystemWallet("Commission");
                    commWallet.Balance += commission + systemShippingCut;
                    await _uow.SystemWalletTransactions.AddAsync(new SystemWalletTransaction
                    {
                        Id = Guid.NewGuid(), WalletType = "Commission",
                        Amount = commission + systemShippingCut,
                        TransactionType = "CommissionCollected",
                        Description = $"{shop.CommissionRate}% commission ({commission:N0}₫) + shipping cut ({systemShippingCut:N0}₫) from order {order.Id}",
                        BalanceAfter = commWallet.Balance, OrderId = order.Id, RelatedUserId = shop.OwnerId,
                        CreatedAt = DateTime.UtcNow
                    });

                    // 6. Save delivery record to ShipperDeliveries table
                    await _uow.ShipperDeliveries.AddAsync(new ShipperDelivery
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ShipperId = shipperId,
                        ShippingFee = order.ShippingFee,
                        EarnedAmount = shipperPayout,
                        SystemCut = systemShippingCut,
                        ShopPayout = shopPayout,
                        Commission = commission,
                        Status = "Completed",
                        DeliveredAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _uow.SaveChangesAsync();
            return "Order marked as delivered. Waiting for customer confirmation.";
        }

        public async Task<ShipperEarningsDto> GetEarningsAsync(Guid shipperId)
        {
            var account = await _uow.Accounts.GetByIdAsync(shipperId);
            if (account == null) throw new ArgumentException("Shipper not found.");

            // Query from dedicated ShipperDeliveries table
            var deliveries = await _uow.ShipperDeliveries.FindAsync(d => d.ShipperId == shipperId);
            var deliveryList = deliveries.OrderByDescending(d => d.DeliveredAt).ToList();

            var recentDeliveries = new List<ShipperDeliveryDto>();
            foreach (var d in deliveryList.Take(50))
            {
                var order = await _uow.Orders.GetByIdWithDetailsAsync(d.OrderId);
                recentDeliveries.Add(new ShipperDeliveryDto
                {
                    OrderId = d.OrderId,
                    ShopName = order?.Shop?.ShopName ?? "",
                    ShippingFee = d.ShippingFee,
                    EarnedAmount = d.EarnedAmount,
                    DeliveredAt = d.DeliveredAt
                });
            }

            return new ShipperEarningsDto
            {
                TotalEarnings = deliveryList.Sum(d => d.EarnedAmount),
                TotalDeliveries = deliveryList.Count,
                WalletBalance = account.WalletBalance,
                RecentDeliveries = recentDeliveries
            };
        }

        private async Task<SystemWallet> GetOrCreateSystemWallet(string walletType)
        {
            var w = await _uow.SystemWallets.FirstOrDefaultAsync(w => w.WalletType == walletType);
            if (w == null)
            {
                w = new SystemWallet { Id = Guid.NewGuid(), WalletType = walletType, Balance = 0, CreatedAt = DateTime.UtcNow };
                await _uow.SystemWallets.AddAsync(w);
            }
            return w;
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
