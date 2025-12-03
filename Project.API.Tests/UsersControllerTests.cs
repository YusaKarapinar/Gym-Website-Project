using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Project.API.Controllers;
using Project.API.DTO;
using Project.API.Models;
using Xunit;

namespace Project.API.Tests
{
    public class UsersControllerTests
    {
        private static Mock<UserManager<AppUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private static Mock<SignInManager<AppUser>> MockSignInManager(Mock<UserManager<AppUser>> um)
        {
            var ctx = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
            var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<SignInManager<AppUser>>>();
            var schemes = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            var confirmation = new Mock<IUserConfirmation<AppUser>>();

            return new Mock<SignInManager<AppUser>>(um.Object, ctx.Object, claimsFactory.Object, options.Object, logger.Object, schemes.Object, confirmation.Object);
        }

        [Fact]
        public async Task Register_EmailExists_ReturnsBadRequest()
        {
            var um = MockUserManager();
            um.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new AppUser());
            var sm = MockSignInManager(um);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "AppSettings:Token", "dummy" } }).Build();

            var controller = new UsersController(um.Object, sm.Object, config);
            var result = await controller.Register(new UserDTO { Email = "exists@a.com" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_Success_ReturnsCreated()
        {
            var um = MockUserManager();
            um.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
            um.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            um.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            var sm = MockSignInManager(um);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "AppSettings:Token", "dummy" } }).Build();

            var controller = new UsersController(um.Object, sm.Object, config);
            var dto = new UserDTO { Email = "new@u.com", UserName = "u", Password = "P@ss" };

            var result = await controller.Register(dto);

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Login_Success_ReturnsOkWithToken()
        {
            var user = new AppUser { Id = 1, UserName = "u1", Email = "u1@t.com" };
            var um = MockUserManager();
            um.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            um.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string>());

            var sm = MockSignInManager(um);
            sm.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "AppSettings:Token", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" } }).Build();

            var controller = new UsersController(um.Object, sm.Object, config);
            var result = await controller.Login(new LoginDTO { Username = "u1", Password = "p" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsBadRequest()
        {
            var user = new AppUser { Id = 1, UserName = "u1", Email = "u1@t.com" };
            var um = MockUserManager();
            um.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            um.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string>());

            var sm = MockSignInManager(um);
            sm.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "AppSettings:Token", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" } }).Build();

            var controller = new UsersController(um.Object, sm.Object, config);
            var result = await controller.Login(new LoginDTO { Username = "u1", Password = "wrongpassword" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_UserNotFound_ReturnsBadRequest()
        {
            var um = MockUserManager();
            um.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
            um.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

            var sm = MockSignInManager(um);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "AppSettings:Token", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" } }).Build();

            var controller = new UsersController(um.Object, sm.Object, config);
            var result = await controller.Login(new LoginDTO { Username = "nonexistent", Password = "p" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_WithEmail_ReturnsOkWithToken()
        {
            var user = new AppUser { Id = 1, UserName = "u1", Email = "u1@t.com" };
            var um = MockUserManager();
            um.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
            um.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            um.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string>());

            var sm = MockSignInManager(um);
            sm.Setup(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "AppSettings:Token", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" } }).Build();

            var controller = new UsersController(um.Object, sm.Object, config);
            var result = await controller.Login(new LoginDTO { Username = "u1@t.com", Password = "p" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task Register_PasswordMismatch_ReturnsBadRequest()
        {
            var um = MockUserManager();
            um.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
            um.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));
            
            var sm = MockSignInManager(um);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "AppSettings:Token", "dummy" } }).Build();

            var controller = new UsersController(um.Object, sm.Object, config);
            var dto = new UserDTO { Email = "new@u.com", UserName = "u", Password = "P@ss" };

            var result = await controller.Register(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
