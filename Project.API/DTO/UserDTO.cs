using System.ComponentModel.DataAnnotations;
using Project.API.Constants;

namespace Project.API.DTO
{
    public class UserDTO
    {
        public int Id { get; set; }
        [Required]
        [MinLength(3)]
        public string UserName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(3)]
        public string Password { get; set; } = null!;
        public string? Bio { get; set; }
        [Required]
        public string Role { get; set; } = Roles.Member;
        public int? GymId { get; set; }
        public string? GymName { get; set; }
    }
}