using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Project.web.Models;

public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = true;
}
