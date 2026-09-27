using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LithographERP.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> entity)
    {
        entity.ToTable("employees", "employees");
        entity.HasKey(employee => employee.Id);
        entity.Property(employee => employee.Id).HasColumnName("id");
        entity.Property(employee => employee.UserId).HasColumnName("user_id");
        entity.Property(employee => employee.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        entity.Property(employee => employee.Position).HasColumnName("position").HasMaxLength(150);
        entity.Property(employee => employee.Phone).HasColumnName("phone").HasMaxLength(50);
        entity.Property(employee => employee.Email).HasColumnName("email").HasMaxLength(200);
        entity.Property(employee => employee.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        entity.Property(employee => employee.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(employee => employee.CreatedBy).HasColumnName("created_by");
        entity.Property(employee => employee.UpdatedAt).HasColumnName("updated_at");
        entity.Property(employee => employee.UpdatedBy).HasColumnName("updated_by");
        entity.HasIndex(employee => employee.UserId).IsUnique().HasDatabaseName("employees_user_id_uq");
        entity.HasOne(employee => employee.User)
            .WithOne()
            .HasForeignKey<Employee>(employee => employee.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("employees_user_id_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(employee => employee.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("employees_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(employee => employee.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("employees_updated_by_fkey");
    }
}
