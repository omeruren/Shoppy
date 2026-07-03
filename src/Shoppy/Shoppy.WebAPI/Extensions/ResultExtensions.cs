namespace Shoppy.Business.BaseResult;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, string? location = null)
    {
        if (!result.IsSuccessful)
            return Results.Json(result, statusCode: result.StatusCode);

        return result.StatusCode switch
        {
            201 => Results.Created(location ?? string.Empty, result),
            204 => Results.NoContent(),
            _ => Results.Json(result, statusCode: result.StatusCode)
        };
    }
}
