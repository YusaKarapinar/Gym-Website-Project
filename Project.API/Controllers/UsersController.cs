using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Project.API.Constants;
using Project.API.DTO;
using Project.API.Models;

namespace Project.API.Controllers 
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;

        public UsersController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users
                .Include(u => u.Gym)
                .ToListAsync();

            var userDtos = new List<UserDTO>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDTO
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    Bio = user.Bio,
                    Role = roles.FirstOrDefault() ?? "Member",
                    GymId = user.GymId,
                    GymName = user.Gym?.Name
                });
            }

            return Ok(userDtos);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserDTO userDto)
        {
            // Email benzersizliği kontrolü
            var existingUserByEmail = await _userManager.FindByEmailAsync(userDto.Email);
            if (existingUserByEmail != null)
            {
                return BadRequest(new { message = "Bu email adresi zaten kullanılıyor." });
            }

            var user = new AppUser
            {
                UserName = userDto.UserName,
                Email = userDto.Email,
                GymId = userDto.GymId
            };
            var result = await _userManager.CreateAsync(user, userDto.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, userDto.Role);
                return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
            }
            else
            {
                return BadRequest(result.Errors);
            }
            
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            // Önce username ile dene
            var user = await _userManager.FindByNameAsync(loginDTO.Username);
            
            // Username bulunamazsa email ile dene
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(loginDTO.Username);
            }
            
            if (user == null)
            {
                return BadRequest("bu kullanıcı adı veya email ile hesap bulunamadı");
            }
            
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDTO.Password, false);
            if (!result.Succeeded)
            {
                return BadRequest("Geçersiz kullanıcı adı veya şifre.");
            }
            return Ok(new { Token = GenerateJWT(user) });
        }

        private object GenerateJWT(AppUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValue = _configuration.GetSection("AppSettings:Token").Value;
            if (string.IsNullOrEmpty(tokenValue))
            {
                throw new InvalidOperationException("JWT token key is not configured.");
            }
            var key = Encoding.ASCII.GetBytes(tokenValue);
            var roles = _userManager.GetRolesAsync(user).Result;
            var claims = new List<Claim>
               {
                  new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                  new Claim(ClaimTypes.Name, user.UserName ?? ""),
                  new Claim(ClaimTypes.Email, user.Email ?? "")
              };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}