using Application;
using Application.Services;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var conn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
                var options = ConfigurationOptions.Parse(conn);
                options.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(options);
            });
            builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
            builder.Services.AddSingleton<IRefreshTokenService, RedisRefreshTokenService>();
            builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyHereAtLeast32CharsLong!!!";
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
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CakeDesignPlatform",
                    ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CakeDesignPlatformUsers",
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var redis = context.HttpContext.RequestServices.GetRequiredService<IRedisCacheService>();

                        // "sub" bị auto-map thành ClaimTypes.NameIdentifier
                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        // "jti" bị auto-map thành "jti" HOẶC có thể không map — tìm cả hai
                        var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti)
                               ?? context.Principal?.FindFirstValue("jti");

                        if (string.IsNullOrWhiteSpace(jti) || string.IsNullOrWhiteSpace(userId))
                        {
                            context.Fail("Missing jti or sub");
                            return;
                        }

                        var active = await redis.GetStringAsync($"auth:jti:{jti}");
                        if (active is null)
                        {
                            context.Fail("Token not active");
                            return;
                        }

                        var bl = await redis.GetStringAsync($"auth:bl:{userId}:{jti}");
                        if (bl is not null)
                        {
                            context.Fail("Token is blacklisted");
                        }
                    }
                };
            });

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

            // Seed mock data on startup
            await DataSeeder.SeedAllAsync(app.Services);

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
    }
}
