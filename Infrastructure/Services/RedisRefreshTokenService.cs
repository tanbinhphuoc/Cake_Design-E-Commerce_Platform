using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Services;
using StackExchange.Redis;

namespace Infrastructure.Services
{
    public class RedisRefreshTokenService : IRefreshTokenService
    {
        private readonly IDatabase _db;
        public RedisRefreshTokenService(IConnectionMultiplexer conn) => _db = conn.GetDatabase();

        private static string Key(Guid userId, Guid tokenId) => $"auth:rt:{userId}:{tokenId}";

        public async Task<RefreshTokenInfo> CreateAsync(Guid userId, TimeSpan lifetime)
        {
            var info = new RefreshTokenInfo
            {
                TokenId = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.Add(lifetime)
            };
            await _db.StringSetAsync(Key(userId, info.TokenId),
                JsonSerializer.Serialize(info),
                lifetime);
            return info;
        }

        public async Task<RefreshTokenInfo?> GetAsync(string refreshToken)
        {
            if (!Guid.TryParse(refreshToken, out var tokenId)) return null;
            var parts = refreshToken.Split(':');
            // Format: "{userId}:{tokenId}"
            if (parts.Length != 2) return null;
            if (!Guid.TryParse(parts[0], out var userId)) return null;
            if (!Guid.TryParse(parts[1], out tokenId)) return null;

            var val = await _db.StringGetAsync(Key(userId, tokenId));
            if (!val.HasValue) return null;
            return JsonSerializer.Deserialize<RefreshTokenInfo>(val!);
        }

        public async Task InvalidateAsync(string refreshToken)
        {
            if (!Guid.TryParse(refreshToken, out var tokenId)) return;
            var parts = refreshToken.Split(':');
            if (parts.Length != 2) return;
            if (!Guid.TryParse(parts[0], out var userId)) return;
            if (!Guid.TryParse(parts[1], out tokenId)) return;

            await _db.KeyDeleteAsync(Key(userId, tokenId));
        }

        public async Task InvalidateAllForUserAsync(Guid userId)
        {
            // pattern delete; for large scale, consider SCAN + batch delete
            var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints()[0]);
            foreach (var key in server.Keys(pattern: $"auth:rt:{userId}:*"))
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }
}
