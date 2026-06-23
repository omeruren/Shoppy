using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shoppy.Entity.Models;

namespace Shoppy.DataAccess.Configurations;

internal class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderDate)
            .IsRequired();


        builder.HasMany(o => o.Items)
            .WithOne(o => o.Order)
            .HasForeignKey(o => o.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);

    }
}

internal sealed class OrdertemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.UnitPrice).HasColumnType("decimal(18,2)");

        builder.HasOne(o => o.Product)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(o => o.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // constraint for checking quantity is more than 0
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
        });

        builder.HasQueryFilter(x => !x.IsDeleted);

    }
}