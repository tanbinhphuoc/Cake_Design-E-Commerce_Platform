using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RefreshTokenInfo
    {
        public Guid TokenId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public interface IRefreshTokenService
    {
        Task<RefreshTokenInfo> CreateAsync(Guid userId, TimeSpan lifetime);
        Task<RefreshTokenInfo?> GetAsync(string refreshToken);
        Task InvalidateAsync(string refreshToken);
        Task InvalidateAllForUserAsync(Guid userId);
    }
}
