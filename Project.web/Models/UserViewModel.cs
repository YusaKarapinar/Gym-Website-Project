using System;

namespace Project.web.Models
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string? Role { get; set; }
        public int? GymId { get; set; }
        public string? GymName { get; set; }
    }
}
