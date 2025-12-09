using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Project.API.Controllers;
using Project.API.DTO;
using Project.API.Models;
using Project.API.Data;
using Xunit;

namespace Project.API.Tests
{
    public class ServicesControllerTestFixture : IDisposable
    {
        public DbContextOptions<GymContext> Options { get; }
        public int GymId1 { get; private set; }
        public int GymId2 { get; private set; }
        public int ServiceId1 { get; private set; }
        public int ServiceId2 { get; private set; }

        public ServicesControllerTestFixture()
        {
            Options = new DbContextOptionsBuilder<GymContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        public async Task InitializeAsync()
        {
            using var context = new GymContext(Options);
            var gym1 = new Gym { Name = "G1", Address = "A", PhoneNumber = "P", IsActive = true };
            var gym2 = new Gym { Name = "G2", Address = "B", PhoneNumber = "D", IsActive = true };
            
            context.Gyms.Add(gym1);
            context.Gyms.Add(gym2);
            await context.SaveChangesAsync();

            GymId1 = gym1.GymId;
            GymId2 = gym2.GymId;

            var serviceActive = new Service 
            { 
                Name = "S1", 
                Description = "Active", 
                GymId = gym1.GymId, 
                Price = 10m, 
                Duration = TimeSpan.FromMinutes(30), 
                ServiceType = "Type1", 
                IsActive = true 
            };
            var serviceInactive = new Service 
            { 
                Name = "S2", 
                Description = "Inactive", 
                GymId = gym2.GymId, 
                Price = 5m, 
                Duration = TimeSpan.FromMinutes(15), 
                ServiceType = "Type2", 
                IsActive = false 
            };
            
            context.Services.AddRange(serviceActive, serviceInactive);
            await context.SaveChangesAsync();

            ServiceId1 = serviceActive.ServiceId;
            ServiceId2 = serviceInactive.ServiceId;
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    // Testler: Her testin kendi seeded DB'si olmalı
    public class ServicesControllerTests : IAsyncLifetime
    {
        private ServicesControllerTestFixture _fixture;

        public async Task InitializeAsync()
        {
            _fixture = new ServicesControllerTestFixture();
            await _fixture.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await _fixture.DisposeAsync();
        }

        private ServicesController CreateController(ClaimsIdentity? identity = null)
        {
            var context = new GymContext(_fixture.Options);
            var logger = new Mock<ILogger<ServicesController>>().Object;
            var controller = new ServicesController(logger, context);
            
            var httpContext = new DefaultHttpContext();
            if (identity != null)
                httpContext.User = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            
            return controller;
        }

        [Fact]
        public async Task GetServices_UnauthenticatedUser_ReturnsUnauthorized()
        {
            var controller = CreateController();
            var result = await controller.GetServices();
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetServices_AdminUser_ReturnsAllServices()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Test");
            var controller = CreateController(identity);
            var result = await controller.GetServices();
            
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<ServiceDTO>>(ok.Value);
            Assert.Equal(2, System.Linq.Enumerable.Count(list));
        }

        [Fact]
        public async Task GetServices_DefaultUser_ReturnsActiveServicesOnly()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "2") }, "Test");
            var controller = CreateController(identity);
            var result = await controller.GetServices();
            
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<ServiceDTO>>(ok.Value);
            Assert.Single(list);
            var item = System.Linq.Enumerable.First(list);
            Assert.Equal("S1", item.Name);
        }

        [Fact]
        public async Task GetServiceById_UnauthenticatedUser_ReturnsUnauthorized()
        {
            var controller = CreateController();
            var result = await controller.GetServiceById(_fixture.ServiceId1);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetServiceById_AdminUser_ReturnsServiceBothActiveAndInactive()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Test");
            var controller = CreateController(identity);
            
            var result1 = await controller.GetServiceById(_fixture.ServiceId1);
            var result2 = await controller.GetServiceById(_fixture.ServiceId2);
            
            var ok1 = Assert.IsType<OkObjectResult>(result1);
            var svc1 = Assert.IsType<ServiceDTO>(ok1.Value);
            Assert.Equal("S1", svc1.Name);

            var ok2 = Assert.IsType<OkObjectResult>(result2);
            var svc2 = Assert.IsType<ServiceDTO>(ok2.Value);
            Assert.Equal("S2", svc2.Name);
        }

