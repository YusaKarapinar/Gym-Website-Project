using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Project.API.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(500)]
        public string Description { get; set; }
        public string ServiceType { get; set; }
        public decimal Price { get; set; }
        public int GymId { get; set; }
        public Gym Gym { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
   
        public TimeSpan Duration { get; set; }
    }
}