using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        Guid? userId = ResolveCurrentUserId();

        var rootEntries = ChangeTracker.Entries<BaseEntity>().ToList();
        var visited = new HashSet<object>();

        foreach (var entry in rootEntries)
            ProcessEntry(entry, userId, visited);

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private Guid? ResolveCurrentUserId()
    {
        if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value;

        return !string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : null;
    }

    private void ProcessEntry(EntityEntry<BaseEntity> entry, Guid? userId, HashSet<object> visited)
    {
        if (!visited.Add(entry.Entity))
            return; // cycle guard

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

                CascadeSoftDeleteToLoadedChildren(entry, userId, visited);
            }
        }
    }

    // Soft-deleting an entity via ChangeTracker doesn't hit the DB's ON DELETE CASCADE
    // (the row is UPDATEd, not DELETEd), so loaded child collections must be cascaded manually here.
    private void CascadeSoftDeleteToLoadedChildren(EntityEntry<BaseEntity> parentEntry, Guid? userId, HashSet<object> visited)
    {
        foreach (var navigation in parentEntry.Metadata.GetNavigations().Where(n => n.IsCollection))
        {
            var collectionEntry = parentEntry.Collection(navigation.Name);

            if (!collectionEntry.IsLoaded || collectionEntry.CurrentValue is null)
                continue; // only cascade into navigations the caller actually loaded via .Include()

            foreach (var child in collectionEntry.CurrentValue.Cast<object>().ToList())
            {
                if (child is not BaseEntity childEntity)
                    continue;

                var childEntry = Entry(childEntity);

                if (childEntry.State == EntityState.Added)
                {
                    // Never persisted — nothing to soft-delete in the DB; just stop tracking it.
                    childEntry.State = EntityState.Detached;
                    continue;
                }

                if (childEntry.State != EntityState.Deleted)
                    childEntry.State = EntityState.Deleted;

                ProcessEntry(childEntry, userId, visited);
            }
        }
    }
}
