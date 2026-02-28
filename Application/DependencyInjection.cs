using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IShopService, ShopService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWishlistService, WishlistService>();
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<ICategoryTagService, CategoryTagService>();
            services.AddScoped<IShipperService, ShipperService>();

            return services;
        }
    }
}
