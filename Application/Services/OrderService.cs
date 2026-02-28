using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IVnPayService _vnPay;
        private readonly IViettelPostService _viettelPost;
        
        public OrderService(IUnitOfWork uow, IVnPayService vnPay, IViettelPostService viettelPost) 
        { 
            _uow = uow; 
            _vnPay = vnPay; 
            _viettelPost = viettelPost;
        }

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

            // Filter by selected cart items if CartItemIds is provided
            var selectedItems = cart.Items.ToList();
            if (dto.CartItemIds != null && dto.CartItemIds.Any())
            {
                selectedItems = selectedItems.Where(ci => dto.CartItemIds.Contains(ci.Id)).ToList();
                if (!selectedItems.Any()) throw new ArgumentException("No matching items found in cart.");
            }

            // Validate shipping address
            if (dto.ShippingAddressId.HasValue)
            {
                var addr = await _uow.Addresses.FirstOrDefaultAsync(a => a.Id == dto.ShippingAddressId && a.UserId == userId);
                if (addr == null) throw new ArgumentException("Shipping address not found.");
            }

            foreach (var item in selectedItems)
                if (item.Product.Stock < item.Quantity)
                    throw new ArgumentException($"Insufficient stock for '{item.Product.Name}'. Available: {item.Product.Stock}");

            var totalAmount = selectedItems.Sum(ci => ci.Product.Price * ci.Quantity);
            if (dto.PaymentMethod == "Wallet" && account.WalletBalance < totalAmount)
                throw new InvalidOperationException($"Insufficient wallet balance. Required: {totalAmount:F2}, Available: {account.WalletBalance:F2}");

            var createdOrders = new List<Order>();
            var createdOrdersResult = new List<object>();
            // Generate VnPayGroupId for linking multiple orders in one VNPay checkout
            var vnPayGroupId = dto.PaymentMethod == "VNPay" ? Guid.NewGuid().ToString("N")[..16] : null;

            foreach (var shopGroup in selectedItems.GroupBy(ci => ci.Product.ShopId))
            {
                var shop = await _uow.Shops.GetByIdAsync(shopGroup.Key);
                decimal shippingFee = 0;
                string? shippingProvider = null;

                if (dto.ShippingAddressId.HasValue)
                {
                    var addr = await _uow.Addresses.FirstOrDefaultAsync(a => a.Id == dto.ShippingAddressId && a.UserId == userId);
                    if (addr != null && shop != null)
                    {
                        try 
                        {
                            // A rough estimation of weight (e.g. 500g per item)
                            int totalWeight = shopGroup.Sum(ci => ci.Quantity) * 500;
                            var shopTotalAmount = shopGroup.Sum(ci => ci.Product.Price * ci.Quantity);
                            // Only calculate fee if the IDs are provided correctly from the frontend
                            if (shop.ProvinceId.HasValue && shop.DistrictId.HasValue && addr.ProvinceId.HasValue && addr.DistrictId.HasValue)
                            {
                                shippingFee = await _viettelPost.CalculateShippingFeeAsync(shop.ProvinceId.Value, shop.DistrictId.Value, addr.ProvinceId.Value, addr.DistrictId.Value, totalWeight, shopTotalAmount);
                                shippingProvider = "ViettelPost";
                            }
                            else 
                            {
                                // fallback if IDs are missing
                                shippingFee = 30000; 
                                shippingProvider = "Fixed";
                            }
                        }
                        catch
                        {
                            // fallback if API fails
                            shippingFee = 30000; 
                            shippingProvider = "External";
                        }
                    }
                }

                var shopTotal = shopGroup.Sum(ci => ci.Product.Price * ci.Quantity);
                var orderTotal = shopTotal + shippingFee; // Include shipping fee in total amount
                
                var order = new Order
                {
                    Id = Guid.NewGuid(), UserId = userId, ShopId = shopGroup.Key,
                    ShippingAddressId = dto.ShippingAddressId, 
                    ShippingFee = shippingFee,
                    ShippingProvider = shippingProvider,
                    TotalAmount = orderTotal, 
                    Status = "Pending",
                    Note = dto.Note, PaymentMethod = dto.PaymentMethod,
                    PaymentStatus = dto.PaymentMethod == "Wallet" ? "Paid" : "Pending",
                    VnPayGroupId = vnPayGroupId,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                foreach (var ci in shopGroup)
                {
                    order.Items.Add(new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, ProductId = ci.ProductId, Quantity = ci.Quantity, PriceAtPurchase = ci.Product.Price });
                    ci.Product.Stock -= ci.Quantity;
                }
                await _uow.Orders.AddAsync(order);
                await _uow.Payments.AddAsync(new Payment { Id = Guid.NewGuid(), OrderId = order.Id, UserId = userId, Amount = orderTotal, Method = dto.PaymentMethod, Status = dto.PaymentMethod == "Wallet" ? "Completed" : "Pending", CreatedAt = DateTime.UtcNow, CompletedAt = dto.PaymentMethod == "Wallet" ? DateTime.UtcNow : null });

                createdOrders.Add(order);
                createdOrdersResult.Add(new { OrderId = order.Id, ShopId = shopGroup.Key, ItemsAmount = shopTotal, ShippingFee = shippingFee, TotalAmount = orderTotal, order.Status });
            }

            // Adjust the total amount to deduct from wallet by calculating new grand total
            totalAmount = createdOrders.Sum(o => o.TotalAmount);
            if (dto.PaymentMethod == "Wallet" && account.WalletBalance < totalAmount)
                throw new InvalidOperationException($"Insufficient wallet balance including shipping fees. Required: {totalAmount:F2}, Available: {account.WalletBalance:F2}");

            if (dto.PaymentMethod == "Wallet")
            {
                account.WalletBalance -= totalAmount;
                await _uow.WalletTransactions.AddAsync(new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = account.Id, WalletType = "User", Amount = -totalAmount, TransactionType = "Purchase", Description = "Order purchase", BalanceAfter = account.WalletBalance, CreatedAt = DateTime.UtcNow });
                
                // Hold payment in System Escrow for each order
                foreach (var order in createdOrders)
                {
                    await HoldInEscrowAsync(order.TotalAmount, order.Id, userId, $"Hold payment for order {order.Id}");
                }
            }
            // Only remove purchased items from cart (not all items)
            _uow.CartItems.RemoveRange(selectedItems);
            await _uow.SaveChangesAsync();

            var result = new CreateOrderResult { Orders = createdOrdersResult, TotalAmount = totalAmount };
            if (dto.PaymentMethod == "VNPay")
            {
                var orderInfo = $"Thanh toan don hang {vnPayGroupId}";
                // Use vnPayGroupId as vnp_TxnRef so we can find all orders on callback
                result.PaymentUrl = _vnPay.CreatePaymentUrl(vnPayGroupId!, totalAmount, orderInfo, ipAddress);
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
            if (order.Status != "Delivered") throw new InvalidOperationException("Can only confirm received for delivered orders.");
            order.Status = "Completed"; order.UpdatedAt = DateTime.UtcNow;
            if (order.PaymentStatus == "Paid")
            {
                await CreditShopOwnerAsync(order.ShopId, order.TotalAmount, order.Id, "Sale", $"Sale from completed order {order.Id}");
            }
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
            return new { o.Id, o.UserId, CustomerName = o.User.FullName != "" ? o.User.FullName : o.User.Username, ItemsAmount = o.TotalAmount - o.ShippingFee, o.ShippingFee, o.TotalAmount, o.ShippingProvider, o.Status, o.PaymentMethod, o.PaymentStatus, o.Note, ShippingAddress = MapAddress(o.ShippingAddress), Items = o.Items.Select(oi => MapOrderItem(oi)).ToList(), o.CreatedAt };
        }

        public async Task<string> UpdateOrderStatusAsync(Guid shopId, Guid orderId, UpdateOrderStatusDto dto)
        {
            var order = await _uow.Orders.GetByIdWithItemsAsync(orderId);
            if (order == null || order.ShopId != shopId) throw new ArgumentException("Order not found.");
            var transitions = new Dictionary<string, string[]> 
            { 
                { "Pending", new[] { "Confirmed", "Cancelled" } }, 
                { "Confirmed", new[] { "ReadyForPickup", "Cancelled" } }
            };
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
            if (string.IsNullOrEmpty(txnRef)) return new VnPayIpnResult { RspCode = "01", Message = "Order not found" };

            // Find all orders by VnPayGroupId
            var orders = await _uow.Orders.GetByVnPayGroupIdAsync(txnRef);
            if (!orders.Any()) return new VnPayIpnResult { RspCode = "01", Message = "Order not found" };
            if (orders.All(o => o.PaymentStatus == "Paid")) return new VnPayIpnResult { RspCode = "02", Message = "Already confirmed" };

            var transactionNo = vnpayData.ContainsKey("vnp_TransactionNo") ? vnpayData["vnp_TransactionNo"] : null;
            await UpdateVnPayOrdersStatusAsync(orders, responseCode, transactionNo);
            await _uow.SaveChangesAsync();
            return new VnPayIpnResult { RspCode = "00", Message = "Confirm Success" };
        }

        public async Task<VnPayReturnResult> ProcessVnPayReturnAsync(Dictionary<string, string> vnpayData)
        {
            var isValid = _vnPay.ValidateCallback(vnpayData);
            var responseCode = _vnPay.GetResponseCode(vnpayData);
            var txnRef = vnpayData.ContainsKey("vnp_TxnRef") ? vnpayData["vnp_TxnRef"] : "";

            if (isValid && !string.IsNullOrEmpty(txnRef))
            {
                // Update payment status for all orders in this VNPay group
                var orders = await _uow.Orders.GetByVnPayGroupIdAsync(txnRef);
                if (orders.Any() && orders.Any(o => o.PaymentStatus == "Pending"))
                {
                    var transactionNo = vnpayData.ContainsKey("vnp_TransactionNo") ? vnpayData["vnp_TransactionNo"] : null;
                    await UpdateVnPayOrdersStatusAsync(orders, responseCode, transactionNo);
                    await _uow.SaveChangesAsync();
                }
            }

            return new VnPayReturnResult { Success = isValid && responseCode == "00", OrderId = txnRef, ResponseCode = responseCode, Message = responseCode == "00" ? "Payment successful" : "Payment failed or cancelled" };
        }

        /// <summary>
        /// Update payment status for all orders linked by VnPayGroupId
        /// </summary>
        private async Task UpdateVnPayOrdersStatusAsync(List<Order> orders, string responseCode, string? transactionNo)
        {
            foreach (var order in orders)
            {
                if (order.PaymentStatus == "Paid") continue; // Skip already paid

                if (responseCode == "00")
                {
                    order.PaymentStatus = "Paid"; order.UpdatedAt = DateTime.UtcNow;
                    var payment = await _uow.Payments.GetByOrderIdAsync(order.Id);
                    if (payment != null) { payment.Status = "Completed"; payment.CompletedAt = DateTime.UtcNow; payment.TransactionRef = transactionNo; }
                    
                    // Hold VNPay payment in System Escrow
                    await HoldInEscrowAsync(order.TotalAmount, order.Id, order.UserId, $"VNPay payment held for order {order.Id}");
                }
                else
                {
                    order.PaymentStatus = "Failed"; order.Status = "Cancelled"; order.UpdatedAt = DateTime.UtcNow;
                    foreach (var item in order.Items) item.Product.Stock += item.Quantity;
                    var payment = await _uow.Payments.GetByOrderIdAsync(order.Id);
                    if (payment != null) payment.Status = "Failed";
                }
            }
        }

        // === System Wallet Helpers ===

        /// <summary>
        /// Gi? ti?n trong Escrow khi thanh toán thành công
        /// </summary>
        private async Task HoldInEscrowAsync(decimal amount, Guid orderId, Guid customerId, string description)
        {
            var escrowWallet = await _uow.SystemWallets.GetByTypeAsync("Escrow");
            if (escrowWallet == null)
            {
                escrowWallet = new SystemWallet 
                { 
                    WalletType = "Escrow", 
                    Balance = 0,
                    Description = "Ví gi? ti?n t?m th?i cho ??n hàng ch?a hoàn thành"
                };
                await _uow.SystemWallets.AddAsync(escrowWallet);
            }

            escrowWallet.Balance += amount;
            escrowWallet.UpdatedAt = DateTime.UtcNow;

            await _uow.SystemWalletTransactions.AddAsync(new SystemWalletTransaction
            {
                WalletType = "Escrow",
                Amount = amount,
                TransactionType = "HoldFromCustomer",
                BalanceAfter = escrowWallet.Balance,
                OrderId = orderId,
                RelatedUserId = customerId,
                Description = description
            });
        }

        /// <summary>
        /// Gi?i phóng ti?n t? Escrow sang Shop Owner khi ??n hàng hoàn thành
        /// </summary>
        private async Task ReleaseFromEscrowAsync(decimal amount, Guid orderId, Guid shopOwnerId, string description)
        {
            var escrowWallet = await _uow.SystemWallets.GetByTypeAsync("Escrow");
            if (escrowWallet != null && escrowWallet.Balance >= amount)
            {
                escrowWallet.Balance -= amount;
                escrowWallet.UpdatedAt = DateTime.UtcNow;

                await _uow.SystemWalletTransactions.AddAsync(new SystemWalletTransaction
                {
                    WalletType = "Escrow",
                    Amount = -amount,
                    TransactionType = "ReleaseToShop",
                    BalanceAfter = escrowWallet.Balance,
                    OrderId = orderId,
                    RelatedUserId = shopOwnerId,
                    Description = description
                });
            }
        }

        /// <summary>
        /// Hoàn ti?n t? Escrow v? Customer khi h?y ??n
        /// </summary>
        private async Task RefundFromEscrowAsync(decimal amount, Guid orderId, Guid customerId, string description)
        {
            var escrowWallet = await _uow.SystemWallets.GetByTypeAsync("Escrow");
            if (escrowWallet != null && escrowWallet.Balance >= amount)
            {
                escrowWallet.Balance -= amount;
                escrowWallet.UpdatedAt = DateTime.UtcNow;

                await _uow.SystemWalletTransactions.AddAsync(new SystemWalletTransaction
                {
                    WalletType = "Escrow",
                    Amount = -amount,
                    TransactionType = "RefundToCustomer",
                    BalanceAfter = escrowWallet.Balance,
                    OrderId = orderId,
                    RelatedUserId = customerId,
                    Description = description
                });
            }
        }

        // === Existing Helpers ===

        /// <summary>
        /// Credit sale amount to shop owner's Account.WalletBalance (unified wallet)
        /// </summary>
        private async Task CreditShopOwnerAsync(Guid shopId, decimal amount, Guid orderId, string txnType, string description)
        {
            var shop = await _uow.Shops.GetByIdAsync(shopId);
            if (shop == null) return;
            var shopOwner = await _uow.Accounts.GetByIdAsync(shop.OwnerId);
            if (shopOwner == null) return;

            // Release from Escrow first
            await ReleaseFromEscrowAsync(amount, orderId, shopOwner.Id, $"Released to shop owner for order {orderId}");

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
            // Refund from Escrow first
            await RefundFromEscrowAsync(order.TotalAmount, order.Id, order.UserId, $"Refund for cancelled order {order.Id}");

            // Refund customer wallet (for Wallet payments)
            if (order.PaymentMethod == "Wallet")
            {
                var customer = await _uow.Accounts.GetByIdAsync(order.UserId);
                if (customer != null)
                {
                    customer.WalletBalance += order.TotalAmount;
                    await _uow.WalletTransactions.AddAsync(new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = customer.Id, WalletType = "User", Amount = order.TotalAmount, TransactionType = "Refund", Description = $"Refund for order {order.Id}", BalanceAfter = customer.WalletBalance, ReferenceId = order.Id, CreatedAt = DateTime.UtcNow });
                }
            }
            // Note: VNPay refunds would need to be handled separately (manual process or VNPay refund API)
            order.PaymentStatus = "Refunded";
        }

        private static OrderDetailDto MapOrderDetail(Order o) => new()
        {
            Id = o.Id, UserId = o.UserId, ShopId = o.ShopId, ShopName = o.Shop.ShopName, 
            ShipperId = o.ShipperId,
            ShipperName = o.Shipper != null ? (o.Shipper.FullName != "" ? o.Shipper.FullName : o.Shipper.Username) : null,
            ItemsAmount = o.TotalAmount - o.ShippingFee,
            ShippingFee = o.ShippingFee,
            TotalAmount = o.TotalAmount,
            ShippingProvider = o.ShippingProvider,
            Status = o.Status, PaymentMethod = o.PaymentMethod, PaymentStatus = o.PaymentStatus, Note = o.Note,
            ShippingAddress = MapAddress(o.ShippingAddress),
            Items = o.Items.Select(oi => MapOrderItem(oi)).ToList(), CreatedAt = o.CreatedAt
        };
        private static AddressDto? MapAddress(Address? a) => a == null ? null : new AddressDto { Id = a.Id, ReceiverName = a.ReceiverName, Phone = a.Phone, Street = a.Street, Ward = a.Ward, District = a.District, City = a.City, IsDefault = a.IsDefault };
        private static OrderItemDto MapOrderItem(OrderItem oi) => new() { ProductId = oi.ProductId, ProductName = oi.Product.Name, ProductImageUrl = oi.Product.ImageUrl, Quantity = oi.Quantity, PriceAtPurchase = oi.PriceAtPurchase };

        // === Refund Request ===

        public async Task<string> RequestRefundAsync(Guid userId, Guid orderId, CreateRefundRequestDto dto)
        {
            var order = await _uow.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order == null) throw new ArgumentException("Order not found.");
            if (order.Status != "Delivered") throw new InvalidOperationException("Can only request refund for delivered orders.");
            if (order.PaymentStatus != "Paid") throw new InvalidOperationException("Order has not been paid.");

            var existing = await _uow.RefundRequests.GetByOrderIdAsync(orderId);
            if (existing != null) throw new InvalidOperationException("A refund request already exists for this order.");

            if (string.IsNullOrWhiteSpace(dto.Reason)) throw new ArgumentException("Reason is required.");

            var refund = new RefundRequest
            {
                OrderId = orderId,
                CustomerId = userId,
                Reason = dto.Reason,
                Description = dto.Description,
                EvidenceUrls = dto.EvidenceUrls,
                Status = "Pending"
            };
            await _uow.RefundRequests.AddAsync(refund);

            order.Status = "RefundRequested";
            order.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return "Refund request submitted. Waiting for system staff review.";
        }

        public async Task<List<RefundRequestDto>> GetPendingRefundsAsync()
        {
            var refunds = await _uow.RefundRequests.GetPendingAsync();
            return refunds.Select(r => MapRefundRequest(r)).ToList();
        }

        public async Task<RefundRequestDto?> GetRefundByIdAsync(Guid refundId)
        {
            var r = await _uow.RefundRequests.GetByIdWithDetailsAsync(refundId);
            if (r == null) return null;
            return MapRefundRequest(r);
        }

        public async Task<string> ResolveRefundAsync(Guid staffId, Guid refundId, ResolveRefundDto dto)
        {
            var refund = await _uow.RefundRequests.GetByIdWithDetailsAsync(refundId);
            if (refund == null) throw new ArgumentException("Refund request not found.");
            if (refund.Status != "Pending") throw new InvalidOperationException("Refund request has already been resolved.");

            var order = await _uow.Orders.GetByIdWithItemsAsync(refund.OrderId);
            if (order == null) throw new ArgumentException("Order not found.");

            refund.ResolvedBy = staffId;
            refund.StaffNote = dto.StaffNote;
            refund.ResolvedAt = DateTime.UtcNow;

            if (dto.Approved)
            {
                refund.Status = "Approved";
                order.Status = "Returned";
                order.UpdatedAt = DateTime.UtcNow;

                // Refund from Escrow to customer
                await RefundFromEscrowAsync(order.TotalAmount, order.Id, order.UserId, $"Refund approved for order {order.Id}");

                if (order.PaymentMethod == "Wallet")
                {
                    var customer = await _uow.Accounts.GetByIdAsync(order.UserId);
                    if (customer != null)
                    {
                        customer.WalletBalance += order.TotalAmount;
                        await _uow.WalletTransactions.AddAsync(new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = customer.Id, WalletType = "User", Amount = order.TotalAmount, TransactionType = "Refund", Description = $"Refund approved for order {order.Id}", BalanceAfter = customer.WalletBalance, ReferenceId = order.Id, CreatedAt = DateTime.UtcNow });
                    }
                }
                order.PaymentStatus = "Refunded";

                // Restore stock
                foreach (var item in order.Items) item.Product.Stock += item.Quantity;
            }
            else
            {
                refund.Status = "Rejected";
                order.Status = "Completed";
                order.UpdatedAt = DateTime.UtcNow;

                // Refund rejected ? release Escrow to shop
                await CreditShopOwnerAsync(order.ShopId, order.TotalAmount, order.Id, "Sale", $"Refund rejected, payment released for order {order.Id}");
            }

            await _uow.SaveChangesAsync();
            return dto.Approved ? "Refund approved. Customer has been refunded." : "Refund rejected. Payment released to shop owner.";
        }

        private static RefundRequestDto MapRefundRequest(RefundRequest r) => new()
        {
            Id = r.Id,
            OrderId = r.OrderId,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer.FullName != "" ? r.Customer.FullName : r.Customer.Username,
            OrderAmount = r.Order.TotalAmount,
            Reason = r.Reason,
            Description = r.Description,
            EvidenceUrls = r.EvidenceUrls,
            Status = r.Status,
            StaffNote = r.StaffNote,
            ResolvedBy = r.ResolvedBy,
            CreatedAt = r.CreatedAt,
            ResolvedAt = r.ResolvedAt
        };
    }
}
