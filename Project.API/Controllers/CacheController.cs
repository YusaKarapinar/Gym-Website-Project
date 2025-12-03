using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project.API.Services;

namespace Project.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CacheController : ControllerBase
    {
        private readonly ILogger<CacheController> _logger;
        private readonly ICacheService _cacheService;

        public CacheController(ILogger<CacheController> logger, ICacheService cacheService)
        {
            _logger = logger;
            _cacheService = cacheService;
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetCache(string key)
        {
            var value = await _cacheService.GetCachedValueAsync(key);
            if (string.IsNullOrEmpty(value))
                return NotFound($"Key '{key}' not found in cache.");
            
            return Ok(new { Key = key, Value = value });
        }

        [HttpPost]
        public async Task<IActionResult> SetCache([FromBody] CacheRequest request)
        {
            if (request.ExpirationMinutes.HasValue)
            {
                await _cacheService.SetCachedValueAsync(
                    request.Key, 
                    request.Value, 
                    TimeSpan.FromMinutes(request.ExpirationMinutes.Value));
            }
            else
            {
                await _cacheService.SetCachedValueAsync(request.Key, request.Value);
            }
            
            _logger.LogInformation("Cache set: Key={Key}", request.Key);
            return Ok(new { Message = "Cache set successfully", Key = request.Key });
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> DeleteCache(string key)
        {
            await _cacheService.RemoveCachedValueAsync(key);
            _logger.LogInformation("Cache deleted: Key={Key}", key);
            return Ok(new { Message = "Cache deleted successfully", Key = key });
        }
    }

    public class CacheRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int? ExpirationMinutes { get; set; }
    }
}