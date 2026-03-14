using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext dbContext)
    {
        await dbContext.Database.MigrateAsync();

        if (!await dbContext.Permissions.AnyAsync())
        {
            var permissions = new[]
            {
                "customers.view","customers.create","customers.edit","customers.delete",
                "vehicles.view","vehicles.create","vehicles.edit","vehicles.delete",
                "labels.view","labels.manage","labels.assign",
                "parts.view","parts.create","parts.edit","parts.delete",
                "history.view","history.create","history.edit","history.delete",
                "users.view","users.manage","roles.manage",
                "settings.manage"
            };

            dbContext.Permissions.AddRange(permissions.Select(key => new Permission
            {
                Key = key,
                Description = key
            }));

            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Roles.AnyAsync())
        {
            var superadmin = new Role { Name = "Superadmin", Description = "Voller Zugriff" };
            var admin = new Role { Name = "Admin", Description = "Verwaltung" };
            var employee = new Role { Name = "Mitarbeiter", Description = "Werkstatt" };
            var readOnly = new Role { Name = "ReadOnly", Description = "Nur Ansicht" };

            dbContext.Roles.AddRange(superadmin, admin, employee, readOnly);
            await dbContext.SaveChangesAsync();

            var allPermissions = await dbContext.Permissions.ToListAsync();
            dbContext.RolePermissions.AddRange(allPermissions.Select(p => new RolePermission
            {
                RoleId = superadmin.Id,
                PermissionId = p.Id
            }));

            var adminPermissionKeys = new[]
            {
                "customers.view","customers.create","customers.edit",
                "vehicles.view","vehicles.create","vehicles.edit",
                "labels.view","labels.manage","labels.assign",
                "parts.view","parts.create","parts.edit",
                "history.view","history.create","history.edit",
                "users.view"
            };

            dbContext.RolePermissions.AddRange(allPermissions
                .Where(p => adminPermissionKeys.Contains(p.Key))
                .Select(p => new RolePermission { RoleId = admin.Id, PermissionId = p.Id }));

            var employeePermissionKeys = new[]
            {
                "customers.view",
                "vehicles.view","vehicles.create","vehicles.edit",
                "labels.view","labels.assign",
                "parts.view","parts.create","parts.edit",
                "history.view","history.create","history.edit"
            };

            dbContext.RolePermissions.AddRange(allPermissions
                .Where(p => employeePermissionKeys.Contains(p.Key))
                .Select(p => new RolePermission { RoleId = employee.Id, PermissionId = p.Id }));

            dbContext.RolePermissions.AddRange(allPermissions
                .Where(p => p.Key is "customers.view" or "vehicles.view" or "labels.view" or "parts.view" or "history.view")
                .Select(p => new RolePermission { RoleId = readOnly.Id, PermissionId = p.Id }));

            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.Users.AnyAsync())
        {
            var superadminRole = await dbContext.Roles.SingleAsync(x => x.Name == "Superadmin");
            var user = new User
            {
                Username = "admin",
                DisplayName = "RS Admin",
                Email = "admin@local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                IsActive = true
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = superadminRole.Id
            });

            await dbContext.SaveChangesAsync();
        }
    }
}
