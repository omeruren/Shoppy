using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Roles.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.Roles;

public sealed class RoleService(ApplicationDbContext _context, IMemoryCache _cache) : IRoleService
{
    private const string CacheKeyPrefix = "roles";
    private readonly DbSet<Role> _roles = _context.Set<Role>();


    // GET ALL ROLES
    public async Task<Result<List<Role>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var roles = _cache.Get<List<Role>>(CacheKeyPrefix);

        if (roles is null)
        {
            roles = await _roles
               .AsNoTracking()
               .OrderBy(r => r.Name)
               .Select(r => new Role
               {
                   Id = r.Id,
                   Name = r.Name,

                   CreatedAt = r.CreatedAt,
                   CreatedBy = r.CreatedBy,

                   UpdatedAt = r.UpdatedAt,
                   UpdatedBy = r.UpdatedBy,

                   IsDeleted = r.IsDeleted,
                   DeletedBy = r.DeletedBy,
                   DeletedAt = r.DeletedAt,
               })
               .ToListAsync(cancellationToken);

            _cache.Set(CacheKeyPrefix, roles, TimeSpan.FromMinutes(5));

        }

        return roles;
    }


    // GET ROLE BY ID
    public async Task<Result<Role>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Role? role = await _roles.FindAsync(id, cancellationToken);

        if (role is null)
            return Result<Role>.Failure(404, "Role not found.");

        return role;
    }


    // CREATE ROLE
    public async Task<Result<string>> CreateAsync(RoleCreateDto request, CancellationToken cancellationToken)
    {
        bool isExists = await _roles.AnyAsync(r => r.Name == request.Name, cancellationToken);

        if (isExists)
            return Result<string>.Failure(409, "Role is already exists.");

        Role role = request.Adapt<Role>();

        await _roles.AddAsync(role, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _cache.Remove(CacheKeyPrefix);

        return "Role created.";
    }


    // UPDATE ROLE
    public async Task<Result<string>> UpdateAsync(RoleUpdateDto request, CancellationToken cancellationToken)
    {
        Role? role = await _roles.FindAsync(request.Id, cancellationToken);

        if (role is null)
            return Result<string>.Failure(404, "Role not found.");

        if (role.Name != request.Name)
        {
            bool isNameExists = await _roles.AnyAsync(r => r.Name == request.Name, cancellationToken);

            if (isNameExists)
                return Result<string>.Failure(409, "Role name is already exists.");

            request.Adapt(role);

            if (request.RowVersion is not null)
                _context.Entry(role).Property(x => x.RowVersion).OriginalValue = request.RowVersion;


            _roles.Update(role);

            await _context.SaveChangesAsync(cancellationToken);

        }
        _cache.Remove(CacheKeyPrefix);

        return "Role updated.";
    }


    // DELETE ROLE
    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Role? role = await _roles.FindAsync([id], cancellationToken);

        if (role is null)
            return Result<string>.Failure(404, "Role not found.");

        _roles.Remove(role);

        await _context.SaveChangesAsync(cancellationToken);

        _cache.Remove(CacheKeyPrefix);

        return "Role deleted.";
    }

}