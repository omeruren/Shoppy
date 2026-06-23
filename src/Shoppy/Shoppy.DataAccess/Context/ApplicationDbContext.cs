using Microsoft.EntityFrameworkCore;
using Shoppy.Entity.Models;

namespace Shoppy.DataAccess.Context;

public sealed class ApplicationDbContext : DbContext
{
    #region Models

    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    #endregion
    public ApplicationDbContext()
    {

    }
}
