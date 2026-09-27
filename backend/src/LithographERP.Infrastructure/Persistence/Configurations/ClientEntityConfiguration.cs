using LithographERP.Domain.Modules.Authentication;
using LithographERP.Domain.Modules.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LithographERP.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> entity)
    {
        entity.ToTable("clients", "clients");
        entity.HasKey(client => client.Id);
        entity.Property(client => client.Id).HasColumnName("id");
        entity.Property(client => client.BusinessId).HasColumnName("business_id").HasMaxLength(20).IsRequired();
        entity.Property(client => client.Name).HasColumnName("name").HasMaxLength(250).IsRequired();
        entity.Property(client => client.ContactPerson).HasColumnName("contact_person").HasMaxLength(200);
        entity.Property(client => client.Phone).HasColumnName("phone").HasMaxLength(50);
        entity.Property(client => client.Email).HasColumnName("email").HasMaxLength(200);
        entity.Property(client => client.Address).HasColumnName("address");
        entity.Property(client => client.Notes).HasColumnName("notes");
        entity.Property(client => client.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        entity.Property(client => client.CreatedAt).HasColumnName("created_at").IsRequired();
        entity.Property(client => client.CreatedBy).HasColumnName("created_by");
        entity.Property(client => client.UpdatedAt).HasColumnName("updated_at");
        entity.Property(client => client.UpdatedBy).HasColumnName("updated_by");
        entity.HasIndex(client => client.BusinessId).IsUnique().HasDatabaseName("clients_business_id_uq");
        entity.HasOne<User>().WithMany().HasForeignKey(client => client.CreatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("clients_created_by_fkey");
        entity.HasOne<User>().WithMany().HasForeignKey(client => client.UpdatedBy).OnDelete(DeleteBehavior.Restrict).HasConstraintName("clients_updated_by_fkey");
    }
}
