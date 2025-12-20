using System.ComponentModel.DataAnnotations;

namespace Project.web.Models;

public class RegisterViewModel
{
    [Required]
    [MinLength(3)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Member"; 

    public int? GymId { get; set; }


}
