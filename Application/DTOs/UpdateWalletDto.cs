using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class UpdateWalletDto
    {
        [Required(ErrorMessage = "UserId is required.")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        public decimal Amount { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }
    }

    public class DepositWalletDto
    {
        [Required(ErrorMessage = "Amount is required.")]
        [Range(1000, 100000000, ErrorMessage = "Amount must be between 1,000 and 100,000,000 VND.")]
        public decimal Amount { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }
    }
}
