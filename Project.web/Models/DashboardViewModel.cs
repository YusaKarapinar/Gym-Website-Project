using System.Collections.Generic;

namespace Project.web.Models
{
    public class DashboardViewModel
    {
        public List<GymViewModel> Gyms { get; set; } = new();
        public List<ServiceViewModel> Services { get; set; } = new();
        public List<UserViewModel> Users { get; set; } = new();
        public List<AppointmentViewModel> Appointments { get; set; } = new();
        
        public int TotalGyms => Gyms.Count;
        public int ActiveGyms => Gyms.Count(g => g.IsActive);
        
        public int TotalServices => Services.Count;
        public int ActiveServices => Services.Count(s => s.IsActive);
        
        public int TotalUsers => Users.Count;
        public int TotalTrainers => Users.Count(u => u.Role == "Trainer");
        public int TotalMembers => Users.Count(u => u.Role == "Member");
        
        public int TotalAppointments => Appointments.Count;
        public int PendingAppointments => Appointments.Count(a => a.Status == "Pending");
        public int ApprovedAppointments => Appointments.Count(a => a.Status == "Approved");
    }
}
