using LithographERP.Application.Modules.Calculator;
using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Calculator;
using LithographERP.Domain.Modules.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LithographERP.Infrastructure.Persistence.Configurations;

public sealed class CalculatorTemplateConfiguration : IEntityTypeConfiguration<CalculatorTemplate>
{
    public void Configure(EntityTypeBuilder<CalculatorTemplate> entity)
    {
        entity.ToTable("templates", "calculator");
        entity.HasKey(template => template.Id);
        entity.Property(template => template.Id).HasColumnName("id");
        entity.Property(template => template.Name).HasColumnName("name").HasMaxLength(CalculatorLimits.TemplateNameMaxLength).IsRequired();
        entity.Property(template => template.Description).HasColumnName("description");
        entity.Property(template => template.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        entity.Property(template => template.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(template => template.CreatedBy).HasColumnName("created_by");
        entity.Property(template => template.UpdatedAt).HasColumnName("updated_at");
        entity.Property(template => template.UpdatedBy).HasColumnName("updated_by");
        entity.HasOne<User>().WithMany().HasForeignKey(template => template.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("templates_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(template => template.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("templates_updated_by_fkey");
    }
}

public sealed class CalculatorTemplateVersionConfiguration : IEntityTypeConfiguration<CalculatorTemplateVersion>
{
    public void Configure(EntityTypeBuilder<CalculatorTemplateVersion> entity)
    {
        entity.ToTable("template_versions", "calculator", table =>
        {
            table.HasCheckConstraint(
                "template_versions_status_ck",
                "status IN ('draft', 'published', 'retired')");
            table.HasCheckConstraint("template_versions_version_number_ck", "version_number >= 1");
        });
        entity.HasKey(version => version.Id);
        entity.Property(version => version.Id).HasColumnName("id");
        entity.Property(version => version.TemplateId).HasColumnName("template_id");
        entity.Property(version => version.VersionNumber).HasColumnName("version_number").IsRequired();
        entity.Property(version => version.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        var definition = entity.Property(version => version.Definition)
            .HasColumnName("definition")
            .HasColumnType("jsonb")
            .HasConversion(
                value => CalculatorDefinitionJson.Serialize(value),
                json => CalculatorDefinitionJson.Deserialize(json))
            .IsRequired();
        definition.Metadata.SetValueComparer(new ValueComparer<CalculatorTemplateDefinition>(
            (left, right) => CalculatorDefinitionJson.Serialize(left!) == CalculatorDefinitionJson.Serialize(right!),
            value => CalculatorDefinitionJson.Serialize(value).GetHashCode(StringComparison.Ordinal),
            value => CalculatorDefinitionJson.Deserialize(CalculatorDefinitionJson.Serialize(value))));
        entity.Property(version => version.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(version => version.CreatedBy).HasColumnName("created_by");
        entity.Property(version => version.PublishedAt).HasColumnName("published_at");
        entity.Property(version => version.PublishedBy).HasColumnName("published_by");
        entity.HasIndex(version => new { version.TemplateId, version.VersionNumber })
            .IsUnique()
            .HasDatabaseName("template_versions_template_id_version_number_uq");
        entity.HasIndex(version => version.TemplateId)
            .IsUnique()
            .HasFilter("status = 'draft'")
            .HasDatabaseName("template_versions_one_draft_uq");
        entity.HasOne<CalculatorTemplate>().WithMany().HasForeignKey(version => version.TemplateId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("template_versions_template_id_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(version => version.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("template_versions_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(version => version.PublishedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("template_versions_published_by_fkey");
    }
}

public sealed class OrderCalculatorConfiguration : IEntityTypeConfiguration<OrderCalculator>
{
    public void Configure(EntityTypeBuilder<OrderCalculator> entity)
    {
        entity.ToTable("order_calculators", "calculator");
        entity.HasKey(calculator => calculator.Id);
        entity.Property(calculator => calculator.Id).HasColumnName("id");
        entity.Property(calculator => calculator.OrderId).HasColumnName("order_id");
        entity.Property(calculator => calculator.TemplateVersionId).HasColumnName("template_version_id");
        entity.Property(calculator => calculator.FieldValues)
            .HasColumnName("field_values")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        entity.Property(calculator => calculator.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(calculator => calculator.CreatedBy).HasColumnName("created_by");
        entity.Property(calculator => calculator.UpdatedAt).HasColumnName("updated_at").IsRequired();
        entity.Property(calculator => calculator.UpdatedBy).HasColumnName("updated_by");
        entity.Property(calculator => calculator.LastCalculatedAt).HasColumnName("last_calculated_at");
        entity.HasIndex(calculator => calculator.OrderId)
            .IsUnique()
            .HasDatabaseName("order_calculators_order_id_uq");
        entity.HasIndex(calculator => calculator.TemplateVersionId)
            .HasDatabaseName("order_calculators_template_version_id_idx");
        entity.HasOne<Order>().WithMany().HasForeignKey(calculator => calculator.OrderId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("order_calculators_order_id_fkey");
        entity.HasOne<CalculatorTemplateVersion>().WithMany().HasForeignKey(calculator => calculator.TemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("order_calculators_template_version_id_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(calculator => calculator.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("order_calculators_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(calculator => calculator.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("order_calculators_updated_by_fkey");
    }
}
