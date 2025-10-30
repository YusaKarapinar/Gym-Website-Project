using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Rating.API.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        
        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _database = _redis.GetDatabase();
        }

        public async Task<string> GetCachedValueAsync(string key)
        {
            var value = await _database.StringGetAsync(key);

            return (string?)value ?? string.Empty;
        }

        public async Task RemoveCachedValueAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }

        public Task SetCachedValueAsync(string key, string value)
        {
            return _database.StringSetAsync(key, value);
        }

        public Task SetCachedValueAsync(string key, string value, TimeSpan expiration)
        {
            return _database.StringSetAsync(key, value, expiration);
        }
    }
}