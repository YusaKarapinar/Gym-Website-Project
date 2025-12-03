using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.web.Models
{
    public class ServiceViewModel
    {
        public int ServiceId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ServiceType { get; set; }
        public decimal Price { get; set; }
        public TimeSpan Duration { get; set; }
        public int GymId { get; set; }
        public bool IsActive { get; set; } = true;
        
        // For displaying gym name in views
        public string? GymName { get; set; }
    }
}