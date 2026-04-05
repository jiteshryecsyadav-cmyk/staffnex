using staffnex.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace staffnex.Api.Data;

public static class AppDbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;

        if (!context.Departments.Any())
        {
            context.Departments.AddRange(
                new Department { Name = "Human Resources", CreatedAt = now },
                new Department { Name = "Operations", CreatedAt = now },
                new Department { Name = "Sales", CreatedAt = now },
                new Department { Name = "IT", CreatedAt = now },
                new Department { Name = "Accounts", CreatedAt = now });

            await context.SaveChangesAsync();
        }

        var adminExists = context.Users.Any(user => user.Role == "Admin" || user.Username == "admin");
        if (!adminExists)
        {
            context.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                IsActive = true,
                CreatedAt = now
            });
        }

        var sampleStaffExists = await context.Staffs.AnyAsync();
        if (!sampleStaffExists)
        {
            var departments = await context.Departments
                .ToDictionaryAsync(department => department.Name, department => department.Id);

            var staffMembers = new List<Staff>
            {
                new()
                {
                    EmployeeId = "EMP001",
                    FullName = "Raj Verma",
                    Phone = "9876543210",
                    Email = "raj.verma@staffnex.local",
                    Designation = "Field Executive",
                    DepartmentId = departments.GetValueOrDefault("Operations"),
                    MonthlySalary = 32000m,
                    JoinDate = new DateTime(2025, 1, 10),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    EmployeeId = "EMP002",
                    FullName = "Pooja Sharma",
                    Phone = "9876501234",
                    Email = "pooja.sharma@staffnex.local",
                    Designation = "HR Executive",
                    DepartmentId = departments.GetValueOrDefault("Human Resources"),
                    MonthlySalary = 38000m,
                    JoinDate = new DateTime(2024, 11, 4),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    EmployeeId = "EMP003",
                    FullName = "Amit Singh",
                    Phone = "9811112233",
                    Email = "amit.singh@staffnex.local",
                    Designation = "Sales Officer",
                    DepartmentId = departments.GetValueOrDefault("Sales"),
                    MonthlySalary = 35000m,
                    JoinDate = new DateTime(2025, 2, 15),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            };

            context.Staffs.AddRange(staffMembers);
            await context.SaveChangesAsync();

            var sampleUsers = new List<User>
            {
                new()
                {
                    Username = "raj.staff",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                    Role = "Staff",
                    StaffId = staffMembers[0].Id,
                    IsActive = true,
                    CreatedAt = now
                },
                new()
                {
                    Username = "pooja.staff",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                    Role = "Staff",
                    StaffId = staffMembers[1].Id,
                    IsActive = true,
                    CreatedAt = now
                },
                new()
                {
                    Username = "amit.staff",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                    Role = "Staff",
                    StaffId = staffMembers[2].Id,
                    IsActive = true,
                    CreatedAt = now
                }
            };

            context.Users.AddRange(sampleUsers);
        }

        await context.SaveChangesAsync();
    }
}