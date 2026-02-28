using Application.DTOs;

namespace Application.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto dto);
        Task LogoutAsync(Guid userId, string? accessJti);
        Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginDto dto);
        Task RequestEmailOtpAsync(RequestEmailOtpDto dto);
    }
}
