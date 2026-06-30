using Mapster;
using Microsoft.EntityFrameworkCore;
using Shoppy.Business.BaseResult;
using Shoppy.Business.UserRoles.DataTransferObjects;
using Shoppy.DataAccess.Context;
using Shoppy.Entity.Models;

namespace Shoppy.Business.UserRoles;

public sealed class UserRoleService(ApplicationDbContext _context) : IUserRoleService
{

    private readonly DbSet<UserRole> _userRoles = _context.Set<UserRole>();


    public async Task<Result<List<UserRole>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var list = await _userRoles
            .AsNoTracking()
            .Select(r => new UserRole
            {
                RoleId = r.RoleId,
                UserId = r.UserId,

            })
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<Result<string>> CreateAsync(UserRoleCreateDto request, CancellationToken cancellationToken)
    {
        UserRole userRole = request.Adapt<UserRole>();

        _userRoles.Add(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        return "User role saved successfully.";
    }

    public async Task<Result<string>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        UserRole? userRole = await _userRoles.FindAsync([id], cancellationToken);

        if (userRole is null)
            return Result<string>.Failure(404, "User role not found.");

        _userRoles.Remove(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        return "User role deleted successfully.";
    }


}