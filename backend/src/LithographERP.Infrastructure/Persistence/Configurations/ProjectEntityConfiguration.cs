using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Employees;
using LithographERP.Domain.Modules.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LithographERP.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> entity)
    {
        entity.ToTable("projects", "projects", table =>
        {
            table.HasCheckConstraint(
                "projects_status_ck",
                "status IN ('draft', 'active', 'on_hold', 'completed', 'cancelled')");
        });
        entity.HasKey(project => project.Id);
        entity.Property(project => project.Id).HasColumnName("id");
        entity.Property(project => project.BusinessId).HasColumnName("business_id").HasMaxLength(30).IsRequired();
        entity.Property(project => project.ClientId).HasColumnName("client_id");
        entity.Property(project => project.Name).HasColumnName("name").HasMaxLength(250).IsRequired();
        entity.Property(project => project.Description).HasColumnName("description");
        entity.Property(project => project.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        entity.Property(project => project.StartDate).HasColumnName("start_date");
        entity.Property(project => project.Deadline).HasColumnName("deadline");
        entity.Property(project => project.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(project => project.CreatedBy).HasColumnName("created_by");
        entity.Property(project => project.UpdatedAt).HasColumnName("updated_at");
        entity.Property(project => project.UpdatedBy).HasColumnName("updated_by");
        entity.HasIndex(project => project.BusinessId).IsUnique().HasDatabaseName("projects_business_id_uq");
        entity.HasIndex(project => project.ClientId).HasDatabaseName("projects_client_id_idx");
        entity.HasOne(project => project.Client).WithMany().HasForeignKey(project => project.ClientId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("projects_client_id_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(project => project.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("projects_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(project => project.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("projects_updated_by_fkey");
    }
}

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> entity)
    {
        entity.ToTable("project_members", "projects", table =>
        {
            table.HasCheckConstraint(
                "project_members_role_ck",
                "project_role IN ('owner', 'assignee', 'participant', 'observer')");
        });
        entity.HasKey(member => new { member.ProjectId, member.EmployeeId, member.ProjectRole });
        entity.Property(member => member.ProjectId).HasColumnName("project_id");
        entity.Property(member => member.EmployeeId).HasColumnName("employee_id");
        entity.Property(member => member.ProjectRole).HasColumnName("project_role").HasMaxLength(30).IsRequired();
        entity.Property(member => member.AssignedAt).HasColumnName("assigned_at").IsRequired();
        entity.Property(member => member.AssignedBy).HasColumnName("assigned_by");
        // EF Core keeps a single index for the same columns, so the owner partial unique index is created in AddProjectsModule.
        entity.HasIndex(member => member.ProjectId)
            .IsUnique()
            .HasFilter("project_role = 'assignee'")
            .HasDatabaseName("project_members_one_assignee_uq");
        entity.HasOne(member => member.Project).WithMany(project => project.Members).HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("project_members_project_id_fkey");
        entity.HasOne(member => member.Employee).WithMany().HasForeignKey(member => member.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("project_members_employee_id_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(member => member.AssignedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("project_members_assigned_by_fkey");
    }
}
