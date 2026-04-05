using Microsoft.EntityFrameworkCore;
using staffnex.Api.Models;

namespace staffnex.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
    public DbSet<LocationTrail> LocationTrails => Set<LocationTrail>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>()
            .Property(department => department.Name)
            .HasMaxLength(150);

        modelBuilder.Entity<Staff>()
            .HasIndex(staff => staff.EmployeeId)
            .IsUnique();

        modelBuilder.Entity<Staff>()
            .Property(staff => staff.MonthlySalary)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(user => user.Staff)
            .WithOne(staff => staff.User)
            .HasForeignKey<User>(user => user.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(token => token.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AttendanceLog>()
            .HasIndex(log => new { log.StaffId, log.LogDate })
            .IsUnique();

        modelBuilder.Entity<AttendanceLog>()
            .Property(log => log.CheckInLat)
            .HasColumnType("decimal(9,6)");

        modelBuilder.Entity<AttendanceLog>()
            .Property(log => log.CheckInLng)
            .HasColumnType("decimal(9,6)");

        modelBuilder.Entity<AttendanceLog>()
            .Property(log => log.CheckOutLat)
            .HasColumnType("decimal(9,6)");

        modelBuilder.Entity<AttendanceLog>()
            .Property(log => log.CheckOutLng)
            .HasColumnType("decimal(9,6)");

        modelBuilder.Entity<AttendanceLog>()
            .Property(log => log.WorkingHours)
            .HasColumnType("decimal(6,2)");

        modelBuilder.Entity<AttendanceLog>()
            .HasOne(log => log.Staff)
            .WithMany(staff => staff.AttendanceLogs)
            .HasForeignKey(log => log.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LocationTrail>()
            .Property(trail => trail.Latitude)
            .HasColumnType("decimal(9,6)");

        modelBuilder.Entity<LocationTrail>()
            .Property(trail => trail.Longitude)
            .HasColumnType("decimal(9,6)");

        modelBuilder.Entity<LocationTrail>()
            .HasOne(trail => trail.Staff)
            .WithMany(staff => staff.LocationTrails)
            .HasForeignKey(trail => trail.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeaveRequest>()
            .Property(request => request.Reason)
            .HasMaxLength(500);

        modelBuilder.Entity<LeaveRequest>()
            .Property(request => request.ActionRemarks)
            .HasMaxLength(500);

        modelBuilder.Entity<LeaveRequest>()
            .HasOne(request => request.Staff)
            .WithMany()
            .HasForeignKey(request => request.StaffId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}