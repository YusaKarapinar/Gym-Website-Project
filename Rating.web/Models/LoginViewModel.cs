using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Rating.web.Models;

public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
