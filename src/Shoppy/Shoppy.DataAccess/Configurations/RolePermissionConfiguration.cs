using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shoppy.Entity.Models;

namespace Shoppy.DataAccess.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        // Composite PK: one role can have many permissions, each permission only once per role
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionName });

        builder.Property(rp => rp.PermissionName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mirror Role's soft-delete filter so EF doesn't warn about
        // mismatched query filters on the required end of the relationship
        builder.HasQueryFilter(rp => !rp.Role.IsDeleted);
    }
}
