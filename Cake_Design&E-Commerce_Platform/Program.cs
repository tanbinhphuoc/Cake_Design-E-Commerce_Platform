using Application;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

namespace Cake_Design_E_Commerce_Platform
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            builder.Services.AddEndpointsApiExplorer();

            // Swagger with JWT support
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Cake Design E-Commerce Platform API",
                    Version = "v1",
                    Description = "API for Cake Design & E-Commerce Platform with JWT Authentication"
                });

                // Add JWT Authentication to Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token (without 'Bearer ' prefix)"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // JWT Authentication
            var jwtSecretKey = "YourSuperSecretKeyHereAtLeast32CharsLong!!!";
            var key = Encoding.UTF8.GetBytes(jwtSecretKey);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "CakeDesignPlatform",
                    ValidateAudience = true,
                    ValidAudience = "CakeDesignPlatformUsers",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Register services from each layer
            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureServices(builder.Configuration);

            var app = builder.Build();

            // Seed admin accounts on startup
            await SeedAdminAccounts(app.Services);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// Tự động tạo tài khoản Admin nếu chưa có trong database.
        /// </summary>
        private static async Task SeedAdminAccounts(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Kiểm tra đã có tài khoản Admin chưa
            var hasAdmin = await context.Accounts.AnyAsync(a => a.Role == "Admin");
            if (hasAdmin)
            {
                Console.WriteLine("[Seed] Admin accounts already exist. Skipping seed.");
                return;
            }

            Console.WriteLine("[Seed] No admin accounts found. Creating default admin accounts...");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

            var admins = new List<Account>
            {
                new Account
                {
                    Id = Guid.NewGuid(),
                    Username = "admin1",
                    PasswordHash = passwordHash,
                    FullName = "Administrator 1",
                    Email = "admin1@cakedesign.com",
                    Role = "Admin",
                    IsApproved = true,
                    WalletBalance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Account
                {
                    Id = Guid.NewGuid(),
                    Username = "admin2",
                    PasswordHash = passwordHash,
                    FullName = "Administrator 2",
                    Email = "admin2@cakedesign.com",
                    Role = "Admin",
                    IsApproved = true,
                    WalletBalance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Accounts.AddRange(admins);
            await context.SaveChangesAsync();

            Console.WriteLine("[Seed] Created admin accounts:");
            Console.WriteLine("  - Username: admin1 | Password: 123456");
            Console.WriteLine("  - Username: admin2 | Password: 123456");
        }
    }
}
