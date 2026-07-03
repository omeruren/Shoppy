using Microsoft.EntityFrameworkCore;

namespace Shoppy.Business.Extensions;

public static class PaginationExtension
{
    // Silently clamped (matches this codebase's existing style of falling back to
    // sane defaults for invalid sort params rather than throwing — see SortingExtension).
    // Caps PageSize so a client can't force an unbounded Take()/materialization via
    // e.g. pageSize=1000000.
    private const int MaxPageSize = 100;

    public static async Task<PaginationResultDto<T>> WithPagination<T>(this IQueryable<T> values, PaginationRequestDto request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var totalCount = await values.CountAsync(cancellationToken);

        var list = await values
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pagRes = new PaginationResultDto<T>
        {
            Data = list,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPageCount = (int)Math.Ceiling((double)totalCount / pageSize)
        };
        return pagRes;
    }

    public static string ToCacheKey(this PaginationRequestDto request, string prefix)
        => $"{prefix}:p{request.PageNumber}:s{request.PageSize}:q{request.SearchTerm}:sort{request.SortBy ?? "-"}:{request.SortDirection ?? "-"}";
}

public sealed record PaginationRequestDto(int PageNumber, int PageSize, string SearchTerm, string? SortBy = null, string? SortDirection = null);

public sealed class PaginationResultDto<T>
{
    public List<T> Data { get; set; } = default!;
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPageCount { get; set; }

}