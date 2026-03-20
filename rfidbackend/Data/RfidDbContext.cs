using Microsoft.EntityFrameworkCore;
using rfidbackend.Entities;

namespace rfidbackend.Data;

public class RfidDbContext : DbContext
{
    public RfidDbContext(DbContextOptions<RfidDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ToolType> ToolTypes => Set<ToolType>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<ReasonForRequest> ReasonsForRequest => Set<ReasonForRequest>();
    public DbSet<Tool> Tools => Set<Tool>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<ToolAssignment> ToolAssignments => Set<ToolAssignment>();
    public DbSet<ToolRemoval> ToolRemovals => Set<ToolRemoval>();
    public DbSet<RfidScanRecord> RfidScanRecords => Set<RfidScanRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            e.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
            e.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.EmployeeId).IsUnique();
            e.HasIndex(u => u.BadgeId).IsUnique();
            e.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
        });

        modelBuilder.Entity<Tool>(e =>
        {
            e.HasIndex(t => t.RfidTag).IsUnique();
            e.HasOne(t => t.ToolType).WithMany(tt => tt.Tools).HasForeignKey(t => t.ToolTypeId);
            e.HasOne(t => t.Area).WithMany(a => a.Tools).HasForeignKey(t => t.AreaId);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasOne(t => t.ReasonForRequest).WithMany(r => r.Tickets).HasForeignKey(t => t.ReasonForRequestId);
            e.HasOne(t => t.ToolType).WithMany(tt => tt.Tickets).HasForeignKey(t => t.ToolTypeId);
            e.HasOne(t => t.Area).WithMany(a => a.Tickets).HasForeignKey(t => t.AreaId);
            e.HasOne(t => t.CreatedByUser).WithMany(u => u.Tickets).HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ToolAssignment>(e =>
        {
            e.HasOne(ta => ta.User).WithMany(u => u.ToolAssignments).HasForeignKey(ta => ta.UserId);
            e.HasOne(ta => ta.Tool).WithMany(t => t.ToolAssignments).HasForeignKey(ta => ta.ToolId);
            e.HasOne(ta => ta.Ticket).WithOne(t => t!.ToolAssignment).HasForeignKey<ToolAssignment>(ta => ta.TicketId);
        });

        modelBuilder.Entity<ToolRemoval>(e =>
        {
            e.HasOne(tr => tr.ReasonForRequest).WithMany(r => r.ToolRemovals).HasForeignKey(tr => tr.ReasonForRequestId);
            e.HasOne(tr => tr.Tool).WithMany(t => t.ToolRemovals).HasForeignKey(tr => tr.ToolId);
        });

        modelBuilder.Entity<RfidScanRecord>(e =>
        {
            e.HasIndex(r => r.TagId);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Normal" }
        );

        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1, Name = "New Tool" },
            new Permission { Id = 2, Name = "Request Tool" },
            new Permission { Id = 3, Name = "Assign Tool" },
            new Permission { Id = 4, Name = "Tool Removal" },
            new Permission { Id = 5, Name = "Maintenance Required" },
            new Permission { Id = 6, Name = "RFID Scan" },
            new Permission { Id = 7, Name = "Ticket System" }
        );

        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = 1, PermissionId = 1 },
            new RolePermission { RoleId = 1, PermissionId = 2 },
            new RolePermission { RoleId = 1, PermissionId = 3 },
            new RolePermission { RoleId = 1, PermissionId = 4 },
            new RolePermission { RoleId = 1, PermissionId = 5 },
            new RolePermission { RoleId = 1, PermissionId = 6 },
            new RolePermission { RoleId = 1, PermissionId = 7 },
            new RolePermission { RoleId = 2, PermissionId = 2 },
            new RolePermission { RoleId = 2, PermissionId = 5 },
            new RolePermission { RoleId = 2, PermissionId = 7 }
        );

        modelBuilder.Entity<Area>().HasData(
            new Area { Id = 1, Name = "Engineering - New" }
        );
    }
}
