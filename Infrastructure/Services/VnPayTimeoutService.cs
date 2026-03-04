using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Background service that auto-cancels unpaid VNPay orders after 15 minutes.
    /// Runs every 2 minutes to check for expired VNPay pending orders.
    /// </summary>
    public class VnPayTimeoutService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<VnPayTimeoutService> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan PaymentTimeout = TimeSpan.FromMinutes(15);

        public VnPayTimeoutService(IServiceProvider services, ILogger<VnPayTimeoutService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[VnPayTimeout] Service started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndCancelExpiredOrders(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[VnPayTimeout] Error checking expired orders.");
                }
                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CheckAndCancelExpiredOrders(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var cutoff = DateTime.UtcNow.Subtract(PaymentTimeout);
            var expiredOrders = await uow.Orders.FindAsync(o =>
                o.PaymentMethod == "VNPay" &&
                o.PaymentStatus == "Pending" &&
                o.Status == "Pending" &&
                o.CreatedAt <= cutoff);

            var count = 0;
            foreach (var order in expiredOrders)
            {
                var fullOrder = await uow.Orders.GetByIdWithItemsAsync(order.Id);
                if (fullOrder == null) continue;

                fullOrder.Status = "Cancelled";
                fullOrder.PaymentStatus = "Expired";
                fullOrder.UpdatedAt = DateTime.UtcNow;

                // Restore stock
                foreach (var item in fullOrder.Items)
                    item.Product.Stock += item.Quantity;

                // Mark payment as expired
                var payment = await uow.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
                if (payment != null)
                {
                    payment.Status = "Expired";
                    payment.CompletedAt = DateTime.UtcNow;
                }

                count++;
            }

            if (count > 0)
            {
                await uow.SaveChangesAsync();
                _logger.LogInformation("[VnPayTimeout] Cancelled {Count} expired VNPay orders.", count);
            }
        }
    }
}
