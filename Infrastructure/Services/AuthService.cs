using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(AppDbContext context, IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Check if username already exists
            var existingUser = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == dto.Username);

            if (existingUser != null)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create new account
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                PasswordHash = passwordHash,
                Role = "Customer",
                IsApproved = false,
                WalletBalance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);

            // Create an empty cart for the new user
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = account.Id
            };

            _context.Carts.Add(cart);

            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtTokenGenerator.GenerateToken(account);

            return new AuthResponseDto
            {
                Token = token,
                Username = account.Username,
                Role = account.Role
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // Find user by username
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == dto.Username);

            if (account == null)
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            // Generate JWT token
            var token = _jwtTokenGenerator.GenerateToken(account);

            return new AuthResponseDto
            {
                Token = token,
                Username = account.Username,
                Role = account.Role
            };
        }
    }
}
