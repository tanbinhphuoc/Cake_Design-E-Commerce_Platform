using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(IUnitOfWork unitOfWork, IJwtTokenGenerator jwtTokenGenerator)
        {
            _unitOfWork = unitOfWork;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Length < 3)
                throw new InvalidOperationException("Username must be at least 3 characters.");
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                throw new InvalidOperationException("Password must be at least 6 characters.");
            if (string.IsNullOrWhiteSpace(dto.FullName))
                throw new InvalidOperationException("Full name is required.");
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new InvalidOperationException("Email is required.");
            if (string.IsNullOrWhiteSpace(dto.Phone))
                throw new InvalidOperationException("Phone is required.");

            // Check if username already exists
            var existingUser = await _unitOfWork.Accounts.GetByUsernameAsync(dto.Username);
            if (existingUser != null)
                throw new InvalidOperationException("Username already exists.");

            // Check if email already exists
            var existingEmail = await _unitOfWork.Accounts.FirstOrDefaultAsync(a => a.Email == dto.Email);
            if (existingEmail != null)
                throw new InvalidOperationException("Email already in use.");

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create new account
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                PasswordHash = passwordHash,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Role = "Customer",
                IsApproved = true,
                WalletBalance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Accounts.AddAsync(account);

            // Create an empty cart for the new user
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = account.Id
            };

            await _unitOfWork.Carts.AddAsync(cart);
            await _unitOfWork.SaveChangesAsync();

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
            var account = await _unitOfWork.Accounts.GetByUsernameAsync(dto.Username);
            if (account == null)
                throw new UnauthorizedAccessException("Invalid username or password.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash))
                throw new UnauthorizedAccessException("Invalid username or password.");

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
