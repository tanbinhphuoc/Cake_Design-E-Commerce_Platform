namespace Application.DTOs
{
    public class UpdateWalletDto
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
    }
}
