using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<MaintenancePlan> MaintenancePlans { get; set; }
    public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
    public DbSet<FaultReport> FaultReports { get; set; }
    public DbSet<SparePart> SpareParts { get; set; }
    public DbSet<SparePartConsumption> SparePartConsumptions { get; set; }
    public DbSet<InspectionPlan> InspectionPlans { get; set; }
    public DbSet<InspectionTask> InspectionTasks { get; set; }
    public DbSet<InspectionRecord> InspectionRecords { get; set; }
    public DbSet<InspectionPhoto> InspectionPhotos { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<MaintenanceContract> MaintenanceContracts { get; set; }
    public DbSet<DeviceBorrowRecord> DeviceBorrowRecords { get; set; }
    public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles { get; set; }

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

        modelBuilder.Entity<MaintenancePlan>()
            .HasOne(p => p.MaintenanceSchedule)
            .WithMany(s => s.MaintenancePlans)
            .HasForeignKey(p => p.MaintenanceScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MaintenanceSchedule>()
            .HasIndex(s => s.ScheduleCode)
            .IsUnique();

        modelBuilder.Entity<MaintenanceSchedule>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<MaintenanceSchedule>()
            .Property(s => s.Cycle)
            .HasConversion<string>();

        modelBuilder.Entity<MaintenanceSchedule>()
            .HasOne(s => s.Device)
            .WithMany()
            .HasForeignKey(s => s.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenanceSchedule>()
            .HasOne(s => s.ResponsibleTechnician)
            .WithMany()
            .HasForeignKey(s => s.ResponsibleTechnicianId)
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

        modelBuilder.Entity<SparePart>()
            .HasIndex(s => new { s.DeviceId, s.Name, s.Specification })
            .IsUnique();

        modelBuilder.Entity<SparePart>()
            .HasOne(s => s.Device)
            .WithMany(d => d.SpareParts)
            .HasForeignKey(s => s.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SparePartConsumption>()
            .HasOne(c => c.SparePart)
            .WithMany(s => s.Consumptions)
            .HasForeignKey(c => c.SparePartId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SparePartConsumption>()
            .HasOne(c => c.FaultReport)
            .WithMany(f => f.SparePartConsumptions)
            .HasForeignKey(c => c.FaultReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InspectionPlan>()
            .HasIndex(p => p.PlanCode)
            .IsUnique();

        modelBuilder.Entity<InspectionPlan>()
            .Property(p => p.Status)
            .HasConversion<string>();

        modelBuilder.Entity<InspectionPlan>()
            .Property(p => p.Cycle)
            .HasConversion<string>();

        modelBuilder.Entity<InspectionPlan>()
            .HasOne(p => p.Device)
            .WithMany(d => d.InspectionPlans)
            .HasForeignKey(p => p.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InspectionPlan>()
            .HasOne(p => p.AssignedTechnician)
            .WithMany()
            .HasForeignKey(p => p.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InspectionTask>()
            .HasIndex(t => t.TaskCode)
            .IsUnique();

        modelBuilder.Entity<InspectionTask>()
            .Property(t => t.Status)
            .HasConversion<string>();

        modelBuilder.Entity<InspectionTask>()
            .HasOne(t => t.InspectionPlan)
            .WithMany(p => p.InspectionTasks)
            .HasForeignKey(t => t.InspectionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InspectionTask>()
            .HasOne(t => t.Device)
            .WithMany(d => d.InspectionTasks)
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InspectionTask>()
            .HasOne(t => t.AssignedTechnician)
            .WithMany()
            .HasForeignKey(t => t.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InspectionRecord>()
            .HasIndex(r => r.RecordCode)
            .IsUnique();

        modelBuilder.Entity<InspectionRecord>()
            .Property(r => r.DeviceStatus)
            .HasConversion<string>();

        modelBuilder.Entity<InspectionRecord>()
            .Property(r => r.Result)
            .HasConversion<string>();

        modelBuilder.Entity<InspectionRecord>()
            .HasOne(r => r.InspectionTask)
            .WithMany(t => t.InspectionRecords)
            .HasForeignKey(r => r.InspectionTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InspectionRecord>()
            .HasOne(r => r.InspectionPlan)
            .WithMany()
            .HasForeignKey(r => r.InspectionPlanId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InspectionRecord>()
            .HasOne(r => r.Device)
            .WithMany(d => d.InspectionRecords)
            .HasForeignKey(r => r.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InspectionRecord>()
            .HasOne(r => r.Inspector)
            .WithMany()
            .HasForeignKey(r => r.InspectorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InspectionPhoto>()
            .HasOne(p => p.InspectionRecord)
            .WithMany(r => r.Photos)
            .HasForeignKey(p => p.InspectionRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.CreatedAt });

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.Status, n.CreatedAt });

        modelBuilder.Entity<Notification>()
            .Property(n => n.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(n => n.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(n => n.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .Property(n => n.RelatedEntityType)
            .HasConversion<string>();

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<Supplier>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Device>()
            .HasOne(d => d.Supplier)
            .WithMany(s => s.Devices)
            .HasForeignKey(d => d.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MaintenanceContract>()
            .HasIndex(c => c.ContractCode)
            .IsUnique();

        modelBuilder.Entity<MaintenanceContract>()
            .HasOne(c => c.Device)
            .WithMany(d => d.MaintenanceContracts)
            .HasForeignKey(c => c.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenanceContract>()
            .HasOne(c => c.Supplier)
            .WithMany()
            .HasForeignKey(c => c.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MaintenanceContract>()
            .HasIndex(c => c.EndDate);

        modelBuilder.Entity<MaintenanceContract>()
            .HasIndex(c => new { c.DeviceId, c.EndDate });

        modelBuilder.Entity<DeviceBorrowRecord>()
            .HasIndex(r => r.RecordCode)
            .IsUnique();

        modelBuilder.Entity<DeviceBorrowRecord>()
            .Property(r => r.BorrowType)
            .HasConversion<string>();

        modelBuilder.Entity<DeviceBorrowRecord>()
            .Property(r => r.StatusBeforeBorrow)
            .HasConversion<string>();

        modelBuilder.Entity<DeviceBorrowRecord>()
            .Property(r => r.ApprovalStatus)
            .HasConversion<string>();

        modelBuilder.Entity<DeviceBorrowRecord>()
            .HasOne(r => r.Device)
            .WithMany(d => d.BorrowRecords)
            .HasForeignKey(r => r.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeviceBorrowRecord>()
            .HasOne(r => r.Operator)
            .WithMany()
            .HasForeignKey(r => r.OperatorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DeviceBorrowRecord>()
            .HasOne(r => r.ReturnOperator)
            .WithMany()
            .HasForeignKey(r => r.ReturnOperatorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DeviceBorrowRecord>()
            .HasOne(r => r.Approver)
            .WithMany()
            .HasForeignKey(r => r.ApproverId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DeviceBorrowRecord>()
            .HasOne(r => r.Applicant)
            .WithMany()
            .HasForeignKey(r => r.ApplicantId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DeviceBorrowRecord>()
            .HasIndex(r => new { r.DeviceId, r.IsReturned });

        modelBuilder.Entity<DeviceBorrowRecord>()
            .HasIndex(r => r.BorrowTime);

        modelBuilder.Entity<DeviceBorrowRecord>()
            .HasIndex(r => r.ApprovalStatus);

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .HasIndex(a => a.ArticleCode)
            .IsUnique();

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .Property(a => a.Status)
            .HasConversion<string>();

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .HasOne(a => a.Device)
            .WithMany()
            .HasForeignKey(a => a.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .HasOne(a => a.Author)
            .WithMany()
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .HasIndex(a => new { a.DeviceId, a.Status });

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .HasIndex(a => a.Status);
    }
}
