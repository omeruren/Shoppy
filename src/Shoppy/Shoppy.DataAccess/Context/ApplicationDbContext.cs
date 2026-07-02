using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shoppy.Entity.Abstraction;
using Shoppy.Entity.Models;
using System.Reflection;
using System.Security.Claims;

namespace Shoppy.DataAccess.Context;

public sealed class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{

    private readonly IHttpContextAccessor? _httpContextAccessor;


    #region Models

    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Role> AppRoles { get; set; }
    public DbSet<UserRole> AppUserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    #endregion


    public ApplicationDbContext(
     DbContextOptions<ApplicationDbContext> options,
     IHttpContextAccessor httpContextAccessor)
     : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }



    //public ApplicationDbContext(IHttpContextAccessor? httpContext) => this._httpContextAccessor = httpContext;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<IdentityRole<Guid>>();
        modelBuilder.Ignore<IdentityRoleClaim<Guid>>();

        modelBuilder.Ignore<IdentityUserClaim<Guid>>();
        modelBuilder.Ignore<IdentityUserToken<Guid>>();
        modelBuilder.Ignore<IdentityUserLogin<Guid>>();

        // Automatically finds and applies all configurations in the current project assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        Guid? userId = null;

        if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId))
                userId = parsedId;

        }

        foreach (var entry in entries)
        {
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                    entry.Property(p => p.RowVersion).CurrentValue = Guid.NewGuid().ToByteArray();
            }
            if (entry.State == EntityState.Added)
            {
                entry.Property(x => x.CreatedAt).CurrentValue = DateTimeOffset.UtcNow;

                if (userId.HasValue)
                    entry.Property(x => x.CreatedBy).CurrentValue = userId.Value;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.UpdatedAt).CurrentValue = DateTimeOffset.UtcNow;

                if (userId.HasValue)
                    entry.Property(x => x.UpdatedBy).CurrentValue = userId.Value;
            }
            else if (entry.State == EntityState.Deleted)
            {
                if (!entry.Property(x => x.IsDeleted).CurrentValue)
                {
                    entry.Property(x => x.DeletedAt).CurrentValue = DateTimeOffset.UtcNow;

                    entry.Property(x => x.IsDeleted).CurrentValue = true;

                    entry.State = EntityState.Modified;

                    if (userId.HasValue)
                        entry.Property(x => x.DeletedBy).CurrentValue = userId.Value;
                }
            }

        }
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
