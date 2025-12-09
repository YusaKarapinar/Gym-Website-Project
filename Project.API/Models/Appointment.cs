using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.API.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public TimeSpan Time { get; set; }
        
        
        [ForeignKey("Member")]
        public int UserId { get; set; }
        public AppUser? Member { get; set; }
        
        [ForeignKey("Trainer")]
        public int TrainerId { get; set; }
        public AppUser? Trainer { get; set; }

        [ForeignKey("Service")]
        public int ServiceId { get; set; }
        public Service? Service { get; set; }
        
        [ForeignKey("Gym")]
        public int GymId { get; set; }
        public Gym? Gym { get; set; }
        
        public string Status { get; set; } = AppointmentStatus.Pending;

        public decimal Price { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? CanceledBy { get; set; }
        
        public DateTime? CanceledAt { get; set; }
    }
}