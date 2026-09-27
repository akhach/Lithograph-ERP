using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Employees;
using Microsoft.EntityFrameworkCore;

namespace LithographERP.Infrastructure.Persistence;

public sealed class LithographDbContext(DbContextOptions<LithographDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LithographDbContext).Assembly);
    }
}
