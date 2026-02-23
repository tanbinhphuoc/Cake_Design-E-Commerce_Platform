namespace Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository Accounts { get; }
        IProductRepository Products { get; }
        IOrderRepository Orders { get; }
        ICartRepository Carts { get; }
        IShopRepository Shops { get; }
        ICategoryRepository Categories { get; }
        ITagRepository Tags { get; }
        IReviewRepository Reviews { get; }
        IWishlistRepository Wishlists { get; }
        IReportRepository Reports { get; }
        IAddressRepository Addresses { get; }
        IPaymentRepository Payments { get; }
        IWalletTransactionRepository WalletTransactions { get; }
        IShopStaffRepository ShopStaff { get; }
        ICartItemRepository CartItems { get; }
        IOrderItemRepository OrderItems { get; }
        IProductTagRepository ProductTags { get; }

        Task<int> SaveChangesAsync();
    }
}
