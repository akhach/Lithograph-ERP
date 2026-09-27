using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Calculator;
using LithographERP.Domain.Modules.Orders;
using LithographERP.Domain.Modules.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LithographERP.Infrastructure.Persistence.Configurations;

public sealed class OrderTypeConfiguration : IEntityTypeConfiguration<OrderType>
{
    public const int NameMaxLength = 150;

    public void Configure(EntityTypeBuilder<OrderType> entity)
    {
        entity.ToTable("order_types", "orders");
        entity.HasKey(type => type.Id);
        entity.Property(type => type.Id).HasColumnName("id");
        entity.Property(type => type.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        entity.Property(type => type.Description).HasColumnName("description");
        entity.Property(type => type.CalculatorTemplateId).HasColumnName("calculator_template_id");
        entity.Property(type => type.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        entity.HasIndex(type => type.CalculatorTemplateId).HasDatabaseName("order_types_calculator_template_id_idx");
        entity.HasOne<CalculatorTemplate>().WithMany().HasForeignKey(type => type.CalculatorTemplateId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("order_types_calculator_template_id_fkey");
        entity.Property(type => type.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(type => type.CreatedBy).HasColumnName("created_by");
        entity.Property(type => type.UpdatedAt).HasColumnName("updated_at");
        entity.Property(type => type.UpdatedBy).HasColumnName("updated_by");
        entity.HasOne<User>().WithMany().HasForeignKey(type => type.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("order_types_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(type => type.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("order_types_updated_by_fkey");
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public const int NameMaxLength = 250;
    public const int BusinessIdMaxLength = 30;

    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("orders", "orders", table =>
        {
            table.HasCheckConstraint(
                "orders_status_ck",
                "status IN ('draft', 'active', 'on_hold', 'completed', 'cancelled')");
            table.HasCheckConstraint(
                "orders_priority_ck",
                "priority IN ('low', 'normal', 'high', 'urgent')");
            table.HasCheckConstraint("orders_selling_price_ck", "selling_price >= 0");
            table.HasCheckConstraint("orders_cost_price_ck", "cost_price >= 0");
        });
        entity.HasKey(order => order.Id);
        entity.Property(order => order.Id).HasColumnName("id");
        entity.Property(order => order.BusinessId).HasColumnName("business_id").HasMaxLength(BusinessIdMaxLength).IsRequired();
        entity.Property(order => order.ProjectId).HasColumnName("project_id");
        entity.Property(order => order.OrderTypeId).HasColumnName("order_type_id");
        entity.Property(order => order.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        entity.Property(order => order.Description).HasColumnName("description");
        entity.Property(order => order.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue(OrderStatuses.Draft).IsRequired();
        entity.Property(order => order.Priority).HasColumnName("priority").HasMaxLength(20).HasDefaultValue(OrderPriorities.Normal).IsRequired();
        entity.Property(order => order.SellingPrice).HasColumnName("selling_price").HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entity.Property(order => order.CostPrice).HasColumnName("cost_price").HasPrecision(18, 2).HasDefaultValue(0m).IsRequired();
        entity.Property(order => order.Deadline).HasColumnName("deadline");
        entity.Property(order => order.PreviewImagePath).HasColumnName("preview_image_path");
        entity.Property(order => order.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(order => order.CreatedBy).HasColumnName("created_by");
        entity.Property(order => order.UpdatedAt).HasColumnName("updated_at");
        entity.Property(order => order.UpdatedBy).HasColumnName("updated_by");
        entity.HasIndex(order => order.BusinessId).IsUnique().HasDatabaseName("orders_business_id_uq");
        entity.HasIndex(order => order.ProjectId).HasDatabaseName("orders_project_id_idx");
        entity.HasIndex(order => order.OrderTypeId).HasDatabaseName("orders_order_type_id_idx");
        entity.HasIndex(order => order.Status).HasDatabaseName("orders_status_idx");
        entity.HasIndex(order => order.Deadline).HasDatabaseName("orders_deadline_idx");
        entity.HasOne(order => order.Project).WithMany().HasForeignKey(order => order.ProjectId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("orders_project_id_fkey");
        entity.HasOne(order => order.OrderType).WithMany().HasForeignKey(order => order.OrderTypeId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("orders_order_type_id_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(order => order.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("orders_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(order => order.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("orders_updated_by_fkey");
    }
}

public sealed class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> entity)
    {
        entity.ToTable("checklist_items", "orders");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.OrderId).HasColumnName("order_id");
        entity.Property(item => item.Text).HasColumnName("text").IsRequired();
        entity.Property(item => item.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false).IsRequired();
        entity.Property(item => item.SortOrder).HasColumnName("sort_order").IsRequired();
        entity.HasIndex(item => new { item.OrderId, item.SortOrder }).HasDatabaseName("checklist_items_order_id_sort_order_idx");
        entity.HasOne(item => item.Order).WithMany(order => order.ChecklistItems).HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("checklist_items_order_id_fkey");
    }
}

public sealed class FolderLinkConfiguration : IEntityTypeConfiguration<FolderLink>
{
    public const int NameMaxLength = 150;

    public void Configure(EntityTypeBuilder<FolderLink> entity)
    {
        entity.ToTable("folder_links", "orders");
        entity.HasKey(link => link.Id);
        entity.Property(link => link.Id).HasColumnName("id");
        entity.Property(link => link.OrderId).HasColumnName("order_id");
        entity.Property(link => link.Name).HasColumnName("name").HasMaxLength(NameMaxLength);
        entity.Property(link => link.Path).HasColumnName("path").IsRequired();
        entity.Property(link => link.SortOrder).HasColumnName("sort_order").IsRequired();
        entity.HasIndex(link => new { link.OrderId, link.SortOrder }).HasDatabaseName("folder_links_order_id_sort_order_idx");
        entity.HasOne(link => link.Order).WithMany(order => order.FolderLinks).HasForeignKey(link => link.OrderId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("folder_links_order_id_fkey");
    }
}
