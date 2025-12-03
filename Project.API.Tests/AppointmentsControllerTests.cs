using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Project.API.Controllers;
using Project.API.Data;
using Project.API.DTO;
using Project.API.Models;
using Xunit;

namespace Project.API.Tests
{
    public class AppointmentsControllerTests
    {
        private GymContext GetContext()
        {
            var options = new DbContextOptionsBuilder<GymContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new GymContext(options);
        }

        private AppointmentsController GetController(GymContext ctx, int userId, string role)
        {
            var logger = new Mock<ILogger<AppointmentsController>>();
            var controller = new AppointmentsController(logger.Object, ctx);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role) };
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) } };
            return controller;
        }

        [Fact]
        public async Task CreateAppointment_Success()
        {
            var ctx = GetContext();
            ctx.Services.Add(new Service { ServiceId = 1, Name = "PT", Description = "Personal Training", Price = 100, ServiceType = "Training" });
            await ctx.SaveChangesAsync();

            var result = await GetController(ctx, 1, "Trainer").CreateAppointment(new AppointmentDTO
            {
                Date = DateTime.UtcNow.Date.AddDays(1),
                Time = new TimeSpan(10, 0, 0),
                UserId = 2,
                TrainerId = 1,
                ServiceId = 1,
                GymId = 1
            });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task CreateAppointment_PastDate_BadRequest()
        {
            var result = await GetController(GetContext(), 1, "Trainer").CreateAppointment(new AppointmentDTO
            {
                Date = DateTime.UtcNow.Date.AddDays(-1),
                Time = new TimeSpan(10, 0, 0),
                UserId = 2,
                TrainerId = 1,
                ServiceId = 1,
                GymId = 1
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAppointment_ServiceNotFound_BadRequest()
        {
            var result = await GetController(GetContext(), 1, "Trainer").CreateAppointment(new AppointmentDTO
            {
                Date = DateTime.UtcNow.Date.AddDays(1),
                Time = new TimeSpan(10, 0, 0),
                UserId = 2,
                TrainerId = 1,
                ServiceId = 999,
                GymId = 1
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAppointment_TrainerForOther_Forbidden()
        {
            var ctx = GetContext();
            ctx.Services.Add(new Service { ServiceId = 1, Name = "PT", Description = "Personal Training", Price = 100, ServiceType = "Training" });
            await ctx.SaveChangesAsync();

            var result = await GetController(ctx, 1, "Trainer").CreateAppointment(new AppointmentDTO
            {
                Date = DateTime.UtcNow.Date.AddDays(1),
                Time = new TimeSpan(10, 0, 0),
                UserId = 2,
                TrainerId = 3,
                ServiceId = 1,
                GymId = 1
            });

            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
        }

        [Fact]
        public async Task DeleteAppointment_Success()
        {
            var ctx = GetContext();
            var apt = new Appointment { Date = DateTime.UtcNow.Date.AddDays(1), Time = new TimeSpan(10, 0, 0), UserId = 2, TrainerId = 1, ServiceId = 1, GymId = 1, Price = 100, Status = AppointmentStatus.Pending };
            ctx.Appointments.Add(apt);
            await ctx.SaveChangesAsync();

            var result = await GetController(ctx, 1, "Trainer").DeleteAppointment(apt.AppointmentId);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteAppointment_NotFound()
        {
            var result = await GetController(GetContext(), 1, "Trainer").DeleteAppointment(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteAppointment_TrainerForOther_Forbidden()
        {
            var ctx = GetContext();
            var apt = new Appointment { Date = DateTime.UtcNow.Date.AddDays(1), Time = new TimeSpan(10, 0, 0), UserId = 2, TrainerId = 3, ServiceId = 1, GymId = 1, Price = 100, Status = AppointmentStatus.Pending };
            ctx.Appointments.Add(apt);
            await ctx.SaveChangesAsync();

            var result = await GetController(ctx, 1, "Trainer").DeleteAppointment(apt.AppointmentId);

            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
        }
    }
}
