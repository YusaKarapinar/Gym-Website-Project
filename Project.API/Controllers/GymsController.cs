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
        public async Task<IActionResult> GetGyms()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }
            var gyms = new List<Gym>();
            if (User.IsInRole("Admin"))
            {

                gyms = await _context.Gyms.ToListAsync();
            }
            else
            {
                gyms = await _context.Gyms.Where(g => g.IsActive).ToListAsync();
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
            Gym? gym;
            if (User.IsInRole("Admin"))
            {
                gym = await _context.Gyms.FindAsync(id);
                if (gym == null)
                {
                    return NotFound();
                }
            }
            else
            {
                gym = await _context.Gyms.FirstOrDefaultAsync(g => g.GymId == id && g.IsActive);
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
                IsActive = gymDto.IsActive
            };

            _context.Gyms.Add(gym);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGymById), new { id = gym.GymId }, gym);
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
            existingGym.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            // 200 OK ile güncellenmiş veriyi döndür
            return Ok(existingGym);
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