using System.ComponentModel.DataAnnotations;

namespace Project.web.Models;

public class RegisterViewModel
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string? Role { get; set; } 

    public int? GymId { get; set; }


}
