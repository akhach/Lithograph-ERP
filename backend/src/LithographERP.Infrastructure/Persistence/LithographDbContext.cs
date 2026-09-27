using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Calculator;
using LithographERP.Domain.Modules.Clients;
using LithographERP.Domain.Modules.Employees;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Domain.Modules.Projects;
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

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<OrderType> OrderTypes => Set<OrderType>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    public DbSet<FolderLink> FolderLinks => Set<FolderLink>();

    public DbSet<CalculatorTemplate> CalculatorTemplates => Set<CalculatorTemplate>();

    public DbSet<CalculatorTemplateVersion> CalculatorTemplateVersions => Set<CalculatorTemplateVersion>();

    public DbSet<OrderCalculator> OrderCalculators => Set<OrderCalculator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>("client_business_id_seq", "clients").StartsAt(1).IncrementsBy(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LithographDbContext).Assembly);
    }
}
