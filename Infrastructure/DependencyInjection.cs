using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Infrastructure
{
    public static class DependencyInjection
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
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                    mysqlOptions.SchemaBehavior(MySqlSchemaBehavior.Ignore);
                });

                if (config.GetValue<bool>("EnableDebugLogging", false))
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            // Đăng ký Infrastructure services (Repositories) ở đây
            // Ví dụ:
            // services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            // services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
