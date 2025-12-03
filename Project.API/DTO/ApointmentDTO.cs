using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project.API.DTO
{
    public class AppointmentDTO
    {
        public int AppointmentId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int TrainerId { get; set; }
        public string? TrainerName { get; set; }
        public int ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public int GymId { get; set; }
        public string? GymName { get; set; }
        public string? Status { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}