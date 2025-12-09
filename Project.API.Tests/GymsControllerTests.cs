using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
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
    public class GymsControllerTests
    {
        private GymContext GetContext()
        {
            var options = new DbContextOptionsBuilder<GymContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new GymContext(options);
        }

        private GymsController GetController(GymContext ctx, string? role = null, bool authenticated = false)
        {
            var logger = new Mock<ILogger<GymsController>>();
            var controller = new GymsController(logger.Object, ctx);

            if (authenticated && !string.IsNullOrEmpty(role))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, role)
                };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                var claimsPrincipal = new ClaimsPrincipal(identity);
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                };
            }
            else
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };
            }

            return controller;
        }

        [Fact]
        public async Task GetGyms_Anonymous_ReturnsOnlyActiveGyms()
        {
            // Arrange
            var ctx = GetContext();
            ctx.Gyms.AddRange(
                new Gym { Name = "Active Gym", Address = "123 St", PhoneNumber = "555-1234", IsActive = true },
                new Gym { Name = "Inactive Gym", Address = "456 St", PhoneNumber = "555-5678", IsActive = false }
            );
            await ctx.SaveChangesAsync();

            var controller = GetController(ctx);

            // Act
            var result = await controller.GetGyms();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var gyms = okResult.Value.Should().BeAssignableTo<List<GymDTO>>().Subject;
            gyms.Should().HaveCount(1);
            gyms.First().Name.Should().Be("Active Gym");
        }

        [Fact]
        public async Task GetGyms_Admin_ReturnsAllGyms()
        {
            // Arrange
            var ctx = GetContext();
            ctx.Gyms.AddRange(
                new Gym { Name = "Active Gym", Address = "123 St", PhoneNumber = "555-1234", IsActive = true },
                new Gym { Name = "Inactive Gym", Address = "456 St", PhoneNumber = "555-5678", IsActive = false }
            );
            await ctx.SaveChangesAsync();

            var controller = GetController(ctx, "Admin", true);

            // Act
            var result = await controller.GetGyms();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var gyms = okResult.Value.Should().BeAssignableTo<List<GymDTO>>().Subject;
            gyms.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetGymById_Unauthorized_ReturnsUnauthorized()
        {
            // Arrange
            var ctx = GetContext();
            var controller = GetController(ctx);

            // Act
            var result = await controller.GetGymById(1);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetGymById_Admin_ReturnsGym()
        {
            // Arrange
            var ctx = GetContext();
            var gym = new Gym { Name = "Test Gym", Address = "123 St", PhoneNumber = "555-1234", IsActive = true };
            ctx.Gyms.Add(gym);
            await ctx.SaveChangesAsync();

            var controller = GetController(ctx, "Admin", true);

            // Act
            var result = await controller.GetGymById(gym.GymId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var gymDto = okResult.Value.Should().BeAssignableTo<GymDTO>().Subject;
            gymDto.Name.Should().Be("Test Gym");
        }

        [Fact]
        public async Task GetGymById_NotFound_ReturnsNotFound()
        {
            // Arrange
            var ctx = GetContext();
            var controller = GetController(ctx, "Admin", true);

            // Act
            var result = await controller.GetGymById(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CreateGym_Admin_ReturnsCreated()
        {
            // Arrange
            var ctx = GetContext();
            var controller = GetController(ctx, "Admin", true);
            var dto = new GymDTO
            {
                Name = "New Gym",
                Address = "789 St",
                PhoneNumber = "555-9999",
                IsActive = true
            };

            // Act
            var result = await controller.CreateGym(dto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var gymDto = createdResult.Value.Should().BeAssignableTo<GymDTO>().Subject;
            gymDto.Name.Should().Be("New Gym");
            ctx.Gyms.Should().HaveCount(1);
        }

        [Fact]
        public async Task UpdateGym_Admin_ReturnsOk()
        {
            // Arrange
            var ctx = GetContext();
            var gym = new Gym { Name = "Old Name", Address = "123 St", PhoneNumber = "555-1234", IsActive = true };
            ctx.Gyms.Add(gym);
            await ctx.SaveChangesAsync();

            var controller = GetController(ctx, "Admin", true);
            var dto = new GymDTO
            {
                GymId = gym.GymId,
                Name = "Updated Name",
                Address = "123 St",
                PhoneNumber = "555-1234",
                IsActive = true
            };

            // Act
            var result = await controller.UpdateGym(gym.GymId, dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var updatedGym = await ctx.Gyms.FindAsync(gym.GymId);
            updatedGym!.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task UpdateGym_NotFound_ReturnsNotFound()
        {
            // Arrange
            var ctx = GetContext();
            var controller = GetController(ctx, "Admin", true);
            var dto = new GymDTO
            {
                GymId = 999,
                Name = "Updated Name",
                Address = "123 St",
                PhoneNumber = "555-1234",
                IsActive = true
            };

            // Act
            var result = await controller.UpdateGym(999, dto);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteGym_Admin_ReturnsOk()
        {
            // Arrange
            var ctx = GetContext();
            var gym = new Gym { Name = "To Delete", Address = "123 St", PhoneNumber = "555-1234", IsActive = true };
            ctx.Gyms.Add(gym);
            await ctx.SaveChangesAsync();

            var controller = GetController(ctx, "Admin", true);

            // Act
            var result = await controller.DeleteGym(gym.GymId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var deletedGym = await ctx.Gyms.FindAsync(gym.GymId);
            deletedGym.Should().BeNull();
        }

        [Fact]
        public async Task DeleteGym_NotFound_ReturnsNotFound()
        {
            // Arrange
            var ctx = GetContext();
            var controller = GetController(ctx, "Admin", true);

            // Act
            var result = await controller.DeleteGym(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
