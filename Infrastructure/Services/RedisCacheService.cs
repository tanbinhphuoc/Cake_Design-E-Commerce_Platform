using System;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;

namespace Infrastructure.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly StackExchange.Redis.IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer connection)
        {
            _db = connection.GetDatabase();
        }

        public Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
            => _db.StringSetAsync(key, value, (Expiration)expiry);

        public async Task<string?> GetStringAsync(string key)
        {
            var val = await _db.StringGetAsync(key);
            return val.HasValue ? val.ToString() : null;
        }

        public Task RemoveAsync(string key) => _db.KeyDeleteAsync(key);
    }
}