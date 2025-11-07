using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
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
    public class AppointmentsController : Controller
    {
        private readonly ILogger<AppointmentsController> _logger;
        private readonly GymContext _context;

        public AppointmentsController(ILogger<AppointmentsController> logger, GymContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentDTO appointmentDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Request atan kullanıcının ID'sini al
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Map DTO to domain model
            if (User.IsInRole("Trainer") && currentUserId != appointmentDto.TrainerId)
            {
                return StatusCode(403, $"Sadece kendi randevularınızı oluşturabilirsiniz. Sizin ID'niz: {currentUserId}");
            }

            if (appointmentDto.Date < DateTime.UtcNow.Date)
            {
                return BadRequest("Randevu tarihi bugünden önce olamaz.");
            }
            if (appointmentDto.Date == DateTime.UtcNow.Date && appointmentDto.Time < DateTime.UtcNow.TimeOfDay)
            {
                return BadRequest("Randevu saati geçmiş olamaz.");
            }
            if (await _context.Appointments.AnyAsync(a => a.Date == appointmentDto.Date && a.Time == appointmentDto.Time && a.TrainerId == appointmentDto.TrainerId))
            {
                return Conflict("Bu tarihte ve saatte zaten bir randevu var.");
            }
            if (await _context.Appointments.AnyAsync(a => a.Date == appointmentDto.Date && a.Time == appointmentDto.Time && a.UserId == appointmentDto.UserId))
            {
                return Conflict("Üyenin bu tarihte ve saatte zaten bir randevusu var.");
            }
            if (await _context.Appointments.AnyAsync(a => a.Date == appointmentDto.Date && a.Time == appointmentDto.Time && a.UserId == appointmentDto.TrainerId))
            {
                return Conflict("trainerin Bu tarihte ve saatte zaten bir randevusu var.");
            }
            
            



            var appointment = new Appointment
            {
                Date = appointmentDto.Date,
                Time = appointmentDto.Time,
                UserId = appointmentDto.UserId,
                TrainerId = appointmentDto.TrainerId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Yeni randevu oluşturuldu: {@Appointment}", appointment);
            return CreatedAtAction(nameof(CreateAppointment), new { id = appointment.AppointmentId }, appointment);
        }

        [HttpPost("delete")]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> DeleteAppointment([FromBody] int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null)
            {
                return NotFound();
            }
            
            // Request atan kullanıcının ID'sini al
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if(User.IsInRole("Trainer") && currentUserId != appointment.TrainerId)
            {
                return StatusCode(403, "Sadece kendi randevularınızı silebilirsiniz.");
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Randevu silindi: {@Appointment}", appointment);
            return NoContent();
        }

        [HttpPost("get")]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> GetAppointments()
        {
            if(User.IsInRole("Trainer"))
            {
                // Request atan kullanıcının ID'sini al
                var trainerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var trainerAppointments = await _context.Appointments
                    .Where(a => a.TrainerId == trainerId)
                    .ToListAsync();

                return Ok(trainerAppointments);
            }
            var appointments = await _context.Appointments.ToListAsync();

            return Ok(appointments);
        }

    }
}