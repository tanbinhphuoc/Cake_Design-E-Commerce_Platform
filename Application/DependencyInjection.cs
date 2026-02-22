using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Đăng ký Application services ở đây
            // Ví dụ:
            // services.AddScoped<IProductService, ProductService>();
            return services;
        }
    }
}
