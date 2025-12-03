using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.web.Models
{
    public class GymViewModel
    {
        public int GymId { get; set; }

        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // For displaying services in details view
        public List<ServiceViewModel>? Services { get; set; }
    }
}