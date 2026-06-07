using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<MaintenancePlan> MaintenancePlans { get; set; }
    public DbSet<FaultReport> FaultReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Device>()
            .HasIndex(d => d.DeviceCode)
            .IsUnique();

        modelBuilder.Entity<Device>()
            .Property(d => d.Status)
            .HasConversion<string>();

        modelBuilder.Entity<MaintenancePlan>()
            .HasIndex(p => p.PlanCode)
            .IsUnique();

        modelBuilder.Entity<MaintenancePlan>()
            .Property(p => p.Status)
            .HasConversion<string>();

        modelBuilder.Entity<MaintenancePlan>()
            .Property(p => p.Cycle)
            .HasConversion<string>();

        modelBuilder.Entity<MaintenancePlan>()
            .HasOne(p => p.Device)
            .WithMany(d => d.MaintenancePlans)
            .HasForeignKey(p => p.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenancePlan>()
            .HasOne(p => p.ResponsibleTechnician)
            .WithMany()
            .HasForeignKey(p => p.ResponsibleTechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FaultReport>()
            .HasIndex(f => f.ReportCode)
            .IsUnique();

        modelBuilder.Entity<FaultReport>()
            .Property(f => f.Status)
            .HasConversion<string>();

        modelBuilder.Entity<FaultReport>()
            .Property(f => f.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<FaultReport>()
            .HasOne(f => f.Device)
            .WithMany(d => d.FaultReports)
            .HasForeignKey(f => f.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FaultReport>()
            .HasOne(f => f.Reporter)
            .WithMany()
            .HasForeignKey(f => f.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FaultReport>()
            .HasOne(f => f.AssignedTechnician)
            .WithMany()
            .HasForeignKey(f => f.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
