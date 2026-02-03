using Infrastructure;
using Microsoft.EntityFrameworkCore;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("MySqlConnection")
            ?? throw new InvalidOperationException("Connection string 'MySqlConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
        {
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            options.UseMySql(connectionString, serverVersion, mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                mysqlOptions.SchemaBehavior(Pomelo.EntityFrameworkCore.MySql.Infrastructure.MySqlSchemaBehavior.Ignore);
            });

            if (config.GetValue<bool>("EnableDebugLogging", false)) // hoặc check Environment.IsDevelopment()
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Đăng ký service + repo ở đây sau
        // services.AddScoped<IProductService, ProductService>();
        // services.AddScoped<IProductRepository, ProductRepository>();
        return services;
    }
}