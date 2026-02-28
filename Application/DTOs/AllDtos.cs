namespace Application.DTOs
{
    // ===== User Profile =====
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public decimal WalletBalance { get; set; }
        public Guid? DefaultAddressId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateUserProfileDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public Guid? DefaultAddressId { get; set; }
    }

    // ===== Address =====
    public class AddressDto
    {
        public Guid Id { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
        public bool IsDefault { get; set; }
    }

    public class CreateAddressDto
    {
        public string ReceiverName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
        public bool IsDefault { get; set; } = false;
    }

    public class UpdateAddressDto
    {
        public string? ReceiverName { get; set; }
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
        public bool? IsDefault { get; set; }
    }

    // ===== Shop =====
    public class ShopProfileDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateShopDto
    {
        public string? ShopName { get; set; }
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? Address { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }
        public string? Phone { get; set; }
    }

    public class ShopPublicDto
    {
        public Guid Id { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ===== Shop Staff =====
    public class AddStaffDto
    {
        public Guid AccountId { get; set; }
        public string Role { get; set; } = "Staff";
    }

    public class StaffDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ===== Category & Tag =====
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
    }

    public class UpdateCategoryDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? SortOrder { get; set; }
        public bool? IsActive { get; set; }
    }

    public class TagDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CreateTagDto
    {
        public string Name { get; set; } = string.Empty;
    }

    // ===== Product (extended) =====
    public class ProductDetailDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProductDto
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? CategoryId { get; set; }
        public int? Stock { get; set; }
        public bool? IsActive { get; set; }
        public List<Guid>? TagIds { get; set; }
    }

    public class CreateProductExtendedDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public int Stock { get; set; } = 0;
        public List<Guid>? TagIds { get; set; }
    }

    // ===== Cart (extended) =====
    public class UpdateCartItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // ===== Order (extended) =====
    public class CreateOrderDto
    {
        public List<Guid>? CartItemIds { get; set; } // null = all items in cart
        public Guid? ShippingAddressId { get; set; }
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "Wallet";
    }

    public class OrderDetailDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public Guid? ShipperId { get; set; }
        public string? ShipperName { get; set; }
        public decimal ItemsAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ShippingProvider { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? Note { get; set; }
        public AddressDto? ShippingAddress { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PriceAtPurchase { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    // ===== Shipper =====
    public class ShipperOrderDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public AddressDto? ShippingAddress { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    // ===== Refund =====
    public class CreateRefundRequestDto
    {
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EvidenceUrls { get; set; } // JSON array of image URLs
    }

    public class RefundRequestDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal OrderAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? EvidenceUrls { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? StaffNote { get; set; }
        public Guid? ResolvedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class ResolveRefundDto
    {
        public bool Approved { get; set; }
        public string? StaffNote { get; set; }
    }

    // ===== Review =====
    public class CreateReviewDto
    {
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ===== Wishlist =====
    public class WishlistItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
    }

    // ===== Payment =====
    public class CreatePaymentDto
    {
        public Guid OrderId { get; set; }
        public string Method { get; set; } = "Wallet";
    }

    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? TransactionRef { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ===== Wallet =====
    public class WalletDto
    {
        public decimal Balance { get; set; }
        public string OwnerType { get; set; } = string.Empty;
    }

    public class WalletTransactionDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BalanceAfter { get; set; }
        public Guid? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ===== Report =====
    public class CreateReportDto
    {
        public string TargetType { get; set; } = string.Empty; // "Product", "Shop"
        public Guid TargetId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ReportDto
    {
        public Guid Id { get; set; }
        public Guid ReporterId { get; set; }
        public string ReporterUsername { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public Guid TargetId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class UpdateReportDto
    {
        public string Status { get; set; } = string.Empty; // "Reviewed", "Resolved", "Dismissed"
        public string? AdminNote { get; set; }
    }

    // ===== Search =====
    public class ProductSearchDto
    {
        public string? Keyword { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? ShopId { get; set; }
        public Guid? TagId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Sort { get; set; } // "price_asc", "price_desc", "newest", "rating", "popular"
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PaginatedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    // ===== Admin Stats =====
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalShops { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NewUsersToday { get; set; }
        public int OrdersToday { get; set; }
        public decimal RevenueToday { get; set; }
        public int PendingReports { get; set; }
        public int PendingShopRequests { get; set; }
    }

    // ===== Address Lookup =====
    public class ProvinceDto
    {
        public int ProvinceId { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
    }

    public class DistrictDto
    {
        public int DistrictId { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public int ProvinceId { get; set; }
    }

    public class WardDto
    {
        public string WardId { get; set; } = string.Empty;
        public string WardName { get; set; } = string.Empty;
        public int DistrictId { get; set; }
    }
}
