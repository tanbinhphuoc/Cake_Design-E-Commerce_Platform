using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// DTO để Admin tạo tài khoản mới (Shipper, SystemStaff, Staff, v.v.)
    /// </summary>
    public class AdminCreateAccountDto
    {
        [Required(ErrorMessage = "Username is required.")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [MinLength(10)]
        [MaxLength(10)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;
    }
}
