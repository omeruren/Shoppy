using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shoppy.Business.BaseResult;
using Shoppy.Business.Caching;
using Shoppy.Business.Extensions;
using Shoppy.Business.Roles.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;
using PermissionCatalog = Shoppy.Business.Permissions.Permissions;

namespace Shoppy.Business.Roles;

public sealed class RoleService(ApplicationDbContext _context, ICacheService _cacheService, ILogger<RoleService> _logger) : IRoleService
{
    private const string CacheKeyPrefix = "roles";
    private readonly DbSet<Role> _roles = _context.Set<Role>();


    // GET ALL ROLES
    public async Task<Result<PaginationResultDto<RoleResultDto>>> GetAllAsync(PaginationRequestDto request, CancellationToken cancellationToken)
    {
        return await _cacheService.GetOrCreateAsync(CacheKeyPrefix, request.ToCacheKey(CacheKeyPrefix), async () =>
        {
            return await _roles
               .AsNoTracking()
               .Where(r => string.IsNullOrWhiteSpace(request.SearchTerm) || r.Name.Contains(request.SearchTerm))
               .Select(r => new RoleResultDto
               {
                   Id = r.Id,
                   Name = r.Name,
                   RowVersion = r.RowVersion,

                   CreatedAt = r.CreatedAt,
                   CreatedBy = r.CreatedBy,

                   UpdatedAt = r.UpdatedAt,
                   UpdatedBy = r.UpdatedBy,

                   IsDeleted = r.IsDeleted,
                   DeletedBy = r.DeletedBy,
                   DeletedAt = r.DeletedAt,
               })
               .ApplyRoleSort(request.SortBy, request.SortDirection)
               .WithPagination(request, cancellationToken);
        }, TimeSpan.FromMinutes(5));
    }


    // GET ROLE BY ID
    public async Task<Result<RoleResultDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Role? role = await _roles.FindAsync(id, cancellationToken);

        if (role is null)
            return Result<RoleResultDto>.Failure(404, ErrorMessages.Role.NotFound);

        return role.Adapt<RoleResultDto>();
    }


    // CREATE ROLE
    public async Task<Result<string>> CreateAsync(RoleCreateDto request, CancellationToken cancellationToken)
    {
        bool isExists = await _roles.AnyAsync(r => r.Name == request.Name, cancellationToken);

        if (isExists)
        {
            _logger.LogWarning("Role creation attempted with already-existing name {RoleName}", request.Name);
            return Result<string>.Failure(409, ErrorMessages.Role.AlreadyExists);
        }

        Role role = request.Adapt<Role>();

        await _roles.AddAsync(role, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("Role {RoleId} ({RoleName}) created", role.Id, role.Name);

        return Result<string>.Success("Role created.", 201);
    }


    // UPDATE ROLE
    public async Task<Result<string>> UpdateAsync(RoleUpdateDto request, CancellationToken cancellationToken)
    {
        Role? role = await _roles.FindAsync(request.Id, cancellationToken);

        if (role is null)
            return Result<string>.Failure(404, ErrorMessages.Role.NotFound);

        if (role.Name != request.Name)
        {
            bool isNameExists = await _roles.AnyAsync(r => r.Name == request.Name, cancellationToken);

            if (isNameExists)
            {
                _logger.LogWarning("Role update attempted with already-existing name {RoleName}", request.Name);
                return Result<string>.Failure(409, ErrorMessages.Role.NameAlreadyExists);
            }

            request.Adapt(role);

            if (request.RowVersion is not null)
                _context.Entry(role).Property(x => x.RowVersion).OriginalValue = request.RowVersion;


            _roles.Update(role);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role {RoleId} updated", role.Id);
        }
        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        return "Role updated.";
    }


    // DELETE ROLE
    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Role? role = await _roles.FindAsync([id], cancellationToken);

        if (role is null)
            return Result<string>.Failure(404, ErrorMessages.Role.NotFound);

        _roles.Remove(role);

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("Role {RoleId} deleted", role.Id);

        return "Role deleted.";
    }


    // GET ROLE PERMISSIONS
    public async Task<Result<List<string>>> GetPermissionsAsync(Guid roleId, CancellationToken cancellationToken)
    {
        bool roleExists = await _roles.AnyAsync(r => r.Id == roleId, cancellationToken);

        if (!roleExists)
            return Result<List<string>>.Failure(404, ErrorMessages.Role.NotFound);

        var permissions = await _context.Set<RolePermission>()
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionName)
            .ToListAsync(cancellationToken);

        return permissions;
    }


    // UPDATE ROLE PERMISSIONS
    public async Task<Result<string>> UpdatePermissionsAsync(Guid roleId, List<string> permissions, CancellationToken cancellationToken)
    {
        if (permissions.Except(PermissionCatalog.GetAll()).Any())
            return Result<string>.Failure(400, ErrorMessages.Role.UnknownPermission);

        Role? role = await _roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
            return Result<string>.Failure(404, ErrorMessages.Role.NotFound);

        var desired = permissions.ToHashSet(StringComparer.Ordinal);
        var current = role.RolePermissions.Select(rp => rp.PermissionName).ToHashSet(StringComparer.Ordinal);

        var toRemove = role.RolePermissions.Where(rp => !desired.Contains(rp.PermissionName)).ToList();
        foreach (var rp in toRemove)
            role.RolePermissions.Remove(rp);

        foreach (var permission in desired.Where(p => !current.Contains(p)))
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionName = permission });

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidatePrefixAsync(CacheKeyPrefix);

        _logger.LogInformation("Role {RoleId} permissions updated ({Count} permissions)", role.Id, desired.Count);

        return "Role permissions updated.";
    }

}
