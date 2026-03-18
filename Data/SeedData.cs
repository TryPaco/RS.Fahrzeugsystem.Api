using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Models;
using System.Text.Json;

namespace RS.Fahrzeugsystem.Api.Data;

public static class SeedData
{
    private sealed record VehicleCatalogSeedItem(
        string? brand,
        string? model,
        string? year,
        string? engine,
        string? engine_code);

    public static async Task EnsureSeededAsync(AppDbContext dbContext)
    {
        await dbContext.Database.MigrateAsync();

        var permissions = new[]
        {
            "customers.view","customers.create","customers.edit","customers.delete",
            "vehicles.view","vehicles.create","vehicles.edit","vehicles.delete",
            "labels.view","labels.manage","labels.assign",
            "parts.view","parts.create","parts.edit","parts.delete",
            "history.view","history.create","history.edit","history.delete",
            "users.view","users.manage","roles.manage",
            "settings.manage",
            "vehiclecatalog.view","vehiclecatalog.manage"
        };

        var existingPermissionKeys = await dbContext.Permissions
            .Select(x => x.Key)
            .ToListAsync();

        var missingPermissions = permissions
            .Except(existingPermissionKeys)
            .Select(key => new Permission
            {
                Key = key,
                Description = key
            })
            .ToList();

        if (missingPermissions.Count > 0)
        {
            dbContext.Permissions.AddRange(missingPermissions);
            await dbContext.SaveChangesAsync();
        }

        var roleDefinitions = new[]
        {
            new Role { Name = "Superadmin", Description = "Voller Zugriff" },
            new Role { Name = "Admin", Description = "Verwaltung" },
            new Role { Name = "Mitarbeiter", Description = "Werkstatt" },
            new Role { Name = "ReadOnly", Description = "Nur Ansicht" }
        };

        var existingRoles = await dbContext.Roles.ToListAsync();
        var missingRoles = roleDefinitions
            .Where(role => existingRoles.All(existing => existing.Name != role.Name))
            .ToList();

        if (missingRoles.Count > 0)
        {
            dbContext.Roles.AddRange(missingRoles);
            await dbContext.SaveChangesAsync();
            existingRoles = await dbContext.Roles.ToListAsync();
        }

        var allPermissions = await dbContext.Permissions.ToListAsync();
        var superadmin = existingRoles.Single(x => x.Name == "Superadmin");
        var admin = existingRoles.Single(x => x.Name == "Admin");
        var employee = existingRoles.Single(x => x.Name == "Mitarbeiter");
        var readOnly = existingRoles.Single(x => x.Name == "ReadOnly");
        var existingRolePermissions = await dbContext.RolePermissions.ToListAsync();

        EnsureRolePermissions(dbContext, existingRolePermissions, superadmin.Id, allPermissions.Select(x => x.Key), allPermissions);

        var adminPermissionKeys = new[]
        {
            "customers.view","customers.create","customers.edit",
            "vehicles.view","vehicles.create","vehicles.edit",
            "labels.view","labels.manage","labels.assign",
            "parts.view","parts.create","parts.edit",
            "history.view","history.create","history.edit",
            "users.view",
            "vehiclecatalog.view","vehiclecatalog.manage"
        };
        EnsureRolePermissions(dbContext, existingRolePermissions, admin.Id, adminPermissionKeys, allPermissions);

        var employeePermissionKeys = new[]
        {
            "customers.view",
            "vehicles.view","vehicles.create","vehicles.edit",
            "vehiclecatalog.view",
            "labels.view","labels.assign",
            "parts.view","parts.create","parts.edit",
            "history.view","history.create","history.edit"
        };
        EnsureRolePermissions(dbContext, existingRolePermissions, employee.Id, employeePermissionKeys, allPermissions);

        var readOnlyPermissionKeys = new[]
        {
            "customers.view","vehicles.view","labels.view","parts.view","history.view"
        };
        EnsureRolePermissions(dbContext, existingRolePermissions, readOnly.Id, readOnlyPermissionKeys, allPermissions);

        if (dbContext.ChangeTracker.HasChanges())
        {
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

        if (!await dbContext.VehicleCatalogEntries.AnyAsync())
        {
            var seedPath = Path.Combine(AppContext.BaseDirectory, "Data", "VehicleCatalogSeed.json");
            if (File.Exists(seedPath))
            {
                await using var stream = File.OpenRead(seedPath);
                var items = await JsonSerializer.DeserializeAsync<List<VehicleCatalogSeedItem>>(stream)
                    ?? [];

                dbContext.VehicleCatalogEntries.AddRange(items
                    .Where(x => !string.IsNullOrWhiteSpace(x.brand) && !string.IsNullOrWhiteSpace(x.model))
                    .Select(x => new VehicleCatalogEntry
                    {
                        Brand = x.brand!.Trim(),
                        Model = x.model!.Trim(),
                        YearLabel = string.IsNullOrWhiteSpace(x.year) ? null : x.year.Trim(),
                        BuildYearFrom = ParseYearRangeStart(x.year),
                        BuildYearTo = ParseYearRangeEnd(x.year),
                        Engine = string.IsNullOrWhiteSpace(x.engine) ? null : x.engine.Trim(),
                        EngineCode = string.IsNullOrWhiteSpace(x.engine_code)
                            ? null
                            : x.engine_code.Split('|', 2)[0].Trim(),
                        Notes = string.IsNullOrWhiteSpace(x.engine_code) ? null : x.engine_code.Trim(),
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    }));

                await dbContext.SaveChangesAsync();
            }
        }
    }

    private static int? ParseYearRangeStart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var matches = System.Text.RegularExpressions.Regex.Matches(value, @"\d{4}");
        if (matches.Count == 0)
        {
            return null;
        }

        return int.TryParse(matches[0].Value, out var year) ? year : null;
    }

    private static int? ParseYearRangeEnd(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Contains("->"))
        {
            return null;
        }

        var matches = System.Text.RegularExpressions.Regex.Matches(value, @"\d{4}");
        if (matches.Count < 2)
        {
            return ParseYearRangeStart(value);
        }

        return int.TryParse(matches[^1].Value, out var year) ? year : null;
    }

    private static void EnsureRolePermissions(
        AppDbContext dbContext,
        IReadOnlyCollection<RolePermission> existingRolePermissions,
        Guid roleId,
        IEnumerable<string> permissionKeys,
        IReadOnlyCollection<Permission> allPermissions)
    {
        var permissionIds = allPermissions
            .Where(x => permissionKeys.Contains(x.Key))
            .Select(x => x.Id)
            .ToHashSet();

        var existingPermissionIds = existingRolePermissions
            .Where(x => x.RoleId == roleId)
            .Select(x => x.PermissionId)
            .ToHashSet();

        var missing = permissionIds
            .Except(existingPermissionIds)
            .Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            })
            .ToList();

        if (missing.Count > 0)
        {
            dbContext.RolePermissions.AddRange(missing);
        }
    }
}
