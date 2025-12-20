using System;
using System.ComponentModel.DataAnnotations;

namespace Project.API.DTO
{
    public class ServiceDTO
    {
        public int ServiceId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string ServiceType { get; set; }
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        [Required]
        public TimeSpan Duration { get; set; }
        [Required]
        public int GymId { get; set; }
        public string? GymName { get; set; }
        public bool IsActive { get; set; }
    }
}