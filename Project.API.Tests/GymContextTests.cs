using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Project.API.Data;
using Project.API.Models;
using Xunit;

namespace Project.API.Tests
{
    public class GymContextTests
    {
        private GymContext GetContext()
        {
            var options = new DbContextOptionsBuilder<GymContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new GymContext(options);
        }

        [Fact]
        public async Task CanAddAndRetrieveGym()
        {
            // Arrange
            using var ctx = GetContext();
            var gym = new Gym
            {
                Name = "Test Gym",
                Address = "123 Test St",
                PhoneNumber = "555-1234",
                IsActive = true
            };

            // Act
            ctx.Gyms.Add(gym);
            await ctx.SaveChangesAsync();

            // Assert
            var retrieved = await ctx.Gyms.FirstOrDefaultAsync(g => g.Name == "Test Gym");
            retrieved.Should().NotBeNull();
            retrieved!.Address.Should().Be("123 Test St");
        }

        [Fact]
        public async Task CanAddAndRetrieveService()
        {
            // Arrange
            using var ctx = GetContext();
            var gym = new Gym
            {
                Name = "Test Gym",
                Address = "123 Test St",
                PhoneNumber = "555-1234",
                IsActive = true
            };
            ctx.Gyms.Add(gym);
            await ctx.SaveChangesAsync();

            var service = new Service
            {
                Name = "Personal Training",
                Description = "One-on-one training",
                GymId = gym.GymId,
                Price = 100m,
                Duration = TimeSpan.FromHours(1),
                ServiceType = "Training",
                IsActive = true
            };

            // Act
            ctx.Services.Add(service);
            await ctx.SaveChangesAsync();

            // Assert
            var retrieved = await ctx.Services
                .Include(s => s.Gym)
                .FirstOrDefaultAsync(s => s.Name == "Personal Training");
            
            retrieved.Should().NotBeNull();
            retrieved!.Gym.Should().NotBeNull();
            retrieved.Gym!.Name.Should().Be("Test Gym");
        }

        [Fact]
        public async Task CanAddAndRetrieveAppointment()
        {
            // Arrange
            using var ctx = GetContext();
            
            var gym = new Gym { Name = "Test Gym", Address = "123 St", PhoneNumber = "555-1234", IsActive = true };
            ctx.Gyms.Add(gym);
            await ctx.SaveChangesAsync();

            var service = new Service
            {
                Name = "PT",
                Description = "Personal Training",
                GymId = gym.GymId,
                Price = 100m,
                Duration = TimeSpan.FromHours(1),
                ServiceType = "Training",
                IsActive = true
            };
            ctx.Services.Add(service);
            await ctx.SaveChangesAsync();

            var appointment = new Appointment
            {
                Date = DateTime.UtcNow.Date.AddDays(1),
                Time = new TimeSpan(10, 0, 0),
                UserId = 1,
                TrainerId = 2,
                ServiceId = service.ServiceId,
                GymId = gym.GymId,
                Status = "Pending"
            };

            // Act
            ctx.Appointments.Add(appointment);
            await ctx.SaveChangesAsync();

            // Assert
            var retrieved = await ctx.Appointments
                .Include(a => a.Service)
                .Include(a => a.Gym)
                .FirstOrDefaultAsync();
            
            retrieved.Should().NotBeNull();
            retrieved!.Status.Should().Be("Pending");
            retrieved.Service.Should().NotBeNull();
            retrieved.Service!.Name.Should().Be("PT");
        }

        [Fact]
        public async Task DeleteGym_CascadesDeleteServices()
        {
            // Arrange
            using var ctx = GetContext();
            
            var gym = new Gym { Name = "Test Gym", Address = "123 St", PhoneNumber = "555-1234", IsActive = true };
            ctx.Gyms.Add(gym);
            await ctx.SaveChangesAsync();

            var service = new Service
            {
                Name = "PT",
                Description = "Personal Training",
                GymId = gym.GymId,
                Price = 100m,
                Duration = TimeSpan.FromHours(1),
                ServiceType = "Training",
                IsActive = true
            };
            ctx.Services.Add(service);
            await ctx.SaveChangesAsync();

            // Act
            ctx.Gyms.Remove(gym);
            await ctx.SaveChangesAsync();

            // Assert
            var gymExists = await ctx.Gyms.AnyAsync(g => g.GymId == gym.GymId);
            var serviceExists = await ctx.Services.AnyAsync(s => s.ServiceId == service.ServiceId);
            
            gymExists.Should().BeFalse();
            serviceExists.Should().BeFalse();
        }

        [Fact]
        public async Task CanQueryActiveGymsOnly()
        {
            // Arrange
            using var ctx = GetContext();
            ctx.Gyms.AddRange(
                new Gym { Name = "Active 1", Address = "A", PhoneNumber = "111", IsActive = true },
                new Gym { Name = "Active 2", Address = "B", PhoneNumber = "222", IsActive = true },
                new Gym { Name = "Inactive", Address = "C", PhoneNumber = "333", IsActive = false }
            );
            await ctx.SaveChangesAsync();

            // Act
            var activeGyms = await ctx.Gyms.Where(g => g.IsActive).ToListAsync();

            // Assert
            activeGyms.Should().HaveCount(2);
            activeGyms.Should().OnlyContain(g => g.IsActive);
        }

        [Fact]
        public async Task ServicePrice_ShouldBeDecimal()
        {
            // Arrange
            using var ctx = GetContext();
            var gym = new Gym { Name = "Test Gym", Address = "123 St", PhoneNumber = "555-1234", IsActive = true };
            ctx.Gyms.Add(gym);
            await ctx.SaveChangesAsync();

            var service = new Service
            {
                Name = "PT",
                Description = "Personal Training",
                GymId = gym.GymId,
                Price = 99.99m,
                Duration = TimeSpan.FromHours(1),
                ServiceType = "Training",
                IsActive = true
            };

            // Act
            ctx.Services.Add(service);
            await ctx.SaveChangesAsync();

            // Assert
            var retrieved = await ctx.Services.FirstOrDefaultAsync(s => s.Name == "PT");
            retrieved.Should().NotBeNull();
            retrieved!.Price.Should().Be(99.99m);
        }
    }
}
