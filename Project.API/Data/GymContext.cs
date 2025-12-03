using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project.API.Models;

namespace Project.API.Data
{
    public class GymContext : IdentityDbContext<AppUser, AppRole, int>
    {
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Gym> Gyms { get; set; }
        public DbSet<Service> Services { get; set; }



        public GymContext(DbContextOptions<GymContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // UserName için unique constraint'i kaldır
            builder.Entity<AppUser>()
                .HasIndex(u => u.NormalizedUserName)
                .IsUnique(false);

            // Email için unique constraint'i koru
            builder.Entity<AppUser>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique(true);

            // Configure UserRoles relationships
            builder.Entity<AppUser>()
                .HasMany(u => u.UserRoles)
                .WithOne()
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            builder.Entity<AppRole>()
                .HasMany(r => r.UserRoles)
                .WithOne()
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            // Seed data
            builder.Entity<AppRole>().HasData(
                new AppRole { Id = 1, Name = "Admin", NormalizedName = "ADMIN" },
                new AppRole { Id = 2, Name = "Trainer", NormalizedName = "TRAINER" },
                new AppRole { Id = 3, Name = "Member", NormalizedName = "MEMBER" }
            );

            // Note: Appointment seed data removed because it references users that don't exist yet
            // You can create appointments after users are registered
        }
    }
}
