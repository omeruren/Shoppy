using Microsoft.EntityFrameworkCore;
using Shoppy.Entity.Abstraction;
using Shoppy.Entity.Models;
using System.Reflection;

namespace Shoppy.DataAccess.Context;

public sealed class ApplicationDbContext : DbContext
{
    #region Models

    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Product> Products { get; set; }
    #endregion
    public ApplicationDbContext()
    {

    }

    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically finds and applies all configurations in the current project assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(x => x.CreatedAt).CurrentValue = DateTimeOffset.Now;
                entry.Property(x => x.UpdatedAt).IsModified = false;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.UpdatedAt).CurrentValue = DateTimeOffset.Now;
                entry.Property(x => x.CreatedAt).IsModified = false;
            }
            else if (entry.State == EntityState.Deleted)
            {
                if (!entry.Property(x => x.IsDeleted).CurrentValue)
                {
                    entry.Property(x => x.DeletedAt).CurrentValue = DateTimeOffset.Now;
                    entry.Property(x => x.IsDeleted).CurrentValue = true;
                    entry.State = EntityState.Modified;
                }
            }

        }
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
