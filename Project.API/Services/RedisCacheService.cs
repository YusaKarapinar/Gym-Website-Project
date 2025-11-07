using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Project.API.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<string> GetCachedValueAsync(string key)
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            return (string?)value ?? string.Empty;
        }

        public async Task RemoveCachedValueAsync(string key)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        public Task SetCachedValueAsync(string key, string value)
        {
            var db = _redis.GetDatabase();
            return db.StringSetAsync(key, value);
        }

        public Task SetCachedValueAsync(string key, string value, TimeSpan expiration)
        {
            var db = _redis.GetDatabase();
            return db.StringSetAsync(key, value, expiration);
        }
    }
}