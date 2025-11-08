using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Project.API.Models
{
    public class AppRole : IdentityRole<int>
    {
        public string Description { get; set; } = string.Empty;
        
        // Navigation properties
        public ICollection<IdentityUserRole<int>> UserRoles { get; set; } = new List<IdentityUserRole<int>>();
    }
}