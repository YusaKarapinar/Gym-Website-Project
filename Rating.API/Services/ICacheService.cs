using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rating.API.Services
{
    public interface ICacheService
    {
        Task<string> GetCachedValueAsync(string key);
        Task SetCachedValueAsync(string key, string value);
        Task SetCachedValueAsync(string key, string value, TimeSpan expiration);
        Task RemoveCachedValueAsync(string key);
    }
}