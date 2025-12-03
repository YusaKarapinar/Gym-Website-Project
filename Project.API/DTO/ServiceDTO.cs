using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Project.API.DTO
{
    public class ServiceDTO
    {
        public int ServiceId { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string ServiceType { get; set; }
        public decimal Price { get; set; }
        public TimeSpan Duration { get; set; }
        public int GymId { get; set; }
        public string? GymName { get; set; }
        public bool IsActive { get; set; }
    }
}