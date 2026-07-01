using Microsoft.EntityFrameworkCore;

namespace Shoppy.Business.Extensions;

public static class PaginationExtension
{
    public static async Task<PaginationResultDto<T>> WithPagination<T>(this IQueryable<T> values, PaginationRequestDto request, CancellationToken cancellationToken)
    {
        var totalCount = await values.CountAsync(cancellationToken);

        var list = await values
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var pagRes = new PaginationResultDto<T>
        {
            Data = list,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPageCount = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
        return pagRes;
    }
}

public sealed record PaginationRequestDto(int PageNumber, int PageSize, string SearchTerm);

public sealed class PaginationResultDto<T>
{
    public List<T> Data { get; set; } = default!;
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPageCount { get; set; }

}