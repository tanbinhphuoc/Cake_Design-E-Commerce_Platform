using Application.Interfaces;
using Application.Services;
using Infrastructure.Repositories;
using Infrastructure.Services;
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

            // Register UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register Infrastructure services
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IVnPayService, VnPayService>();
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

            // Register Viettel Post Service with HttpClient
            services.AddHttpClient<IViettelPostService, ViettelPostService>();

            return services;
        }
    }
}
