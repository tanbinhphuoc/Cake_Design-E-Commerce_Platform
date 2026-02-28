using System;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using BCrypt.Net;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IRedisCacheService _redis;
        private readonly IRefreshTokenService _refreshTokens;
        private readonly IEmailSender _emailSender;
        private readonly TimeSpan _refreshLifetime;
        private readonly string _googleClientId;

        public AuthService(
            IUnitOfWork unitOfWork,
            IJwtTokenGenerator jwtTokenGenerator,
            IRedisCacheService redis,
            IRefreshTokenService refreshTokens,
            IEmailSender emailSender,
            IConfiguration config)
        {
            _unitOfWork = unitOfWork;
            _jwtTokenGenerator = jwtTokenGenerator;
            _redis = redis;
            _refreshTokens = refreshTokens;
            _emailSender = emailSender;
            _refreshLifetime = TimeSpan.FromDays(int.TryParse(config["RefreshToken:ExpirationDays"], out var d) ? d : 7);
            _googleClientId = config["GoogleAuth:ClientId"] ?? string.Empty;
        }

        public async Task RequestEmailOtpAsync(RequestEmailOtpDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new InvalidOperationException("Email is required.");

            // (Tùy ngữ cảnh) kiểm tra email đã tồn tại hay chưa
            var existingEmail = await _unitOfWork.Accounts.FirstOrDefaultAsync(a => a.Email == dto.Email);
            // Ví dụ: nếu là luồng đăng ký, không muốn cho email đã đăng ký:
            if (existingEmail != null)
                throw new InvalidOperationException("Email đã được sử dụng.");

            // Rate limit 60s
            var throttleKey = $"otp:sent:{dto.Email}";
            if (await _redis.GetStringAsync(throttleKey) != null)
                throw new InvalidOperationException("Vui lòng thử lại sau 60 giây.");

            await _redis.SetStringAsync(throttleKey, "1", TimeSpan.FromSeconds(60));

            // Giới hạn số lần/ngày (ví dụ 5 lần)
            var countKey = $"otp:count:{dto.Email}:{DateTime.UtcNow:yyyyMMdd}";
            var countStr = await _redis.GetStringAsync(countKey);
            var count = countStr is null ? 0 : int.Parse(countStr);
            if (count >= 5)
                throw new InvalidOperationException("Bạn đã vượt quá số lần gửi OTP hôm nay.");
            await _redis.SetStringAsync(countKey, (count + 1).ToString(), TimeSpan.FromDays(1));

            // Sinh và lưu OTP
            var otp = Random.Shared.Next(100000, 999999).ToString();
            await _redis.SetStringAsync($"otp:email:{dto.Email}", otp, TimeSpan.FromMinutes(5));
            await _emailSender.SendAsync(dto.Email, "Your OTP", $"Your OTP is {otp} (valid 5 minutes)");
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // basic validate
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Length < 3)
                throw new InvalidOperationException("Username must be at least 3 characters.");
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                throw new InvalidOperationException("Password must be at least 6 characters.");
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new InvalidOperationException("Email is required.");
            if (string.IsNullOrWhiteSpace(dto.Otp))
                throw new InvalidOperationException("OTP is required.");

            // OTP check
            var otpStored = await _redis.GetStringAsync($"otp:email:{dto.Email}");
            if (otpStored == null || !otpStored.Equals(dto.Otp, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid or expired OTP");
            await _redis.RemoveAsync($"otp:email:{dto.Email}");

            // uniqueness
            var existingUser = await _unitOfWork.Accounts.GetByUsernameAsync(dto.Username);
            if (existingUser != null) throw new InvalidOperationException("Username already exists.");
            var existingEmail = await _unitOfWork.Accounts.FirstOrDefaultAsync(a => a.Email == dto.Email);
            if (existingEmail != null) throw new InvalidOperationException("Email already in use.");

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
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
            await _unitOfWork.Carts.AddAsync(new Cart { Id = Guid.NewGuid(), UserId = account.Id });
            await _unitOfWork.SaveChangesAsync();

            return await IssueTokens(account);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var account = await _unitOfWork.Accounts.GetByUsernameAsync(dto.Username);
            if (account == null || !BCrypt.Net.BCrypt.Verify(dto.Password, account.PasswordHash))
                throw new UnauthorizedAccessException("Invalid username or password.");
            return await IssueTokens(account);
        }

        public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto dto)
        {
            var info = await _refreshTokens.GetAsync(dto.RefreshToken);
            if (info == null || info.ExpiresAtUtc <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            var account = await _unitOfWork.Accounts.GetByIdAsync(info.UserId)
                          ?? throw new UnauthorizedAccessException("User not found.");

            // rotate refresh token
            await _refreshTokens.InvalidateAsync(dto.RefreshToken);
            return await IssueTokens(account);
        }

        public async Task LogoutAsync(Guid userId, string? accessJti)
        {
            if (!string.IsNullOrWhiteSpace(accessJti))
            {
                await _redis.SetStringAsync($"auth:bl:{userId}:{accessJti}", "1", TimeSpan.FromHours(1));
                await _redis.RemoveAsync($"auth:jti:{accessJti}");
            }
            await _refreshTokens.InvalidateAllForUserAsync(userId);
        }

        public async Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.IdToken))
                throw new UnauthorizedAccessException("Missing Google id_token.");

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[]
    {
        "198799505981-e8i090ffqfd0q0ki8u5huq979e8o6mto.apps.googleusercontent.com"    }
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            catch
            {
                throw new UnauthorizedAccessException("Invalid Google token.");
            }

            var email = payload.Email;
            var name = payload.Name ?? payload.GivenName ?? payload.FamilyName ?? email;
            var picture = payload.Picture;
            if (string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedAccessException("Google account missing email.");

            var account = await _unitOfWork.Accounts.FirstOrDefaultAsync(a => a.Email == email);
            if (account == null)
            {
                account = new Account
                {
                    Id = Guid.NewGuid(),
                    Username = email,
                    PasswordHash = string.Empty,
                    FullName = name,
                    Email = email,
                    Phone = string.Empty,
                    Role = "Customer",
                    IsApproved = true,
                    WalletBalance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AvatarUrl = picture // nếu có field này
                };
                await _unitOfWork.Accounts.AddAsync(account);
                await _unitOfWork.Carts.AddAsync(new Cart { Id = Guid.NewGuid(), UserId = account.Id });
                await _unitOfWork.SaveChangesAsync();
            }

            return await IssueTokens(account);
        }

        // Helper: issue access + refresh
        private async Task<AuthResponseDto> IssueTokens(Account account)
        {
            var tokenResult = _jwtTokenGenerator.GenerateToken(account);
            var ttl = tokenResult.ExpiresAtUtc - DateTime.UtcNow;
            await _redis.SetStringAsync($"auth:jti:{tokenResult.Jti}", account.Id.ToString(), ttl);

            var rt = await _refreshTokens.CreateAsync(account.Id, _refreshLifetime);

            return new AuthResponseDto
            {
                Token = tokenResult.Token,
                Username = account.Username,
                Role = account.Role,
                ExpiresAtUtc = tokenResult.ExpiresAtUtc,
                RefreshToken = $"{rt.UserId}:{rt.TokenId}",
                RefreshTokenExpiresAtUtc = rt.ExpiresAtUtc
            };
        }
    }
}