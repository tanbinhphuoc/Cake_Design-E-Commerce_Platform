using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IVnPayService _vnPay;
        public OrderService(IUnitOfWork uow, IVnPayService vnPay) { _uow = uow; _vnPay = vnPay; }

        public async Task<CreateOrderResult> CreateOrderAsync(Guid userId, CreateOrderDto dto, string ipAddress)
        {
            // Validate payment method
            var validMethods = new[] { "Wallet", "VNPay" };
            if (!validMethods.Contains(dto.PaymentMethod))
                throw new ArgumentException("Payment method must be 'Wallet' or 'VNPay'.");

            var account = await _uow.Accounts.GetByIdAsync(userId);
            if (account == null) throw new UnauthorizedAccessException();
            var cart = await _uow.Carts.GetCartWithItemsAndProductsAsync(userId);
            if (cart == null || !cart.Items.Any()) throw new ArgumentException("Cart is empty.");

            // Validate shipping address
            if (dto.ShippingAddressId.HasValue)
            {
                var addr = await _uow.Addresses.FirstOrDefaultAsync(a => a.Id == dto.ShippingAddressId && a.UserId == userId);
                if (addr == null) throw new ArgumentException("Shipping address not found.");
            }

            foreach (var item in cart.Items)
                if (item.Product.Stock < item.Quantity)
                    throw new ArgumentException($"Insufficient stock for '{item.Product.Name}'. Available: {item.Product.Stock}");

            var totalAmount = cart.Items.Sum(ci => ci.Product.Price * ci.Quantity);
            if (dto.PaymentMethod == "Wallet" && account.WalletBalance < totalAmount)
                throw new InvalidOperationException($"Insufficient wallet balance. Required: {totalAmount:F2}, Available: {account.WalletBalance:F2}");

            var createdOrders = new List<object>();
            var orderIds = new List<Guid>();

            foreach (var shopGroup in cart.Items.GroupBy(ci => ci.Product.ShopId))
            {
                var shopTotal = shopGroup.Sum(ci => ci.Product.Price * ci.Quantity);
                var order = new Order
                {
                    Id = Guid.NewGuid(), UserId = userId, ShopId = shopGroup.Key,
                    ShippingAddressId = dto.ShippingAddressId, TotalAmount = shopTotal, Status = "Pending",
                    Note = dto.Note, PaymentMethod = dto.PaymentMethod,
                    PaymentStatus = dto.PaymentMethod == "Wallet" ? "Paid" : "Pending",
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                foreach (var ci in shopGroup)
                {
                    order.Items.Add(new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = ci.ProductId, Quantity = ci.Quantity, PriceAtPurchase = ci.Product.Price });
                    ci.Product.Stock -= ci.Quantity;
                }
                await _uow.Orders.AddAsync(order);
                await _uow.Payments.AddAsync(new Payment { Id = Guid.NewGuid(), OrderId = order.Id, UserId = userId, Amount = shopTotal, Method = dto.PaymentMethod, Status = dto.PaymentMethod == "Wallet" ? "Completed" : "Pending", CreatedAt = DateTime.UtcNow, CompletedAt = dto.PaymentMethod == "Wallet" ? DateTime.UtcNow : null });

                if (dto.PaymentMethod == "Wallet")
                {
                    // Credit shop OWNER's account wallet (unified wallet)
                    await CreditShopOwnerAsync(shopGroup.Key, shopTotal, order.Id, "Sale", $"Sale from order {order.Id}");
                }
                orderIds.Add(order.Id);
                createdOrders.Add(new { OrderId = order.Id, ShopId = shopGroup.Key, TotalAmount = shopTotal, order.Status });
            }

            if (dto.PaymentMethod == "Wallet")
            {
                account.WalletBalance -= totalAmount;
                await _uow.WalletTransactions.AddAsync(new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = account.Id, WalletType = "User", Amount = -totalAmount, TransactionType = "Purchase", Description = "Order purchase", BalanceAfter = account.WalletBalance, CreatedAt = DateTime.UtcNow });
            }
            _uow.CartItems.RemoveRange(cart.Items);
            await _uow.SaveChangesAsync();

            var result = new CreateOrderResult { Orders = createdOrders, TotalAmount = totalAmount };
            if (dto.PaymentMethod == "VNPay")
            {
                var orderInfo = $"Thanh toan don hang {string.Join(", ", orderIds.Select(id => id.ToString()[..8]))}";
                result.PaymentUrl = _vnPay.CreatePaymentUrl(orderIds.First(), totalAmount, orderInfo, ipAddress);
                result.RequiresPaymentRedirect = true;
            }
            else { result.RemainingBalance = account.WalletBalance; }
            return result;
        }

        public async Task<List<OrderDetailDto>> GetOrdersAsync(Guid userId)
        {
            var orders = await _uow.Orders.GetOrdersByUserIdAsync(userId);
            return orders.Select(o => MapOrderDetail(o)).ToList();
        }

        public async Task<OrderDetailDto?> GetOrderByIdAsync(Guid userId, Guid orderId)
        {
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId);
            if (order == null || order.UserId != userId) return null;
            return MapOrderDetail(order);
        }

        public async Task<string> CancelOrderAsync(Guid userId, Guid orderId)
        {
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null || order.UserId != userId) throw new ArgumentException("Order not found.");
            if (order.Status != "Pending") throw new InvalidOperationException("Can only cancel pending orders.");
            order.Status = "Cancelled"; order.UpdatedAt = DateTime.UtcNow;
            foreach (var item in order.Items) item.Product.Stock += item.Quantity;
            if (order.PaymentStatus == "Paid")
                await ProcessRefundAsync(order);
            await _uow.SaveChangesAsync();
            return "Order cancelled successfully.";
        }

        public async Task<string> ConfirmReceivedAsync(Guid userId, Guid orderId)
        {
            var order = await _uow.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order == null) throw new ArgumentException("Order not found.");
            if (order.Status != "Shipping") throw new InvalidOperationException("Can only confirm received for shipping orders.");
            order.Status = "Completed"; order.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return "Order confirmed as received.";
        }

        public async Task<List<object>> GetShopOrdersAsync(Guid shopId)
        {
            var orders = await _uow.Orders.GetOrdersByShopIdAsync(shopId);
            return orders.Select(o => (object)new
            {
                o.Id, o.UserId, CustomerName = o.User.FullName != "" ? o.User.FullName : o.User.Username,
                o.TotalAmount, o.Status, o.PaymentMethod, o.PaymentStatus, o.Note,
                ShippingAddress = MapAddress(o.ShippingAddress),
                Items = o.Items.Select(oi => MapOrderItem(oi)).ToList(), o.CreatedAt
            }).ToList();
        }

        public async Task<object?> GetShopOrderByIdAsync(Guid shopId, Guid orderId)
        {
            var o = await _uow.Orders.GetByIdWithDetailsAsync(orderId);
            if (o == null || o.ShopId != shopId) return null;
            return new { o.Id, o.UserId, CustomerName = o.User.FullName != "" ? o.User.FullName : o.User.Username, o.TotalAmount, o.Status, o.PaymentMethod, o.PaymentStatus, o.Note, ShippingAddress = MapAddress(o.ShippingAddress), Items = o.Items.Select(oi => MapOrderItem(oi)).ToList(), o.CreatedAt };
        }

        public async Task<string> UpdateOrderStatusAsync(Guid shopId, Guid orderId, UpdateOrderStatusDto dto)
        {
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null || order.ShopId != shopId) throw new ArgumentException("Order not found.");
            var transitions = new Dictionary<string, string[]> { { "Pending", new[] { "Confirmed", "Cancelled" } }, { "Confirmed", new[] { "Shipping", "Cancelled" } }, { "Shipping", new[] { "Completed" } } };
            if (!transitions.ContainsKey(order.Status) || !transitions[order.Status].Contains(dto.Status))
                throw new InvalidOperationException($"Invalid status transition from '{order.Status}' to '{dto.Status}'.");
            order.Status = dto.Status; order.UpdatedAt = DateTime.UtcNow;
            if (dto.Status == "Cancelled")
            {
                foreach (var item in order.Items) item.Product.Stock += item.Quantity;
                if (order.PaymentStatus == "Paid") await ProcessRefundAsync(order);
            }
            await _uow.SaveChangesAsync();
            return $"Order status updated to '{dto.Status}'.";
        }

        public async Task<List<object>> GetAllOrdersAsync()
        {
            var all = await _uow.Orders.GetAllAsync();
            var result = new List<object>();
            foreach (var o in all.OrderByDescending(o => o.CreatedAt))
            {
                var full = await _uow.Orders.GetByIdWithDetailsAsync(o.Id);
                if (full == null) continue;
                result.Add(new { full.Id, full.UserId, CustomerName = full.User.Username, full.ShopId, ShopName = full.Shop.ShopName, full.TotalAmount, full.Status, full.PaymentMethod, full.PaymentStatus, full.CreatedAt });
            }
            return result;
        }

        public async Task<VnPayIpnResult> ProcessVnPayIpnAsync(Dictionary<string, string> vnpayData)
        {
            if (!_vnPay.ValidateCallback(vnpayData)) return new VnPayIpnResult { RspCode = "97", Message = "Invalid signature" };
            var responseCode = _vnPay.GetResponseCode(vnpayData);
            var txnRef = vnpayData.ContainsKey("vnp_TxnRef") ? vnpayData["vnp_TxnRef"] : "";
            if (!Guid.TryParse(txnRef, out var orderId)) return new VnPayIpnResult { RspCode = "01", Message = "Order not found" };
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null) return new VnPayIpnResult { RspCode = "01", Message = "Order not found" };
            if (order.PaymentStatus == "Paid") return new VnPayIpnResult { RspCode = "02", Message = "Already confirmed" };

            if (responseCode == "00")
            {
                order.PaymentStatus = "Paid"; order.UpdatedAt = DateTime.UtcNow;
                var payment = await _uow.Payments.GetByOrderIdAsync(orderId);
                if (payment != null) { payment.Status = "Completed"; payment.CompletedAt = DateTime.UtcNow; payment.TransactionRef = vnpayData.ContainsKey("vnp_TransactionNo") ? vnpayData["vnp_TransactionNo"] : null; }
                // Credit shop owner's wallet (unified)
                await CreditShopOwnerAsync(order.ShopId, order.TotalAmount, order.Id, "Sale", $"VNPay payment for order {order.Id}");
            }
            else
            {
                order.PaymentStatus = "Failed"; order.Status = "Cancelled"; order.UpdatedAt = DateTime.UtcNow;
                foreach (var item in order.Items) item.Product.Stock += item.Quantity;
                var payment = await _uow.Payments.GetByOrderIdAsync(orderId);
                if (payment != null) payment.Status = "Failed";
            }
            await _uow.SaveChangesAsync();
            return new VnPayIpnResult { RspCode = "00", Message = "Confirm Success" };
        }

        public VnPayReturnResult ProcessVnPayReturn(Dictionary<string, string> vnpayData)
        {
            var isValid = _vnPay.ValidateCallback(vnpayData);
            var responseCode = _vnPay.GetResponseCode(vnpayData);
            var txnRef = vnpayData.ContainsKey("vnp_TxnRef") ? vnpayData["vnp_TxnRef"] : "";
            return new VnPayReturnResult { Success = isValid && responseCode == "00", OrderId = txnRef, ResponseCode = responseCode, Message = responseCode == "00" ? "Payment successful" : "Payment failed or cancelled" };
        }

        // === Helpers ===

        /// <summary>
        /// Credit sale amount to shop owner's Account.WalletBalance (unified wallet)
        /// </summary>
        private async Task CreditShopOwnerAsync(Guid shopId, decimal amount, Guid orderId, string txnType, string description)
        {
            var shop = await _uow.Shops.GetByIdAsync(shopId);
            if (shop == null) return;
            var shopOwner = await _uow.Accounts.GetByIdAsync(shop.OwnerId);
            if (shopOwner == null) return;

            shopOwner.WalletBalance += amount;
            await _uow.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid(), WalletOwnerId = shopOwner.Id, WalletType = "Shop",
                Amount = amount, TransactionType = txnType, Description = description,
                BalanceAfter = shopOwner.WalletBalance, ReferenceId = orderId,
                CreatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Process refund: credit customer's wallet, debit shop owner's wallet
        /// </summary>
        private async Task ProcessRefundAsync(Order order)
        {
            // Refund customer
            if (order.PaymentMethod == "Wallet")
            {
                var customer = await _uow.Accounts.GetByIdAsync(order.UserId);
                if (customer != null)
                {
                    customer.WalletBalance += order.TotalAmount;
                    await _uow.WalletTransactions.AddAsync(new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = customer.Id, WalletType = "User", Amount = order.TotalAmount, TransactionType = "Refund", Description = $"Refund for order {order.Id}", BalanceAfter = customer.WalletBalance, ReferenceId = order.Id, CreatedAt = DateTime.UtcNow });
                }
            }
            // Debit shop owner's wallet (unified)
            var shop = await _uow.Shops.GetByIdAsync(order.ShopId);
            if (shop != null)
            {
                var shopOwner = await _uow.Accounts.GetByIdAsync(shop.OwnerId);
                if (shopOwner != null)
                {
                    shopOwner.WalletBalance -= order.TotalAmount;
                    await _uow.WalletTransactions.AddAsync(new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = shopOwner.Id, WalletType = "Shop", Amount = -order.TotalAmount, TransactionType = "Refund", Description = $"Refund for order {order.Id}", BalanceAfter = shopOwner.WalletBalance, ReferenceId = order.Id, CreatedAt = DateTime.UtcNow });
                }
            }
            order.PaymentStatus = "Refunded";
        }

        private static OrderDetailDto MapOrderDetail(Order o) => new()
        {
            Id = o.Id, UserId = o.UserId, ShopId = o.ShopId, ShopName = o.Shop.ShopName, TotalAmount = o.TotalAmount,
            Status = o.Status, PaymentMethod = o.PaymentMethod, PaymentStatus = o.PaymentStatus, Note = o.Note,
            ShippingAddress = MapAddress(o.ShippingAddress),
            Items = o.Items.Select(oi => MapOrderItem(oi)).ToList(), CreatedAt = o.CreatedAt
        };
        private static AddressDto? MapAddress(Address? a) => a == null ? null : new AddressDto { Id = a.Id, ReceiverName = a.ReceiverName, Phone = a.Phone, Street = a.Street, Ward = a.Ward, District = a.District, City = a.City, IsDefault = a.IsDefault };
        private static OrderItemDto MapOrderItem(OrderItem oi) => new() { ProductId = oi.ProductId, ProductName = oi.Product.Name, ProductImageUrl = oi.Product.ImageUrl, Quantity = oi.Quantity, PriceAtPurchase = oi.PriceAtPurchase };
    }
}
