using Shoppy.Business.Categories.DataTransferObjects;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.Business.Orders.DataTransferObjects;
using Shoppy.Business.Products.DataTransferObjects;
using Shoppy.Business.Roles.DataTransferObjects;

namespace Shoppy.Business.Extensions;

public static class SortingExtension
{
    public static IQueryable<ProductResultDto> ApplyProductSort(this IQueryable<ProductResultDto> query, string? sortBy, string? sortDirection)
    {
        bool desc = IsDescending(sortDirection);

        return sortBy?.ToLowerInvariant() switch
        {
            "createdat" => desc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "updatedat" => desc ? query.OrderByDescending(p => p.UpdatedAt) : query.OrderBy(p => p.UpdatedAt),
            "name" => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => query.OrderBy(p => p.Name),
        };
    }

    public static IQueryable<CategoryResultDto> ApplyCategorySort(this IQueryable<CategoryResultDto> query, string? sortBy, string? sortDirection)
    {
        bool desc = IsDescending(sortDirection);

        return sortBy?.ToLowerInvariant() switch
        {
            "createdat" => desc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            "updatedat" => desc ? query.OrderByDescending(c => c.UpdatedAt) : query.OrderBy(c => c.UpdatedAt),
            "name" => desc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            _ => query.OrderBy(c => c.Name),
        };
    }

    public static IQueryable<OrderResultDto> ApplyOrderSort(this IQueryable<OrderResultDto> query, string? sortBy, string? sortDirection)
    {
        bool desc = IsDescending(sortDirection);

        return sortBy?.ToLowerInvariant() switch
        {
            "createdat" => desc ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt),
            "updatedat" => desc ? query.OrderByDescending(o => o.UpdatedAt) : query.OrderBy(o => o.UpdatedAt),
            "orderdate" => desc ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate),
            _ => query.OrderBy(o => o.OrderDate),
        };
    }

    public static IQueryable<OrderItemResultDto> ApplyOrderItemSort(this IQueryable<OrderItemResultDto> query, string? sortBy, string? sortDirection)
    {
        bool desc = IsDescending(sortDirection);

        return sortBy?.ToLowerInvariant() switch
        {
            "createdat" => desc ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt),
            "updatedat" => desc ? query.OrderByDescending(i => i.UpdatedAt) : query.OrderBy(i => i.UpdatedAt),
            "quantity" => desc ? query.OrderByDescending(i => i.Quantity) : query.OrderBy(i => i.Quantity),
            _ => query.OrderBy(i => i.CreatedAt),
        };
    }

    public static IQueryable<RoleResultDto> ApplyRoleSort(this IQueryable<RoleResultDto> query, string? sortBy, string? sortDirection)
    {
        bool desc = IsDescending(sortDirection);

        return sortBy?.ToLowerInvariant() switch
        {
            "createdat" => desc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
            "updatedat" => desc ? query.OrderByDescending(r => r.UpdatedAt) : query.OrderBy(r => r.UpdatedAt),
            "name" => desc ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
            _ => query.OrderBy(r => r.Name),
        };
    }

    private static bool IsDescending(string? sortDirection)
        => string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
}
