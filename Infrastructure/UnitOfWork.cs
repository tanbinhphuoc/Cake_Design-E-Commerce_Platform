using Application.Interfaces;
using Infrastructure.Repositories;

namespace Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public IAccountRepository Accounts { get; }
        public IProductRepository Products { get; }
        public IOrderRepository Orders { get; }
        public ICartRepository Carts { get; }
        public IShopRepository Shops { get; }
        public ICategoryRepository Categories { get; }
        public ITagRepository Tags { get; }
        public IReviewRepository Reviews { get; }
        public IWishlistRepository Wishlists { get; }
        public IReportRepository Reports { get; }
        public IAddressRepository Addresses { get; }
        public IPaymentRepository Payments { get; }
        public IWalletTransactionRepository WalletTransactions { get; }
        public IShopStaffRepository ShopStaff { get; }
        public ICartItemRepository CartItems { get; }
        public IOrderItemRepository OrderItems { get; }
        public IProductTagRepository ProductTags { get; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Accounts = new AccountRepository(context);
            Products = new ProductRepository(context);
            Orders = new OrderRepository(context);
            Carts = new CartRepository(context);
            Shops = new ShopRepository(context);
            Categories = new CategoryRepository(context);
            Tags = new TagRepository(context);
            Reviews = new ReviewRepository(context);
            Wishlists = new WishlistRepository(context);
            Reports = new ReportRepository(context);
            Addresses = new AddressRepository(context);
            Payments = new PaymentRepository(context);
            WalletTransactions = new WalletTransactionRepository(context);
            ShopStaff = new ShopStaffRepository(context);
            CartItems = new CartItemRepository(context);
            OrderItems = new OrderItemRepository(context);
            ProductTags = new ProductTagRepository(context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
