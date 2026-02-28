using System;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IRedisCacheService
    {
        Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetStringAsync(string key);
        Task RemoveAsync(string key);
    }
}