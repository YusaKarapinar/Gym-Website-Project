using System;
using System.ComponentModel.DataAnnotations;

namespace Project.API.DTO
{
    public class AppointmentDTO
    {
        public int AppointmentId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        [Required]
        public int UserId { get; set; }
        public string? UserName { get; set; }

        [Required]
        public int TrainerId { get; set; }
        public string? TrainerName { get; set; }

        [Required]
        public int ServiceId { get; set; }
        public string? ServiceName { get; set; }

        [Required]
        public int GymId { get; set; }
        public string? GymName { get; set; }

        public string? Status { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CanceledBy { get; set; }
        public DateTime? CanceledAt { get; set; }
    }
}