using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Project.API.Data;
using Project.API.DTO;
using Project.API.Models;

namespace Project.API.Controllers
{
    [Route("api/[controller]")]
    public class GymsController : Controller
    {
        private readonly ILogger<GymsController> _logger;
        private readonly GymContext _context;

        public GymsController(ILogger<GymsController> logger, GymContext context)
        {
            _logger = logger;
            _context = context;
        }
        [HttpGet]
        [AllowAnonymous]  // Register sayfası için gym listesi herkese açık
        public async Task<IActionResult> GetGyms()
        {
            var gyms = new List<GymDTO>();
            
            // Eğer kullanıcı giriş yapmışsa ve Admin ise tüm gymler, değilse sadece aktif olanlar
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                gyms = await _context.Gyms
                    .Select(g => new GymDTO
                    {
                        GymId = g.GymId,
                        Name = g.Name,
                        Address = g.Address,
                        PhoneNumber = g.PhoneNumber,
                        IsActive = g.IsActive,
                        CreatedAt = g.CreatedAt,
                        UpdatedAt = g.UpdatedAt
                    })
                    .ToListAsync();
            }
            else
            {
                // Giriş yapmamış kullanıcılar veya normal kullanıcılar sadece aktif gymler görebilir
                gyms = await _context.Gyms
                    .Where(g => g.IsActive)
                    .Select(g => new GymDTO
                    {
                        GymId = g.GymId,
                        Name = g.Name,
                        Address = g.Address,
                        PhoneNumber = g.PhoneNumber,
                        IsActive = g.IsActive,
                        CreatedAt = g.CreatedAt,
                        UpdatedAt = g.UpdatedAt
                    })
                    .ToListAsync();
            }
            return Ok(gyms);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGymById(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }
            GymDTO? gym;
            if (User.IsInRole("Admin"))
            {
                gym = await _context.Gyms
                    .Where(g => g.GymId == id)
                    .Select(g => new GymDTO
                    {
                        GymId = g.GymId,
                        Name = g.Name,
                        Address = g.Address,
                        PhoneNumber = g.PhoneNumber,
                        IsActive = g.IsActive,
                        CreatedAt = g.CreatedAt,
                        UpdatedAt = g.UpdatedAt
                    })
                    .FirstOrDefaultAsync();
                if (gym == null)
                {
                    return NotFound();
                }
            }
            else
            {
                gym = await _context.Gyms
                    .Where(g => g.GymId == id && g.IsActive)
                    .Select(g => new GymDTO
                    {
                        GymId = g.GymId,
                        Name = g.Name,
                        Address = g.Address,
                        PhoneNumber = g.PhoneNumber,
                        IsActive = g.IsActive,
                        CreatedAt = g.CreatedAt,
                        UpdatedAt = g.UpdatedAt
                    })
                    .FirstOrDefaultAsync();
                if (gym == null)
                {
                    return NotFound();
                }
            }
            return Ok(gym);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateGym([FromBody] GymDTO gymDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            if (gymDto == null)
            {
                return BadRequest();
            }

            var gym = new Gym
            {
                Name = gymDto.Name,
                Address = gymDto.Address,
                PhoneNumber = gymDto.PhoneNumber,
                IsActive = gymDto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Gyms.Add(gym);
            await _context.SaveChangesAsync();

            var createdGymDto = new GymDTO
            {
                GymId = gym.GymId,
                Name = gym.Name,
                Address = gym.Address,
                PhoneNumber = gym.PhoneNumber,
                IsActive = gym.IsActive,
                CreatedAt = gym.CreatedAt,
                UpdatedAt = gym.UpdatedAt
            };

            return CreatedAtAction(nameof(GetGymById), new { id = gym.GymId }, createdGymDto);
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGym(int id, [FromBody] GymDTO gymDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            if (gymDto == null)
            {
                return BadRequest();
            }
            var existingGym = await _context.Gyms.FindAsync(id);
            if (existingGym == null)
            {
                return NotFound();
            }
            existingGym.Name = gymDto.Name;
            existingGym.Address = gymDto.Address;
            existingGym.PhoneNumber = gymDto.PhoneNumber;
            existingGym.IsActive = gymDto.IsActive;
            existingGym.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            var updatedGymDto = new GymDTO
            {
                GymId = existingGym.GymId,
                Name = existingGym.Name,
                Address = existingGym.Address,
                PhoneNumber = existingGym.PhoneNumber,
                IsActive = existingGym.IsActive,
                CreatedAt = existingGym.CreatedAt,
                UpdatedAt = existingGym.UpdatedAt
            };
            
            return Ok(updatedGymDto);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGym(int id)
        {
            var existingGym = await _context.Gyms.FindAsync(id);
            if (existingGym == null)
            {
                return NotFound();
            }

            _context.Gyms.Remove(existingGym);
            await _context.SaveChangesAsync();

            // 200 OK ile başarı mesajı döndür
            return Ok(new { message = $"Gym {id} başarıyla silindi." });
        }



    }
}