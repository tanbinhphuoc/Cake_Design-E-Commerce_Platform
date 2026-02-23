using Application.DTOs;

namespace Application.Services
{
    public interface IUserService
    {
        Task<UserProfileDto?> GetProfileAsync(Guid userId);
        Task<string> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto);
        Task<List<AddressDto>> GetAddressesAsync(Guid userId);
        Task<Guid> CreateAddressAsync(Guid userId, CreateAddressDto dto);
        Task<string> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressDto dto);
        Task<string> DeleteAddressAsync(Guid userId, Guid addressId);
        Task<string> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    }
}
