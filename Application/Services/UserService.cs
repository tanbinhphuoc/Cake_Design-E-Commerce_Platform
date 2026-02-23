using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IPasswordHasher _hasher;
        public UserService(IUnitOfWork uow, IPasswordHasher hasher) { _uow = uow; _hasher = hasher; }

        public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
        {
            var a = await _uow.Accounts.GetByIdAsync(userId);
            if (a == null) return null;
            return new UserProfileDto
            {
                Id = a.Id, Username = a.Username, FullName = a.FullName, Email = a.Email,
                Phone = a.Phone, AvatarUrl = a.AvatarUrl, Role = a.Role, WalletBalance = a.WalletBalance,
                DefaultAddressId = a.DefaultAddressId, CreatedAt = a.CreatedAt
            };
        }

        public async Task<string> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto)
        {
            var a = await _uow.Accounts.GetByIdAsync(userId);
            if (a == null) throw new ArgumentException("User not found.");
            if (dto.FullName != null) a.FullName = dto.FullName;
            if (dto.Email != null) a.Email = dto.Email;
            if (dto.Phone != null) a.Phone = dto.Phone;
            if (dto.AvatarUrl != null) a.AvatarUrl = dto.AvatarUrl;
            if (dto.DefaultAddressId != null)
            {
                var addr = await _uow.Addresses.FirstOrDefaultAsync(ad => ad.Id == dto.DefaultAddressId && ad.UserId == userId);
                if (addr == null) throw new ArgumentException("Address not found or does not belong to you.");
                a.DefaultAddressId = dto.DefaultAddressId;
            }
            a.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return "Profile updated successfully.";
        }

        public async Task<List<AddressDto>> GetAddressesAsync(Guid userId)
        {
            var addresses = await _uow.Addresses.GetByUserIdAsync(userId);
            return addresses.Select(a => new AddressDto
            {
                Id = a.Id, ReceiverName = a.ReceiverName, Phone = a.Phone,
                Street = a.Street, Ward = a.Ward, District = a.District, City = a.City, IsDefault = a.IsDefault
            }).ToList();
        }

        public async Task<Guid> CreateAddressAsync(Guid userId, CreateAddressDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ReceiverName) || string.IsNullOrWhiteSpace(dto.Phone) ||
                string.IsNullOrWhiteSpace(dto.Street) || string.IsNullOrWhiteSpace(dto.City))
                throw new ArgumentException("ReceiverName, Phone, Street, and City are required.");

            if (dto.IsDefault)
            {
                var defaults = await _uow.Addresses.GetDefaultAddressesByUserIdAsync(userId);
                foreach (var d in defaults) d.IsDefault = false;
            }

            var address = new Address
            {
                Id = Guid.NewGuid(), UserId = userId, ReceiverName = dto.ReceiverName, Phone = dto.Phone,
                Street = dto.Street, Ward = dto.Ward, District = dto.District, City = dto.City,
                IsDefault = dto.IsDefault, CreatedAt = DateTime.UtcNow
            };
            await _uow.Addresses.AddAsync(address);
            await _uow.SaveChangesAsync();
            return address.Id;
        }

        public async Task<string> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressDto dto)
        {
            var address = await _uow.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
            if (address == null) throw new ArgumentException("Address not found.");
            if (dto.ReceiverName != null) address.ReceiverName = dto.ReceiverName;
            if (dto.Phone != null) address.Phone = dto.Phone;
            if (dto.Street != null) address.Street = dto.Street;
            if (dto.Ward != null) address.Ward = dto.Ward;
            if (dto.District != null) address.District = dto.District;
            if (dto.City != null) address.City = dto.City;
            if (dto.IsDefault == true)
            {
                var others = (await _uow.Addresses.GetByUserIdAsync(userId)).Where(a => a.IsDefault && a.Id != addressId);
                foreach (var o in others) o.IsDefault = false;
                address.IsDefault = true;
            }
            else if (dto.IsDefault == false) address.IsDefault = false;
            await _uow.SaveChangesAsync();
            return "Address updated successfully.";
        }

        public async Task<string> DeleteAddressAsync(Guid userId, Guid addressId)
        {
            var address = await _uow.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
            if (address == null) throw new ArgumentException("Address not found.");
            _uow.Addresses.Remove(address);
            await _uow.SaveChangesAsync();
            return "Address deleted successfully.";
        }

        public async Task<string> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var account = await _uow.Accounts.GetByIdAsync(userId);
            if (account == null) throw new ArgumentException("User not found.");

            // Verify current password
            if (!_hasher.Verify(dto.CurrentPassword, account.PasswordHash))
                throw new InvalidOperationException("Current password is incorrect.");

            // Validate new password
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                throw new ArgumentException("New password must be at least 6 characters.");

            if (dto.NewPassword != dto.ConfirmPassword)
                throw new ArgumentException("New password and confirm password do not match.");

            if (dto.CurrentPassword == dto.NewPassword)
                throw new ArgumentException("New password must be different from current password.");

            // Hash and save
            account.PasswordHash = _hasher.Hash(dto.NewPassword);
            account.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return "Password changed successfully.";
        }
    }
}
