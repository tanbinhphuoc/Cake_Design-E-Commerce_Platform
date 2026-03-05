using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IVnPayService _vnPay;
        private readonly ICouponService _couponService;

        public OrderService(IUnitOfWork uow, IVnPayService vnPay, ICouponService couponService)
        {
            _uow = uow;
            _vnPay = vnPay;
            _couponService = couponService;
        }

        // =========================================================
        //  CREATE ORDER (Customer)
        // =========================================================
        public async Task<CreateOrderResult> CreateOrderAsync(Guid userId, CreateOrderDto dto, string ipAddress)
        {
            var account = await _uow.Accounts.GetByIdAsync(userId);
            if (account == null) throw new UnauthorizedAccessException("User not found.");

            var cart = await _uow.Carts.GetCartWithItemsAndProductsAsync(userId);
            if (cart == null || !cart.Items.Any()) throw new InvalidOperationException("Cart is empty.");

            var cartItems = dto.CartItemIds != null && dto.CartItemIds.Any()
                ? cart.Items.Where(ci => dto.CartItemIds.Contains(ci.Id)).ToList()
                : cart.Items.ToList();
            if (!cartItems.Any()) throw new InvalidOperationException("No matching items found in cart.");

            var address = dto.ShippingAddressId.HasValue
                ? await _uow.Addresses.GetByIdAsync(dto.ShippingAddressId.Value)
                : account.DefaultAddressId.HasValue
                    ? await _uow.Addresses.GetByIdAsync(account.DefaultAddressId.Value)
                    : null;
            if (address == null) throw new ArgumentException("Shipping address is required.");

            var shopGroups = cartItems.GroupBy(ci => ci.Product.ShopId).ToList();
            var result = new CreateOrderResult();
            var allOrders = new List<Order>();

            foreach (var group in shopGroups)
            {
                var shopId = group.Key;
                var shop = await _uow.Shops.GetByIdAsync(shopId);
                if (shop == null) continue;

                foreach (var item in group)
                {
                    if (item.Product.Stock < item.Quantity)
                        throw new InvalidOperationException($"Not enough stock for '{item.Product.Name}'. Available: {item.Product.Stock}");
                }

                var subtotal = group.Sum(ci => ci.Product.Price * ci.Quantity);

                // Apply Shop Coupon
                decimal shopDiscount = 0;
                Guid? shopCouponId = null;
                if (!string.IsNullOrWhiteSpace(dto.ShopCouponCode))
                {
                    var v = await _couponService.ValidateCouponAsync(dto.ShopCouponCode, userId, subtotal, shopId);
                    if (v.IsValid && v.CouponType == "Shop") { shopDiscount = v.DiscountAmount; shopCouponId = v.CouponId; }
                }

                // Apply System Coupon
                decimal systemDiscount = 0;
                Guid? systemCouponId = null;
                if (!string.IsNullOrWhiteSpace(dto.SystemCouponCode))
                {
                    var v = await _couponService.ValidateCouponAsync(dto.SystemCouponCode, userId, subtotal - shopDiscount, null);
                    if (v.IsValid && v.CouponType == "System") { systemDiscount = v.DiscountAmount; systemCouponId = v.CouponId; }
                }

                var totalDiscount = shopDiscount + systemDiscount;
                var shippingFee = 30000m;
                var totalAmount = Math.Max(subtotal - totalDiscount + shippingFee, 0);

                var order = new Order
                {
                    Id = Guid.NewGuid(), UserId = userId, ShopId = shopId,
                    ShippingAddressId = address.Id, Subtotal = subtotal,
                    DiscountAmount = totalDiscount, TaxRate = 0, TaxAmount = 0,
                    ShippingFee = shippingFee, TotalAmount = totalAmount,
                    ShopCouponId = shopCouponId, SystemCouponId = systemCouponId,
                    Status = "Pending", PaymentMethod = dto.PaymentMethod,
                    PaymentStatus = "Pending", Note = dto.Note,
                    ShippingProvider = "ViettelPost",
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };

                foreach (var ci in group)
                {
                    order.Items.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(), OrderId = order.Id,
                        ProductId = ci.ProductId, Quantity = ci.Quantity,
                        PriceAtPurchase = ci.Product.Price
                    });
                    ci.Product.Stock -= ci.Quantity;
                }

                await _uow.Orders.AddAsync(order);
                allOrders.Add(order);

                if (shopCouponId.HasValue)
                    await _couponService.RecordUsageAsync(shopCouponId.Value, userId, order.Id, shopDiscount);
                if (systemCouponId.HasValue)
                    await _couponService.RecordUsageAsync(systemCouponId.Value, userId, order.Id, systemDiscount);

                foreach (var ci in group) _uow.CartItems.Remove(ci);
            }

            var grandTotal = allOrders.Sum(o => o.TotalAmount);

            if (dto.PaymentMethod == "Wallet")
            {
                if (account.WalletBalance < grandTotal)
                    throw new InvalidOperationException($"Insufficient wallet balance. Required: {grandTotal:N0}, Available: {account.WalletBalance:N0}");

                account.WalletBalance -= grandTotal;
                await _uow.WalletTransactions.AddAsync(new WalletTransaction
                {
                    Id = Guid.NewGuid(), WalletOwnerId = account.Id, WalletType = "User",
                    Amount = -grandTotal, TransactionType = "Purchase",
                    Description = $"Order payment ({allOrders.Count} orders)",
                    BalanceAfter = account.WalletBalance, CreatedAt = DateTime.UtcNow
                });

                var escrow = await GetOrCreateSystemWallet("Escrow");
                escrow.Balance += grandTotal;
                await _uow.SystemWalletTransactions.AddAsync(new SystemWalletTransaction
                {
                    Id = Guid.NewGuid(), WalletType = "Escrow", Amount = grandTotal,
                    TransactionType = "OrderPayment",
                    Description = $"Escrow received for {allOrders.Count} orders",
                    BalanceAfter = escrow.Balance,
                    OrderId = allOrders.First().Id, RelatedUserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                var totalSysDiscount = allOrders.Where(o => o.SystemCouponId != null).Sum(o => o.DiscountAmount);
                if (totalSysDiscount > 0)
                {
                    var revenue = await GetOrCreateSystemWallet("Revenue");
                    revenue.Balance -= totalSysDiscount;
                    await _uow.SystemWalletTransactions.AddAsync(new SystemWalletTransaction
                    {
                        Id = Guid.NewGuid(), WalletType = "Revenue", Amount = -totalSysDiscount,
                        TransactionType = "SystemCouponDiscount",
                        Description = "System coupon discount",
                        BalanceAfter = revenue.Balance, RelatedUserId = userId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                foreach (var o in allOrders)
                {
                    o.PaymentStatus = "Paid";
                    await _uow.Payments.AddAsync(new Payment
                    {
                        Id = Guid.NewGuid(), OrderId = o.Id, UserId = userId,
                        Amount = o.TotalAmount, Method = "Wallet", Status = "Completed",
                        CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow
                    });
                }
                result.RemainingBalance = account.WalletBalance;
            }
            else if (dto.PaymentMethod == "VNPay")
            {
                var vnpayGroupId = Guid.NewGuid().ToString("N")[..16];
                foreach (var o in allOrders)
                {
                    o.VnPayGroupId = vnpayGroupId;
                    await _uow.Payments.AddAsync(new Payment
                    {
                        Id = Guid.NewGuid(), OrderId = o.Id, UserId = userId,
                        Amount = o.TotalAmount, Method = "VNPay", Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                result.PaymentUrl = _vnPay.CreatePaymentUrl(
                    vnpayGroupId, grandTotal,
                    $"Payment for {allOrders.Count} orders", ipAddress
                );
                result.RequiresPaymentRedirect = true;
            }

            result.TotalAmount = grandTotal;
            result.Orders = allOrders.Select(o => (object)new
            {
                OrderId = o.Id, o.ShopId, o.Subtotal, o.DiscountAmount, o.ShippingFee, o.TotalAmount
            }).ToList();

            await _uow.SaveChangesAsync();
            return result;
        }

        // =========================================================
        //  GET ORDERS (Customer)
        // =========================================================
        public async Task<List<OrderDetailDto>> GetOrdersAsync(Guid userId)
        {
            var orders = await _uow.Orders.GetOrdersByUserIdAsync(userId);
            return orders.Select(MapOrderDetail).ToList();
        }

        public async Task<OrderDetailDto?> GetOrderByIdAsync(Guid userId, Guid orderId)
        {
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null || order.UserId != userId) return null;
            return MapOrderDetail(order);
        }

        // =========================================================
        //  CANCEL ORDER (Customer, before shop confirms)
        // =========================================================
        public async Task<string> CancelOrderAsync(Guid userId, Guid orderId)
        {
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null || order.UserId != userId) throw new ArgumentException("Order not found.");
            if (order.Status != "Pending") throw new InvalidOperationException("Can only cancel orders with 'Pending' status.");

            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;

            foreach (var item in order.Items) item.Product.Stock += item.Quantity;
            await _couponService.ReverseUsageAsync(orderId);

            if (order.PaymentStatus == "Paid")
            {
                var escrow = await GetOrCreateSystemWallet("Escrow");
                escrow.Balance -= order.TotalAmount;
                await _uow.SystemWalletTransactions.AddAsync(new SystemWalletTransaction
                {
                    Id = Guid.NewGuid(), WalletType = "Escrow", Amount = -order.TotalAmount,
                    TransactionType = "CancelRefund",
                    Description = $"Refund for cancelled order {order.Id}",
                    BalanceAfter = escrow.Balance, OrderId = order.Id, RelatedUserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                var customer = await _uow.Accounts.GetByIdAsync(userId);
                if (customer != null)
                {
                    customer.WalletBalance += order.TotalAmount;
                    await _uow.WalletTransactions.AddAsync(new WalletTransaction
                    {
                        Id = Guid.NewGuid(), WalletOwnerId = customer.Id, WalletType = "User",
                        Amount = order.TotalAmount, TransactionType = "Refund",
                        Description = $"Refund for cancelled order {order.Id}",
                        BalanceAfter = customer.WalletBalance, ReferenceId = order.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                order.PaymentStatus = "Refunded";
                var payment = await _uow.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
                if (payment != null) { payment.Status = "Refunded"; payment.CompletedAt = DateTime.UtcNow; }
            }

            await _uow.SaveChangesAsync();
            return order.PaymentStatus == "Refunded"
                ? "Order cancelled. Payment has been refunded to your wallet."
                : "Order cancelled successfully.";
        }

        // =========================================================
        //  CONFIRM RECEIVED (Customer)
        // =========================================================
        public async Task<string> ConfirmReceivedAsync(Guid userId, Guid orderId)
        {
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null || order.UserId != userId) throw new ArgumentException("Order not found.");
            if (order.Status != "Delivered") throw new InvalidOperationException("Order must be 'Delivered' to confirm.");
            order.Status = "Completed";
            order.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return "Order confirmed as received.";
        }

        // =========================================================
        //  REQUEST REFUND (Customer)
        // =========================================================
        public async Task<string> RequestRefundAsync(Guid userId, Guid orderId, CreateRefundRequestDto dto)
        {
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null || order.UserId != userId) throw new ArgumentException("Order not found.");
            if (order.Status != "Delivered" && order.Status != "Completed")
                throw new InvalidOperationException("Can only request refund for delivered/completed orders.");
            if (string.IsNullOrWhiteSpace(dto.Reason)) throw new ArgumentException("Reason is required.");

            var existing = await _uow.RefundRequests.FirstOrDefaultAsync(r => r.OrderId == orderId);
            if (existing != null) throw new InvalidOperationException("A refund request already exists for this order.");

            await _uow.RefundRequests.AddAsync(new RefundRequest
            {
                Id = Guid.NewGuid(), OrderId = orderId, CustomerId = userId,
                Reason = dto.Reason, Description = dto.Description ?? "",
                EvidenceUrls = dto.EvidenceUrls ?? "", Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            await _uow.SaveChangesAsync();
            return "Refund request submitted. Waiting for system staff review.";
        }

        // =========================================================
        //  SHOP OWNER
        // =========================================================
        public async Task<List<object>> GetShopOrdersAsync(Guid shopId)
        {
            var orders = await _uow.Orders.FindAsync(o => o.ShopId == shopId);
            return orders.OrderByDescending(o => o.CreatedAt).Select(o => (object)new
            {
                o.Id, o.UserId, CustomerName = o.User?.FullName ?? o.User?.Username ?? "",
                o.Subtotal, o.DiscountAmount, o.ShippingFee, o.TotalAmount,
                o.Status, o.PaymentMethod, o.PaymentStatus, o.CreatedAt
            }).ToList();
        }

        public async Task<object?> GetShopOrderByIdAsync(Guid shopId, Guid orderId)
        {
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null || order.ShopId != shopId) return null;
            return MapOrderDetail(order);
        }

        public async Task<string> UpdateOrderStatusAsync(Guid shopId, Guid orderId, UpdateOrderStatusDto dto)
        {
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null || order.ShopId != shopId) throw new ArgumentException("Order not found.");

            var valid = new Dictionary<string, string[]>
            {
                { "Pending", new[] { "Confirmed", "Cancelled" } },
                { "Confirmed", new[] { "ReadyForPickup", "Cancelled" } }
            };
            if (!valid.ContainsKey(order.Status) || !valid[order.Status].Contains(dto.Status))
                throw new InvalidOperationException($"Cannot change status from '{order.Status}' to '{dto.Status}'.");

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();
            return $"Order status updated to '{dto.Status}'.";
        }

        // =========================================================
        //  ADMIN
        // =========================================================
        public async Task<List<object>> GetAllOrdersAsync()
        {
            var orders = await _uow.Orders.GetAllAsync();
            return orders.OrderByDescending(o => o.CreatedAt).Select(o => (object)new
            {
                o.Id, o.UserId, o.ShopId, o.Subtotal, o.DiscountAmount,
                o.ShippingFee, o.TotalAmount, o.Status, o.PaymentMethod,
                o.PaymentStatus, o.CreatedAt
            }).ToList();
        }

        // =========================================================
        //  VNPAY
        // =========================================================
        public async Task<VnPayIpnResult> ProcessVnPayIpnAsync(Dictionary<string, string> vnpayData)
        {
            if (!_vnPay.ValidateCallback(vnpayData))
                return new VnPayIpnResult { RspCode = "97", Message = "Invalid signature" };

            var vnpayGroupId = vnpayData.GetValueOrDefault("vnp_TxnRef", "");
            var responseCode = vnpayData.GetValueOrDefault("vnp_ResponseCode", "");
            var txnRef = vnpayData.GetValueOrDefault("vnp_TransactionNo", "");

            var orders = (await _uow.Orders.FindAsync(o => o.VnPayGroupId == vnpayGroupId)).ToList();
            if (!orders.Any()) return new VnPayIpnResult { RspCode = "01", Message = "Order not found" };
            if (orders.Any(o => o.PaymentStatus == "Paid"))
                return new VnPayIpnResult { RspCode = "02", Message = "Already processed" };

            if (responseCode == "00")
            {
                var grandTotal = orders.Sum(o => o.TotalAmount);
                var escrow = await GetOrCreateSystemWallet("Escrow");
                escrow.Balance += grandTotal;
                await _uow.SystemWalletTransactions.AddAsync(new SystemWalletTransaction
                {
                    Id = Guid.NewGuid(), WalletType = "Escrow", Amount = grandTotal,
                    TransactionType = "VnPayPayment",
                    Description = $"VNPay payment for group {vnpayGroupId}",
                    BalanceAfter = escrow.Balance,
                    OrderId = orders.First().Id, RelatedUserId = orders.First().UserId,
                    CreatedAt = DateTime.UtcNow
                });

                foreach (var o in orders)
                {
                    o.PaymentStatus = "Paid"; o.UpdatedAt = DateTime.UtcNow;
                    var p = await _uow.Payments.FirstOrDefaultAsync(p => p.OrderId == o.Id);
                    if (p != null) { p.Status = "Completed"; p.TransactionRef = txnRef; p.CompletedAt = DateTime.UtcNow; }
                }
            }
            else
            {
                foreach (var o in orders)
                {
                    o.PaymentStatus = "Failed"; o.Status = "Cancelled"; o.UpdatedAt = DateTime.UtcNow;
                    var full = await _uow.Orders.GetByIdWithItemsAsync(o.Id);
                    if (full != null) foreach (var item in full.Items) item.Product.Stock += item.Quantity;
                    await _couponService.ReverseUsageAsync(o.Id);
                    var p = await _uow.Payments.FirstOrDefaultAsync(p => p.OrderId == o.Id);
                    if (p != null) { p.Status = "Failed"; p.CompletedAt = DateTime.UtcNow; }
                }
            }

            await _uow.SaveChangesAsync();
            return new VnPayIpnResult { RspCode = "00", Message = "Confirm Success" };
        }

        public async Task<VnPayReturnResult> ProcessVnPayReturnAsync(Dictionary<string, string> vnpayData)
        {
            var valid = _vnPay.ValidateCallback(vnpayData);
            var code = vnpayData.GetValueOrDefault("vnp_ResponseCode", "");

            // Also process the payment (update DB) — fallback in case IPN didn't reach us
            if (valid)
            {
                await ProcessVnPayIpnAsync(vnpayData);
            }

            return new VnPayReturnResult
            {
                Success = valid && code == "00",
                OrderId = vnpayData.GetValueOrDefault("vnp_TxnRef", ""),
                ResponseCode = code,
                Message = valid && code == "00" ? "Payment successful" : "Payment failed"
            };
        }

        // =========================================================
        //  REFUND MANAGEMENT (SystemStaff/Admin)
        // =========================================================
        public async Task<List<RefundRequestDto>> GetPendingRefundsAsync()
        {
            var refunds = await _uow.RefundRequests.GetPendingAsync();
            return refunds.Select(MapRefundRequest).ToList();
        }

        public async Task<RefundRequestDto?> GetRefundByIdAsync(Guid refundId)
        {
            var r = await _uow.RefundRequests.GetByIdWithDetailsAsync(refundId);
            return r == null ? null : MapRefundRequest(r);
        }

        public async Task<string> ResolveRefundAsync(Guid staffId, Guid refundId, ResolveRefundDto dto)
        {
            var refund = await _uow.RefundRequests.GetByIdWithDetailsAsync(refundId);
            if (refund == null) throw new ArgumentException("Refund request not found.");
            if (refund.Status != "Pending") throw new InvalidOperationException("Refund already resolved.");

            var order = await _uow.Orders.GetByIdWithItemsAsync(refund.OrderId);
            if (order == null) throw new ArgumentException("Order not found.");

            refund.ResolvedBy = staffId;
            refund.StaffNote = dto.StaffNote;
            refund.ResolvedAt = DateTime.UtcNow;

            if (dto.Approved)
            {
                refund.Status = "Approved";
                order.Status = "Returned";
                order.PaymentStatus = "Refunded";
                order.UpdatedAt = DateTime.UtcNow;

                var shop = await _uow.Shops.GetByIdAsync(order.ShopId);
                if (shop != null)
                {
                    var shopOwner = await _uow.Accounts.GetByIdAsync(shop.OwnerId);
                    if (shopOwner != null)
                    {
                        shopOwner.WalletBalance -= order.TotalAmount;
                        await _uow.WalletTransactions.AddAsync(new WalletTransaction
                        {
                            Id = Guid.NewGuid(), WalletOwnerId = shopOwner.Id, WalletType = "User",
                            Amount = -order.TotalAmount, TransactionType = "RefundDeduction",
                            Description = $"Refund deduction for order {order.Id}",
                            BalanceAfter = shopOwner.WalletBalance, ReferenceId = order.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                var customer = await _uow.Accounts.GetByIdAsync(order.UserId);
                if (customer != null)
                {
                    customer.WalletBalance += order.TotalAmount;
                    await _uow.WalletTransactions.AddAsync(new WalletTransaction
                    {
                        Id = Guid.NewGuid(), WalletOwnerId = customer.Id, WalletType = "User",
                        Amount = order.TotalAmount, TransactionType = "Refund",
                        Description = $"Refund for order {order.Id}",
                        BalanceAfter = customer.WalletBalance, ReferenceId = order.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                foreach (var item in order.Items) item.Product.Stock += item.Quantity;
                await _couponService.CreateCompensationCouponAsync(order.UserId, order.TotalAmount);
            }
            else
            {
                refund.Status = "Rejected";
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _uow.SaveChangesAsync();
            return dto.Approved
                ? "Refund approved. Customer refunded and received a compensation coupon."
                : "Refund rejected.";
        }

        // =========================================================
        //  HELPERS
        // =========================================================
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

        private static OrderDetailDto MapOrderDetail(Order o) => new()
        {
            Id = o.Id, UserId = o.UserId, ShopId = o.ShopId,
            ShopName = o.Shop?.ShopName ?? "",
            ShipperId = o.ShipperId, ShipperName = o.Shipper?.FullName ?? o.Shipper?.Username,
            Subtotal = o.Subtotal, DiscountAmount = o.DiscountAmount,
            TaxAmount = o.TaxAmount, ShippingFee = o.ShippingFee, TotalAmount = o.TotalAmount,
            ShopCouponCode = o.ShopCoupon?.Code, SystemCouponCode = o.SystemCoupon?.Code,
            ShippingProvider = o.ShippingProvider,
            Status = o.Status, PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus, Note = o.Note,
            ShippingAddress = o.ShippingAddress != null ? new AddressDto
            {
                Id = o.ShippingAddress.Id, ReceiverName = o.ShippingAddress.ReceiverName,
                Phone = o.ShippingAddress.Phone, Street = o.ShippingAddress.Street,
                Ward = o.ShippingAddress.Ward, District = o.ShippingAddress.District,
                City = o.ShippingAddress.City
            } : null,
            Items = o.Items.Select(oi => new OrderItemDto
            {
                ProductId = oi.ProductId, ProductName = oi.Product?.Name ?? "",
                ProductImageUrl = oi.Product?.ImageUrl ?? "",
                Quantity = oi.Quantity, PriceAtPurchase = oi.PriceAtPurchase
            }).ToList(),
            CreatedAt = o.CreatedAt
        };

        private static RefundRequestDto MapRefundRequest(RefundRequest r) => new()
        {
            Id = r.Id, OrderId = r.OrderId, CustomerId = r.CustomerId,
            CustomerName = r.Customer?.FullName ?? r.Customer?.Username ?? "",
            OrderAmount = r.Order?.TotalAmount ?? 0,
            Reason = r.Reason, Description = r.Description,
            EvidenceUrls = r.EvidenceUrls, Status = r.Status,
            StaffNote = r.StaffNote, ResolvedBy = r.ResolvedBy,
            CreatedAt = r.CreatedAt, ResolvedAt = r.ResolvedAt
        };
    }
}
