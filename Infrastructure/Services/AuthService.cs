using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using BCrypt.Net;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
using Application.Exceptions;
// Thêm namespace chứa các Custom Exception của bạn vào đây (ví dụ: Application.Exceptions)
// using Application.Exceptions; 

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
                throw new BadRequestException("Email is required."); // 400

            var throttleKey = $"otp:sent:{dto.Email}";
            if (await _redis.GetStringAsync(throttleKey) != null)
                throw new TooManyRequestsException("Vui lòng thử lại sau 60 giây."); // 429

            var countKey = $"otp:count:{dto.Email}:{DateTime.UtcNow:yyyyMMdd}";
            var countStr = await _redis.GetStringAsync(countKey);
            var count = countStr is null ? 0 : int.Parse(countStr);
            if (count >= 5)
                throw new TooManyRequestsException("Bạn đã vượt quá số lần gửi OTP hôm nay."); // 429

            var existingEmail = await _unitOfWork.Accounts.FirstOrDefaultAsync(a => a.Email == dto.Email);
            if (existingEmail != null)
                throw new ConflictException("Email đã được sử dụng."); // 409

            await _redis.SetStringAsync(throttleKey, "1", TimeSpan.FromSeconds(60));
            await _redis.SetStringAsync(countKey, (count + 1).ToString(), TimeSpan.FromDays(1));

            var otp = Random.Shared.Next(100000, 999999).ToString();
            await _redis.SetStringAsync($"otp:email:{dto.Email}", otp, TimeSpan.FromMinutes(5));
            await _emailSender.SendAsync(dto.Email, "Your OTP", $"Your OTP is {otp} (valid 5 minutes)");
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Length < 3)
                throw new BadRequestException("Username must be at least 3 characters."); // 400
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                throw new BadRequestException("Password must be at least 6 characters."); // 400
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new BadRequestException("Email is required."); // 400
            if (string.IsNullOrWhiteSpace(dto.Otp))
                throw new BadRequestException("OTP is required."); // 400

            var otpStored = await _redis.GetStringAsync($"otp:email:{dto.Email}");
            if (otpStored == null || !otpStored.Equals(dto.Otp, StringComparison.Ordinal))
                throw new BadRequestException("Invalid or expired OTP"); // 400

            await _redis.RemoveAsync($"otp:email:{dto.Email}");

            var existingUser = await _unitOfWork.Accounts.GetByUsernameAsync(dto.Username);
            if (existingUser != null)
                throw new ConflictException("Username already exists."); // 409

            var existingEmail = await _unitOfWork.Accounts.FirstOrDefaultAsync(a => a.Email == dto.Email);
            if (existingEmail != null)
                throw new ConflictException("Email already in use."); // 409

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
                throw new UnauthorizedAccessException("Invalid username or password."); // 401 (Dùng ngoại lệ có sẵn của C# là ổn)

            return await IssueTokens(account);
        }

        public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto dto)
        {
            var info = await _refreshTokens.GetAsync(dto.RefreshToken);
            if (info == null || info.ExpiresAtUtc <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token."); // 401

            var account = await _unitOfWork.Accounts.GetByIdAsync(info.UserId)
                          ?? throw new UnauthorizedAccessException("User not found."); // 401 (Lỗi token map với 401 hợp lý hơn 404)

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
                throw new BadRequestException("Missing Google id_token."); // 400

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleClientId }
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            catch
            {
                throw new UnauthorizedAccessException("Invalid Google token."); // 401
            }

            var email = payload.Email;
            var name = payload.Name ?? payload.GivenName ?? payload.FamilyName ?? email;
            var picture = payload.Picture;

            if (string.IsNullOrWhiteSpace(email))
                throw new BadRequestException("Google account missing email."); // 400

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
                    AvatarUrl = picture
                };
                await _unitOfWork.Accounts.AddAsync(account);
                await _unitOfWork.Carts.AddAsync(new Cart { Id = Guid.NewGuid(), UserId = account.Id });
                await _unitOfWork.SaveChangesAsync();
            }

            return await IssueTokens(account);
        }

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

        public async Task RequestPasswordResetOtpAsync(ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new BadRequestException("Email is required."); // 400

            var throttleKey = $"otp:reset:sent:{dto.Email}";
            if (await _redis.GetStringAsync(throttleKey) != null)
                throw new TooManyRequestsException("Vui lòng thử lại sau 60 giây."); // 429

            var countKey = $"otp:reset:count:{dto.Email}:{DateTime.UtcNow:yyyyMMdd}";
            var countStr = await _redis.GetStringAsync(countKey);
            var count = countStr is null ? 0 : int.Parse(countStr);
            if (count >= 5)
                throw new TooManyRequestsException("Bạn đã vượt quá số lần gửi OTP hôm nay."); // 429

            var user = await _unitOfWork.Accounts.FirstOrDefaultAsync(a => a.Email == dto.Email);

            if (user == null)
                return; // Im lặng kết thúc để bảo mật

            await _redis.SetStringAsync(throttleKey, "1", TimeSpan.FromSeconds(60));
            await _redis.SetStringAsync(countKey, (count + 1).ToString(), TimeSpan.FromDays(1));

            var otp = Random.Shared.Next(100000, 999999).ToString();
            await _redis.SetStringAsync($"otp:reset:email:{dto.Email}", otp, TimeSpan.FromMinutes(5));
            await _emailSender.SendAsync(dto.Email, "OTP đặt lại mật khẩu", $"Mã OTP: {otp} (hiệu lực 5 phút)");
        }

        public async Task ResetPasswordAsync(ForgotPasswordResetDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Otp) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
                throw new BadRequestException("Thiếu thông tin yêu cầu."); // 400

            var otpStored = await _redis.GetStringAsync($"otp:reset:email:{dto.Email}");
            if (otpStored == null || !otpStored.Equals(dto.Otp, StringComparison.Ordinal))
                throw new BadRequestException("OTP không đúng hoặc đã hết hạn."); // 400

            var user = await _unitOfWork.Accounts.FirstOrDefaultAsync(a => a.Email == dto.Email);
            if (user == null)
                throw new NotFoundException("Tài khoản không tồn tại."); // 404

            if (dto.NewPassword.Length < 6)
                throw new BadRequestException("Mật khẩu phải ít nhất 6 ký tự."); // 400

            await _redis.RemoveAsync($"otp:reset:email:{dto.Email}");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            await _refreshTokens.InvalidateAllForUserAsync(user.Id);
        }
    }
}