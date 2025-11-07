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
    public class ServicesController : Controller
    {
        private readonly ILogger<ServicesController> _logger;
        private readonly GymContext _context;

        public ServicesController(ILogger<ServicesController> logger, GymContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetServices()
        {
            var allServices = new List<ServiceDTO>();

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }
            if (User.IsInRole("Admin"))
            {
                allServices = await _context.Services
                    .Include(s => s.Gym)
                    .Select(s => new ServiceDTO
                    {
                        ServiceId = s.ServiceId,
                        Name = s.Name,
                        Description = s.Description,
                        ServiceType = s.ServiceType,
                        Duration = s.Duration,
                        Price = s.Price,
                        GymId = s.GymId,
                        IsActive = s.IsActive
                    })
                    .ToListAsync();
                return Ok(allServices);
            }

            allServices = await _context.Services
               .Include(s => s.Gym).Where(c => c.Gym.IsActive == true && c.IsActive == true)
               .Select(s => new ServiceDTO
               {
                   ServiceId = s.ServiceId,
                   Name = s.Name,
                   Description = s.Description,
                   ServiceType = s.ServiceType,
                   Duration = s.Duration,
                   Price = s.Price,
                   GymId = s.GymId,
                   IsActive = s.IsActive
               })
               .ToListAsync();
            return Ok(allServices);


        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetServiceById(int id)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }
            if (!User.IsInRole("Admin"))
            {
                var gymCheck = await _context.Services
                    .Include(s => s.Gym)
                    .Where(s => s.ServiceId == id && s.Gym.IsActive == true && s.IsActive == true)
                    .FirstOrDefaultAsync();
                if (gymCheck == null)
                {
                    return NotFound();
                }
            }
            var service = await _context.Services
                .Include(s => s.Gym)
                .Where(s => s.ServiceId == id)
                .Select(s => new ServiceDTO
                {
                    ServiceId = s.ServiceId,
                    Name = s.Name,
                    Description = s.Description,
                    ServiceType = s.ServiceType,
                    Duration = s.Duration,
                    Price = s.Price,
                    GymId = s.GymId,
                    IsActive = s.IsActive
                })
                .FirstOrDefaultAsync();
            
            if (service == null)
            {
                return NotFound(new { message = $"Service {id} bulunamadı." });
            }
            
            return Ok(service);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateService([FromBody] ServiceDTO serviceDto)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // GymId kontrolü - Gym var mı?
            var gymExists = await _context.Gyms.AnyAsync(g => g.GymId == serviceDto.GymId);
            if (!gymExists)
            {
                return BadRequest(new { message = $"GymId {serviceDto.GymId} bulunamadı. Önce bir Gym oluşturun." });
            }

            var service = new Service
            {
                Name = serviceDto.Name,
                Description = serviceDto.Description,
                ServiceType = serviceDto.ServiceType,
                Duration = serviceDto.Duration,
                Price = serviceDto.Price,
                GymId = serviceDto.GymId,
                IsActive = serviceDto.IsActive
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetServiceById), new { id = service.ServiceId }, service);
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] ServiceDTO serviceDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            service.Name = serviceDto.Name;
            service.Description = serviceDto.Description;
            service.ServiceType = serviceDto.ServiceType;
            service.Duration = serviceDto.Duration;
            service.Price = serviceDto.Price;
            service.GymId = serviceDto.GymId;
            service.IsActive = serviceDto.IsActive;
            service.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            // 200 OK ile güncellenmiş veriyi döndür
            return Ok(service);
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            // 200 OK ile başarı mesajı döndür
            return Ok(new { message = $"Service {id} başarıyla silindi." });
        }

    }
}