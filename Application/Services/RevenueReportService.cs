using Application.DTOs;
using Application.Interfaces;

namespace Application.Services
{
    public class RevenueReportService : IRevenueReportService
    {
        private readonly IUnitOfWork _uow;
        public RevenueReportService(IUnitOfWork uow) { _uow = uow; }

        public async Task<ShopRevenueDto> GetShopRevenueAsync(Guid ownerId)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new ArgumentException("Shop not found.");

            var orders = (await _uow.Orders.FindAsync(o => o.ShopId == shop.Id)).ToList();
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.AddDays(-(int)now.DayOfWeek).Date;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var completedOrders = orders.Where(o => o.Status == "Completed" || o.Status == "Delivered").ToList();
            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            var totalCommission = totalRevenue * shop.CommissionRate / 100m;

            return new ShopRevenueDto
            {
                ShopId = shop.Id,
                ShopName = shop.ShopName,
                TotalRevenue = totalRevenue,
                TotalCommission = Math.Round(totalCommission, 0),
                NetRevenue = totalRevenue - Math.Round(totalCommission, 0),
                TotalOrders = orders.Count,
                CompletedOrders = completedOrders.Count,
                CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                TodayRevenue = completedOrders.Where(o => o.CreatedAt >= todayStart).Sum(o => o.TotalAmount),
                WeekRevenue = completedOrders.Where(o => o.CreatedAt >= weekStart).Sum(o => o.TotalAmount),
                MonthRevenue = completedOrders.Where(o => o.CreatedAt >= monthStart).Sum(o => o.TotalAmount)
            };
        }

        public async Task<SystemRevenueDto> GetSystemRevenueAsync()
        {
            var orders = (await _uow.Orders.GetAllAsync()).ToList();
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.AddDays(-(int)now.DayOfWeek).Date;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var completedOrders = orders.Where(o => o.Status == "Completed" || o.Status == "Delivered").ToList();
            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);

            // Get commission wallet balance
            var commissionWallet = await _uow.SystemWallets.FirstOrDefaultAsync(w => w.WalletType == "Commission");
            var totalCommission = commissionWallet?.Balance ?? 0;

            // Calculate total system coupon discount
            var totalDiscount = orders.Where(o => o.SystemCouponId != null).Sum(o => o.DiscountAmount);

            // Calculate refunds
            var refundedOrders = orders.Where(o => o.PaymentStatus == "Refunded").ToList();
            var totalRefunds = refundedOrders.Sum(o => o.TotalAmount);

            return new SystemRevenueDto
            {
                TotalRevenue = totalRevenue,
                TotalCommission = totalCommission,
                TotalDiscount = totalDiscount,
                TotalRefunds = totalRefunds,
                TotalOrders = orders.Count,
                CompletedOrders = completedOrders.Count,
                PendingOrders = orders.Count(o => o.Status == "Pending"),
                CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                TodayRevenue = completedOrders.Where(o => o.CreatedAt >= todayStart).Sum(o => o.TotalAmount),
                WeekRevenue = completedOrders.Where(o => o.CreatedAt >= weekStart).Sum(o => o.TotalAmount),
                MonthRevenue = completedOrders.Where(o => o.CreatedAt >= monthStart).Sum(o => o.TotalAmount)
            };
        }
    }
}
