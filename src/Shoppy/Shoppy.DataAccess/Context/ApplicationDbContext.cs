using Microsoft.EntityFrameworkCore;
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
}
