using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rating.API.DTO;
using Rating.API.Models;
using Serilog;

namespace Rating.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly PostsContext _context;
        private readonly ILogger<PostsController> _logger;
        private readonly Services.ICacheService _cacheService;

        public PostsController(PostsContext context, ILogger<PostsController> logger, Services.ICacheService cacheService)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
        }

        public static PostDTO ConvertToDTO(Post post)
        {
            return new PostDTO
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };
        }

        [HttpGet("getposts")]
        [Authorize(Roles = "User,Admin")]
        public async Task<ActionResult<IEnumerable<PostDTO>>> GetPosts()
        {
            if (_context.Posts == null)
                return NotFound("Posts not found.");

            List<PostDTO> posts;

            if (User.IsInRole("Admin"))
            {
                _logger.LogInformation("Admin tüm gönderileri çekiyor.");
                posts = await _context.Posts
                    .Select(p => new PostDTO
                    {
                        PostId = p.PostId,
                        Title = p.Title,
                        Content = p.Content,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();
            }
            else
            {
                _logger.LogInformation("User yalnızca public gönderileri çekiyor.");
                posts = await _context.Posts
                    .Where(p => p.PostState == "Public")
                    .Select(p => new PostDTO
                    {
                        PostId = p.PostId,
                        Title = p.Title,
                        Content = p.Content,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();
            }

            if (posts.Count == 0)
                return NotFound("Hiç gönderi bulunamadı.");

            return Ok(posts);
        }

        [HttpGet("getpost/{id}")]
        public async Task<ActionResult<PostDTO>> GetPost(int id)
        {
            if (_context.Posts == null)
                return NotFound("Posts not found.");

            // Try to get from cache first
            var cacheKey = $"post_{id}";
            var cachedPost = await _cacheService.GetCachedValueAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedPost))
            {
                _logger.LogInformation("Post retrieved from cache: ID={PostId}", id);
                var postDto = System.Text.Json.JsonSerializer.Deserialize<PostDTO>(cachedPost);
                return Ok(postDto);
            }

            // If not in cache, get from database
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
                return NotFound("Post not found.");

            if (post.PostState == "Private" && !User.IsInRole("Admin"))
                return Forbid("Bu gönderiye erişim izniniz yok.");

            var dto = ConvertToDTO(post);
            
            // Store in cache for future requests (cache for 5 minutes)
            var serializedPost = System.Text.Json.JsonSerializer.Serialize(dto);
            await _cacheService.SetCachedValueAsync(cacheKey, serializedPost);

            _logger.LogInformation("Post alındı ve cache'e kaydedildi: ID={PostId}, Başlık={Title}", post.PostId, post.Title);
            return Ok(dto);
        }
    }
}
