using Application.DTOs;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // ===== Customer Orders =====

        /// <summary>
        /// Create orders from the current user's cart (groups by shop).
        /// </summary>
        [HttpPost("orders/create")]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var account = await _context.Accounts.FindAsync(userId);
            if (account == null) return Unauthorized();

            // Get cart with items grouped by shop
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Shop)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest(new { Message = "Cart is empty." });

            // Validate stock
            foreach (var item in cart.Items)
            {
                if (item.Product.Stock < item.Quantity)
                    return BadRequest(new { Message = $"Insufficient stock for '{item.Product.Name}'. Available: {item.Product.Stock}" });
            }

            // Group cart items by shop
            var itemsByShop = cart.Items.GroupBy(ci => ci.Product.ShopId);

            var totalAmount = cart.Items.Sum(ci => ci.Product.Price * ci.Quantity);

            // Check wallet balance if payment method is Wallet
            if (dto.PaymentMethod == "Wallet")
            {
                if (account.WalletBalance < totalAmount)
                    return BadRequest(new { Message = $"Insufficient wallet balance. Required: {totalAmount:F2}, Available: {account.WalletBalance:F2}" });
            }

            var createdOrders = new List<object>();

            foreach (var shopGroup in itemsByShop)
            {
                var shopTotal = shopGroup.Sum(ci => ci.Product.Price * ci.Quantity);

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value,
                    ShopId = shopGroup.Key,
                    ShippingAddressId = dto.ShippingAddressId,
                    TotalAmount = shopTotal,
                    Status = "Pending",
                    Note = dto.Note,
                    PaymentMethod = dto.PaymentMethod,
                    PaymentStatus = dto.PaymentMethod == "Wallet" ? "Paid" : "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                foreach (var cartItem in shopGroup)
                {
                    order.Items.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        PriceAtPurchase = cartItem.Product.Price
                    });

                    // Deduct stock
                    cartItem.Product.Stock -= cartItem.Quantity;
                }

                _context.Orders.Add(order);

                // Create payment record
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    UserId = userId.Value,
                    Amount = shopTotal,
                    Method = dto.PaymentMethod,
                    Status = dto.PaymentMethod == "Wallet" ? "Completed" : "Pending",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = dto.PaymentMethod == "Wallet" ? DateTime.UtcNow : null
                };
                _context.Payments.Add(payment);

                // Credit shop wallet if wallet payment
                if (dto.PaymentMethod == "Wallet")
                {
                    var shop = await _context.Shops.FindAsync(shopGroup.Key);
                    if (shop != null)
                    {
                        shop.WalletBalance += shopTotal;

                        // Record shop wallet transaction
                        _context.WalletTransactions.Add(new WalletTransaction
                        {
                            Id = Guid.NewGuid(),
                            WalletOwnerId = shop.Id,
                            WalletType = "Shop",
                            Amount = shopTotal,
                            TransactionType = "Sale",
                            Description = $"Sale from order {order.Id}",
                            BalanceAfter = shop.WalletBalance,
                            ReferenceId = order.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                createdOrders.Add(new
                {
                    OrderId = order.Id,
                    ShopId = shopGroup.Key,
                    TotalAmount = shopTotal,
                    order.Status
                });
            }

            // Deduct from user wallet if wallet payment
            if (dto.PaymentMethod == "Wallet")
            {
                account.WalletBalance -= totalAmount;

                // Record user wallet transaction
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletOwnerId = account.Id,
                    WalletType = "User",
                    Amount = -totalAmount,
                    TransactionType = "Purchase",
                    Description = "Order purchase",
                    BalanceAfter = account.WalletBalance,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Clear cart items
            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Orders created successfully.",
                Orders = createdOrders,
                TotalAmount = totalAmount,
                RemainingBalance = account.WalletBalance
            });
        }

        /// <summary>
        /// Get all orders for the current user.
        /// </summary>
        [HttpGet("orders")]
        [Authorize]
        public async Task<IActionResult> GetOrders()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .Include(o => o.Shop)
                .Include(o => o.ShippingAddress)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDetailDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    ShopId = o.ShopId,
                    ShopName = o.Shop.ShopName,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    Note = o.Note,
                    ShippingAddress = o.ShippingAddress != null ? new AddressDto
                    {
                        Id = o.ShippingAddress.Id,
                        ReceiverName = o.ShippingAddress.ReceiverName,
                        Phone = o.ShippingAddress.Phone,
                        Street = o.ShippingAddress.Street,
                        Ward = o.ShippingAddress.Ward,
                        District = o.ShippingAddress.District,
                        City = o.ShippingAddress.City,
                        IsDefault = o.ShippingAddress.IsDefault
                    } : null,
                    Items = o.Items.Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        ProductImageUrl = oi.Product.ImageUrl,
                        Quantity = oi.Quantity,
                        PriceAtPurchase = oi.PriceAtPurchase
                    }).ToList(),
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        /// <summary>
        /// Get order detail by ID.
        /// </summary>
        [HttpGet("orders/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .Include(o => o.Shop)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound(new { Message = "Order not found." });

            return Ok(new OrderDetailDto
            {
                Id = order.Id,
                UserId = order.UserId,
                ShopId = order.ShopId,
                ShopName = order.Shop.ShopName,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                Note = order.Note,
                ShippingAddress = order.ShippingAddress != null ? new AddressDto
                {
                    Id = order.ShippingAddress.Id,
                    ReceiverName = order.ShippingAddress.ReceiverName,
                    Phone = order.ShippingAddress.Phone,
                    Street = order.ShippingAddress.Street,
                    Ward = order.ShippingAddress.Ward,
                    District = order.ShippingAddress.District,
                    City = order.ShippingAddress.City,
                    IsDefault = order.ShippingAddress.IsDefault
                } : null,
                Items = order.Items.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    ProductImageUrl = oi.Product.ImageUrl,
                    Quantity = oi.Quantity,
                    PriceAtPurchase = oi.PriceAtPurchase
                }).ToList(),
                CreatedAt = order.CreatedAt
            });
        }

        /// <summary>
        /// Customer cancels an order (only if Pending).
        /// </summary>
        [HttpPost("orders/{id:guid}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound(new { Message = "Order not found." });

            if (order.Status != "Pending")
                return BadRequest(new { Message = "Can only cancel pending orders." });

            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;

            // Restore stock
            foreach (var item in order.Items)
            {
                item.Product.Stock += item.Quantity;
            }

            // Refund if already paid
            if (order.PaymentStatus == "Paid" && order.PaymentMethod == "Wallet")
            {
                var account = await _context.Accounts.FindAsync(userId);
                if (account != null)
                {
                    account.WalletBalance += order.TotalAmount;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        Id = Guid.NewGuid(),
                        WalletOwnerId = account.Id,
                        WalletType = "User",
                        Amount = order.TotalAmount,
                        TransactionType = "Refund",
                        Description = $"Refund for cancelled order {order.Id}",
                        BalanceAfter = account.WalletBalance,
                        ReferenceId = order.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Deduct from shop wallet
                var shop = await _context.Shops.FindAsync(order.ShopId);
                if (shop != null)
                {
                    shop.WalletBalance -= order.TotalAmount;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        Id = Guid.NewGuid(),
                        WalletOwnerId = shop.Id,
                        WalletType = "Shop",
                        Amount = -order.TotalAmount,
                        TransactionType = "Refund",
                        Description = $"Refund for cancelled order {order.Id}",
                        BalanceAfter = shop.WalletBalance,
                        ReferenceId = order.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                order.PaymentStatus = "Refunded";
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order cancelled successfully." });
        }

        /// <summary>
        /// Customer confirms receipt.
        /// </summary>
        [HttpPost("orders/{id:guid}/confirm-received")]
        [Authorize]
        public async Task<IActionResult> ConfirmReceived(Guid id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound(new { Message = "Order not found." });

            if (order.Status != "Shipping")
                return BadRequest(new { Message = "Can only confirm received for orders in shipping status." });

            order.Status = "Completed";
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order confirmed as received." });
        }

        // ===== Shop Orders =====

        /// <summary>
        /// Shop owner views their shop's orders.
        /// </summary>
        [HttpGet("shop/orders")]
        [Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> GetShopOrders()
        {
            var shopId = await GetShopIdForUser();
            if (shopId == null) return NotFound(new { Message = "Shop not found." });

            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .Where(o => o.ShopId == shopId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    CustomerName = o.User.FullName != "" ? o.User.FullName : o.User.Username,
                    o.TotalAmount,
                    o.Status,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.Note,
                    ShippingAddress = o.ShippingAddress != null ? new AddressDto
                    {
                        Id = o.ShippingAddress.Id,
                        ReceiverName = o.ShippingAddress.ReceiverName,
                        Phone = o.ShippingAddress.Phone,
                        Street = o.ShippingAddress.Street,
                        Ward = o.ShippingAddress.Ward,
                        District = o.ShippingAddress.District,
                        City = o.ShippingAddress.City
                    } : null,
                    Items = o.Items.Select(oi => new OrderItemDto
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        ProductImageUrl = oi.Product.ImageUrl,
                        Quantity = oi.Quantity,
                        PriceAtPurchase = oi.PriceAtPurchase
                    }).ToList(),
                    o.CreatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        /// <summary>
        /// Shop views individual order detail.
        /// </summary>
        [HttpGet("shop/orders/{id:guid}")]
        [Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> GetShopOrderById(Guid id)
        {
            var shopId = await GetShopIdForUser();
            if (shopId == null) return NotFound(new { Message = "Shop not found." });

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == id && o.ShopId == shopId);

            if (order == null)
                return NotFound(new { Message = "Order not found." });

            return Ok(new
            {
                order.Id,
                order.UserId,
                CustomerName = order.User.FullName != "" ? order.User.FullName : order.User.Username,
                order.TotalAmount,
                order.Status,
                order.PaymentMethod,
                order.PaymentStatus,
                order.Note,
                ShippingAddress = order.ShippingAddress != null ? new AddressDto
                {
                    Id = order.ShippingAddress.Id,
                    ReceiverName = order.ShippingAddress.ReceiverName,
                    Phone = order.ShippingAddress.Phone,
                    Street = order.ShippingAddress.Street,
                    Ward = order.ShippingAddress.Ward,
                    District = order.ShippingAddress.District,
                    City = order.ShippingAddress.City
                } : null,
                Items = order.Items.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    ProductImageUrl = oi.Product.ImageUrl,
                    Quantity = oi.Quantity,
                    PriceAtPurchase = oi.PriceAtPurchase
                }).ToList(),
                order.CreatedAt
            });
        }

        /// <summary>
        /// Shop updates order status: Pending → Confirmed → Shipping → Completed / Cancelled.
        /// </summary>
        [HttpPut("shop/orders/{id:guid}/status")]
        [Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            var shopId = await GetShopIdForUser();
            if (shopId == null) return NotFound(new { Message = "Shop not found." });

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.ShopId == shopId);

            if (order == null)
                return NotFound(new { Message = "Order not found." });

            // Valid transitions
            var validTransitions = new Dictionary<string, string[]>
            {
                { "Pending", new[] { "Confirmed", "Cancelled" } },
                { "Confirmed", new[] { "Shipping", "Cancelled" } },
                { "Shipping", new[] { "Completed" } }
            };

            if (!validTransitions.ContainsKey(order.Status) ||
                !validTransitions[order.Status].Contains(dto.Status))
            {
                return BadRequest(new { Message = $"Invalid status transition from '{order.Status}' to '{dto.Status}'." });
            }

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            // Handle cancellation by shop (refund)
            if (dto.Status == "Cancelled")
            {
                foreach (var item in order.Items)
                    item.Product.Stock += item.Quantity;

                if (order.PaymentStatus == "Paid" && order.PaymentMethod == "Wallet")
                {
                    var customer = await _context.Accounts.FindAsync(order.UserId);
                    if (customer != null)
                    {
                        customer.WalletBalance += order.TotalAmount;
                        _context.WalletTransactions.Add(new WalletTransaction
                        {
                            Id = Guid.NewGuid(),
                            WalletOwnerId = customer.Id,
                            WalletType = "User",
                            Amount = order.TotalAmount,
                            TransactionType = "Refund",
                            Description = $"Refund for cancelled order {order.Id}",
                            BalanceAfter = customer.WalletBalance,
                            ReferenceId = order.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    var shop = await _context.Shops.FindAsync(order.ShopId);
                    if (shop != null)
                    {
                        shop.WalletBalance -= order.TotalAmount;
                        _context.WalletTransactions.Add(new WalletTransaction
                        {
                            Id = Guid.NewGuid(),
                            WalletOwnerId = shop.Id,
                            WalletType = "Shop",
                            Amount = -order.TotalAmount,
                            TransactionType = "Refund",
                            Description = $"Refund for cancelled order {order.Id}",
                            BalanceAfter = shop.WalletBalance,
                            ReferenceId = order.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    order.PaymentStatus = "Refunded";
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Order status updated to '{dto.Status}'." });
        }

        // ===== Admin Orders =====

        /// <summary>
        /// Admin gets all orders.
        /// </summary>
        [HttpGet("admin/orders")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .Include(o => o.Shop)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    CustomerName = o.User.Username,
                    o.ShopId,
                    ShopName = o.Shop.ShopName,
                    o.TotalAmount,
                    o.Status,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.CreatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }

        private async Task<Guid?> GetShopIdForUser()
        {
            var userId = GetUserId();
            if (userId == null) return null;

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop != null) return shop.Id;

            var staffRecord = await _context.ShopStaff.FirstOrDefaultAsync(ss => ss.AccountId == userId);
            if (staffRecord != null) return staffRecord.ShopId;

            return null;
        }
    }
}
