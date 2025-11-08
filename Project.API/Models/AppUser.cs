using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Project.API.Models
{
    public class AppUser : IdentityUser<int>
    {
        public int? GymId { get; set; }
        public Gym? Gym { get; set; }
        public string? Bio { get; set; }
        
        // Navigation properties
        public ICollection<IdentityUserRole<int>> UserRoles { get; set; } = new List<IdentityUserRole<int>>();
    }
}