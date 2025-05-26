using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Models;

namespace WorkshopManager.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ServiceOrder> ServiceOrders { get; set; }
        public DbSet<ServiceTask> ServiceTasks { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<UsedPart> UsedParts { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // VIN i numer rejestracyjny muszą być unikalne
            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.Vin)
                .IsUnique();
            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.RegistrationNumber)
                .IsUnique();

            // Relacja Customer - Vehicle (1:N)
            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Customer)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(v => v.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacja Vehicle - ServiceOrder (1:N)
            modelBuilder.Entity<ServiceOrder>()
                .HasOne(so => so.Vehicle)
                .WithMany(v => v.ServiceOrders)
                .HasForeignKey(so => so.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacja ServiceOrder - ServiceTask (1:N)
            modelBuilder.Entity<ServiceTask>()
                .HasOne(st => st.ServiceOrder)
                .WithMany(so => so.ServiceTasks)
                .HasForeignKey(st => st.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacja ServiceTask - UsedPart (1:N)
            modelBuilder.Entity<UsedPart>()
                .HasOne(up => up.ServiceTask)
                .WithMany(st => st.UsedParts)
                .HasForeignKey(up => up.ServiceTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacja Part - UsedPart (1:N)
            modelBuilder.Entity<UsedPart>()
                .HasOne(up => up.Part)
                .WithMany(p => p.UsedParts)
                .HasForeignKey(up => up.PartId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacja ServiceOrder - Comment (1:N)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ServiceOrder)
                .WithMany(so => so.Comments)
                .HasForeignKey(c => c.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

