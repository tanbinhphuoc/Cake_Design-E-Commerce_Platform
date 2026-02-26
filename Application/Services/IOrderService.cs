using Application.DTOs;

namespace Application.Services
{
    public class CreateOrderResult
    {
        public List<object> Orders { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal? RemainingBalance { get; set; }
        public string? PaymentUrl { get; set; }
        public bool RequiresPaymentRedirect { get; set; }
    }

    public class VnPayIpnResult
    {
        public string RspCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class VnPayReturnResult
    {
        public bool Success { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public interface IOrderService
    {
        Task<CreateOrderResult> CreateOrderAsync(Guid userId, CreateOrderDto dto, string ipAddress);
        Task<List<OrderDetailDto>> GetOrdersAsync(Guid userId);
        Task<OrderDetailDto?> GetOrderByIdAsync(Guid userId, Guid orderId);
        Task<string> CancelOrderAsync(Guid userId, Guid orderId);
        Task<string> ConfirmReceivedAsync(Guid userId, Guid orderId);

        // Shop owner
        Task<List<object>> GetShopOrdersAsync(Guid shopId);
        Task<object?> GetShopOrderByIdAsync(Guid shopId, Guid orderId);
        Task<string> UpdateOrderStatusAsync(Guid shopId, Guid orderId, UpdateOrderStatusDto dto);

        // Admin
        Task<List<object>> GetAllOrdersAsync();

        // VNPay
        Task<VnPayIpnResult> ProcessVnPayIpnAsync(Dictionary<string, string> vnpayData);
        Task<VnPayReturnResult> ProcessVnPayReturnAsync(Dictionary<string, string> vnpayData);
    }
}