        [Fact]
        public async Task GetServiceById_DefaultUser_ReturnsActiveServiceOrNotFound()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "2") }, "Test");
            var controller = CreateController(identity);
            
            var result1 = await controller.GetServiceById(_fixture.ServiceId1);
            var result2 = await controller.GetServiceById(_fixture.ServiceId2);
            
            var ok = Assert.IsType<OkObjectResult>(result1);
            var svc = Assert.IsType<ServiceDTO>(ok.Value);
            Assert.Equal("S1", svc.Name);

            Assert.IsType<NotFoundResult>(result2);
        }

        [Fact]
        public async Task GetServicesByGymId_UnauthenticatedUser_ReturnsUnauthorized()
        {
            var controller = CreateController();
            var result = await controller.GetServicesByGymId(_fixture.GymId1);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetServicesByGymId_AuthenticatedUser_ReturnsActiveServices()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "2") }, "Test");
            var controller = CreateController(identity);
            var result = await controller.GetServicesByGymId(_fixture.GymId1);
            
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<ServiceDTO>>(ok.Value);
            Assert.Single(list);
            var item = System.Linq.Enumerable.First(list);
            Assert.Equal("S1", item.Name);
        }

        [Fact]
        public async Task CreateService_UnauthenticatedUser_ReturnsForbidden()
        {
            var controller = CreateController(null);
            var dto = new ServiceDTO { Name = "New", Description = "Desc", GymId = _fixture.GymId1, Price = 20m, Duration = TimeSpan.FromMinutes(45), ServiceType = "Type", IsActive = true };
            var result = await controller.CreateService(dto);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task CreateService_AdminUser_ReturnsCreated()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Test");
            var controller = CreateController(identity);
            var dto = new ServiceDTO { Name = "NewService", Description = "Desc", GymId = _fixture.GymId1, Price = 20m, Duration = TimeSpan.FromMinutes(45), ServiceType = "TypeNew", IsActive = true };
            var result = await controller.CreateService(dto);
            
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var svc = Assert.IsType<ServiceDTO>(created.Value);
            Assert.Equal("NewService", svc.Name);
            Assert.Equal(20m, svc.Price);
        }

        [Fact]
        public async Task UpdateService_UnauthenticatedUser_ReturnsForbidden()
        {
            var controller = CreateController(null);
            var dto = new ServiceDTO { Name = "Updated", Description = "Desc", GymId = _fixture.GymId1, Price = 25m, Duration = TimeSpan.FromMinutes(45), ServiceType = "Type", IsActive = true };
            var result = await controller.UpdateService(_fixture.ServiceId1, dto);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UpdateService_AdminUser_ReturnsOk()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Test");
            var controller = CreateController(identity);
            var dto = new ServiceDTO { Name = "UpdatedService", Description = "Updated", GymId = _fixture.GymId1, Price = 25m, Duration = TimeSpan.FromMinutes(45), ServiceType = "Type1", IsActive = true };
            var result = await controller.UpdateService(_fixture.ServiceId1, dto);
            
            var ok = Assert.IsType<OkObjectResult>(result);
            var svc = Assert.IsType<ServiceDTO>(ok.Value);
            Assert.Equal("UpdatedService", svc.Name);
            Assert.Equal(25m, svc.Price);
        }

        [Fact]
        public async Task DeleteService_UnauthenticatedUser_ReturnsForbidden()
        {
            var controller = CreateController(null);
            var result = await controller.DeleteService(_fixture.ServiceId1);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task DeleteService_AdminUser_ReturnsOk()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Test");
            var controller = CreateController(identity);
            var result = await controller.DeleteService(_fixture.ServiceId1);
            Assert.IsType<OkObjectResult>(result);
        }
    }
}