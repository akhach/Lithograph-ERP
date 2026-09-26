using LithographERP.Domain.Modules.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LithographERP.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("users", "auth");
        entity.HasKey(user => user.Id);
        entity.Property(user => user.Id).HasColumnName("id");
        entity.Property(user => user.Username).HasColumnName("username").HasMaxLength(Names.MaxLength).IsRequired();
        entity.Property(user => user.NormalizedUsername).HasColumnName("normalized_username").HasMaxLength(Names.MaxLength).IsRequired();
        entity.Property(user => user.PasswordHash).HasColumnName("password_hash").IsRequired();
        entity.Property(user => user.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        entity.Property(user => user.LastLoginAt).HasColumnName("last_login_at");
        entity.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(user => user.CreatedBy).HasColumnName("created_by");
        entity.Property(user => user.UpdatedAt).HasColumnName("updated_at");
        entity.Property(user => user.UpdatedBy).HasColumnName("updated_by");
        entity.HasIndex(user => user.NormalizedUsername).IsUnique().HasDatabaseName("users_normalized_username_uq");
        entity.HasOne<User>().WithMany().HasForeignKey(user => user.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("users_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(user => user.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("users_updated_by_fkey");
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        entity.ToTable("roles", "auth");
        entity.HasKey(role => role.Id);
        entity.Property(role => role.Id).HasColumnName("id");
        entity.Property(role => role.Name).HasColumnName("name").HasMaxLength(Names.MaxLength).IsRequired();
        entity.Property(role => role.NormalizedName).HasColumnName("normalized_name").HasMaxLength(Names.MaxLength).IsRequired();
        entity.Property(role => role.Description).HasColumnName("description");
        entity.Property(role => role.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
        entity.Property(role => role.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(role => role.CreatedBy).HasColumnName("created_by");
        entity.Property(role => role.UpdatedAt).HasColumnName("updated_at");
        entity.Property(role => role.UpdatedBy).HasColumnName("updated_by");
        entity.HasIndex(role => role.NormalizedName).IsUnique().HasDatabaseName("roles_normalized_name_uq");
        entity.HasOne<User>().WithMany().HasForeignKey(role => role.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("roles_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(role => role.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("roles_updated_by_fkey");
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> entity)
    {
        entity.ToTable("permissions", "auth");
        entity.HasKey(permission => permission.Id);
        entity.Property(permission => permission.Id).HasColumnName("id");
        entity.Property(permission => permission.Code).HasColumnName("code").HasMaxLength(150).IsRequired();
        entity.Property(permission => permission.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        entity.Property(permission => permission.Description).HasColumnName("description");
        entity.Property(permission => permission.Module).HasColumnName("module").HasMaxLength(100).IsRequired();
        entity.Property(permission => permission.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.HasIndex(permission => permission.Code).IsUnique().HasDatabaseName("permissions_code_uq");
        entity.HasIndex(permission => permission.Module).HasDatabaseName("permissions_module_idx");
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> entity)
    {
        entity.ToTable("user_roles", "auth");
        entity.HasKey(assignment => new { assignment.UserId, assignment.RoleId });
        entity.Property(assignment => assignment.UserId).HasColumnName("user_id");
        entity.Property(assignment => assignment.RoleId).HasColumnName("role_id");
        entity.Property(assignment => assignment.AssignedAt).HasColumnName("assigned_at").IsRequired();
        entity.Property(assignment => assignment.AssignedBy).HasColumnName("assigned_by");
        entity.HasIndex(assignment => assignment.RoleId).HasDatabaseName("user_roles_role_id_idx");
        entity.HasOne(assignment => assignment.User).WithMany(user => user.UserRoles).HasForeignKey(assignment => assignment.UserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("user_roles_user_id_fkey");
        entity.HasOne(assignment => assignment.Role).WithMany(role => role.UserRoles).HasForeignKey(assignment => assignment.RoleId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("user_roles_role_id_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(assignment => assignment.AssignedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("user_roles_assigned_by_fkey");
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> entity)
    {
        entity.ToTable("role_permissions", "auth");
        entity.HasKey(assignment => new { assignment.RoleId, assignment.PermissionId });
        entity.Property(assignment => assignment.RoleId).HasColumnName("role_id");
        entity.Property(assignment => assignment.PermissionId).HasColumnName("permission_id");
        entity.Property(assignment => assignment.AssignedAt).HasColumnName("assigned_at").IsRequired();
        entity.Property(assignment => assignment.AssignedBy).HasColumnName("assigned_by");
        entity.HasIndex(assignment => assignment.PermissionId).HasDatabaseName("role_permissions_permission_id_idx");
        entity.HasOne(assignment => assignment.Role).WithMany(role => role.RolePermissions).HasForeignKey(assignment => assignment.RoleId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("role_permissions_role_id_fkey");
        entity.HasOne(assignment => assignment.Permission).WithMany(permission => permission.RolePermissions).HasForeignKey(assignment => assignment.PermissionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("role_permissions_permission_id_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(assignment => assignment.AssignedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("role_permissions_assigned_by_fkey");
    }
}

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> entity)
    {
        entity.ToTable("sessions", "auth");
        entity.HasKey(session => session.Id);
        entity.Property(session => session.Id).HasColumnName("id");
        entity.Property(session => session.UserId).HasColumnName("user_id");
        entity.Property(session => session.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        entity.Property(session => session.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(session => session.ExpiresAt).HasColumnName("expires_at").IsRequired();
        entity.Property(session => session.LastActivityAt).HasColumnName("last_activity_at");
        entity.Property(session => session.IpAddress).HasColumnName("ip_address").HasColumnType("inet");
        entity.Property(session => session.UserAgent).HasColumnName("user_agent");
        entity.Property(session => session.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        entity.HasIndex(session => session.TokenHash).IsUnique().HasDatabaseName("sessions_token_hash_uq");
        entity.HasIndex(session => session.UserId).HasDatabaseName("sessions_user_id_idx");
        entity.HasIndex(session => session.ExpiresAt).HasDatabaseName("sessions_expires_at_idx");
        entity.HasOne(session => session.User).WithMany(user => user.Sessions).HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("sessions_user_id_fkey");
    }
}
