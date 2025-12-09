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

        [HttpPost("approve")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> ApproveAppointment([FromBody] int appointmentId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
                return NotFound("Randevu bulunamadı.");

            if (appointment.UserId != currentUserId)
                return StatusCode(403, "Sadece kendi randevularınızı onaylayabilirsiniz.");

            appointment.Status = AppointmentStatus.Approved;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Randevu onaylandı: {AppointmentId} by user {UserId}", appointmentId, currentUserId);
            return Ok();
        }

        [HttpPost("reject")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> RejectAppointment([FromBody] int appointmentId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
                return NotFound("Randevu bulunamadı.");

            if (appointment.UserId != currentUserId)
                return StatusCode(403, "Sadece kendi randevularınızı reddedebilirsiniz.");

            appointment.Status = AppointmentStatus.Rejected;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Randevu reddedildi: {AppointmentId} by user {UserId}", appointmentId, currentUserId);
            return Ok();
        }

        [HttpPost("create")]
        [Authorize(Roles = "Trainer,Admin")]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentDTO appointmentDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

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

            var service = await _context.Services.FindAsync(appointmentDto.ServiceId);
            if (service == null)
            {
                return BadRequest("Seçilen hizmet bulunamadı.");
            }

            var appointment = new Appointment
            {
                Date = appointmentDto.Date,
                Time = appointmentDto.Time,
                UserId = appointmentDto.UserId,
                TrainerId = appointmentDto.TrainerId,
                ServiceId = appointmentDto.ServiceId,
                GymId = appointmentDto.GymId,
                Status = AppointmentStatus.Pending, // Her zaman Pending
                Price = service.Price, // Service'ten otomatik al
                CreatedAt = DateTime.UtcNow
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();
            
            var createdDto = await _context.Appointments
                .Where(a => a.AppointmentId == appointment.AppointmentId)
                .Select(a => new AppointmentDTO
                {
                    AppointmentId = a.AppointmentId,
                    Date = a.Date,
                    Time = a.Time,
                    UserId = a.UserId,
                    UserName = a.Member != null ? a.Member.UserName : null,
                    TrainerId = a.TrainerId,
                    TrainerName = a.Trainer != null ? a.Trainer.UserName : null,
                    ServiceId = a.ServiceId,
                    ServiceName = a.Service != null ? a.Service.Name : null,
                    GymId = a.GymId,
                    Status = a.Status,
                    Price = a.Price,
                    CreatedAt = a.CreatedAt,
                    CanceledBy = a.CanceledBy,
                    CanceledAt = a.CanceledAt
                })
                .FirstOrDefaultAsync();
            
            _logger.LogInformation("Yeni randevu oluşturuldu: {@Appointment}", appointment);
            return CreatedAtAction(nameof(GetAppointments), new { id = appointment.AppointmentId }, createdDto);
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
            
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if(User.IsInRole("Trainer") && currentUserId != appointment.TrainerId)
            {
                return StatusCode(403, "Sadece kendi randevularınızı silebilirsiniz.");
            }

            var currentUser = await _context.Users.FindAsync(currentUserId);
            appointment.Status = AppointmentStatus.Canceled;
            appointment.CanceledBy = currentUser?.UserName;
            appointment.CanceledAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Randevu iptal edildi: {AppointmentId} by user {UserId}", appointmentId, currentUserId);
            return NoContent();
        }

        [HttpPost("get")]
        [Authorize] // Tüm girişliler erişir; aşağıda rol bazlı filtre
        public async Task<IActionResult> GetAppointments()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Trainer: sadece kendi randevuları
            if (User.IsInRole("Trainer"))
            {
                var trainerAppointments = await _context.Appointments
                    .Include(a => a.Member)
                    .Include(a => a.Trainer)
                    .Include(a => a.Service)
                    .Include(a => a.Gym)
                    .Where(a => a.TrainerId == currentUserId)
                    .Select(a => new AppointmentDTO
                    {
                        AppointmentId = a.AppointmentId,
                        Date = a.Date,
                        Time = a.Time,
                        UserId = a.UserId,
                        UserName = a.Member != null ? a.Member.UserName : null,
                        TrainerId = a.TrainerId,
                        TrainerName = a.Trainer != null ? a.Trainer.UserName : null,
                        ServiceId = a.ServiceId,
                        ServiceName = a.Service != null ? a.Service.Name : null,
                        GymId = a.GymId,
                        GymName = a.Gym != null ? a.Gym.Name : null,
                        Status = a.Status,
                        Price = a.Price,
                        CreatedAt = a.CreatedAt,
                        CanceledBy = a.CanceledBy,
                        CanceledAt = a.CanceledAt
                    })
                    .ToListAsync();

                return Ok(trainerAppointments);
            }

            // Member: sadece kendi randevuları
            if (User.IsInRole("Member"))
            {
                var memberAppointments = await _context.Appointments
                    .Include(a => a.Member)
                    .Include(a => a.Trainer)
                    .Include(a => a.Service)
                    .Include(a => a.Gym)
                    .Where(a => a.UserId == currentUserId)
                    .Select(a => new AppointmentDTO
                    {
                        AppointmentId = a.AppointmentId,
                        Date = a.Date,
                        Time = a.Time,
                        UserId = a.UserId,
                        UserName = a.Member != null ? a.Member.UserName : null,
                        TrainerId = a.TrainerId,
                        TrainerName = a.Trainer != null ? a.Trainer.UserName : null,
                        ServiceId = a.ServiceId,
                        ServiceName = a.Service != null ? a.Service.Name : null,
                        GymId = a.GymId,
                        GymName = a.Gym != null ? a.Gym.Name : null,
                        Status = a.Status,
                        Price = a.Price,
                        CreatedAt = a.CreatedAt,
                        CanceledBy = a.CanceledBy,
                        CanceledAt = a.CanceledAt
                    })
                    .ToListAsync();

                return Ok(memberAppointments);
            }

            // Admin: tüm randevular
            var appointments = await _context.Appointments
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Include(a => a.Gym)
                .Select(a => new AppointmentDTO
                {
                    AppointmentId = a.AppointmentId,
                    Date = a.Date,
                    Time = a.Time,
                    UserId = a.UserId,
                    UserName = a.Member != null ? a.Member.UserName : null,
                    TrainerId = a.TrainerId,
                    TrainerName = a.Trainer != null ? a.Trainer.UserName : null,
                    ServiceId = a.ServiceId,
                    ServiceName = a.Service != null ? a.Service.Name : null,
                    GymId = a.GymId,
                    GymName = a.Gym != null ? a.Gym.Name : null,
                    Status = a.Status,
                    Price = a.Price,
                    CreatedAt = a.CreatedAt,
                    CanceledBy = a.CanceledBy,
                    CanceledAt = a.CanceledAt
                })
                .ToListAsync();

            return Ok(appointments);
        }

    }
}