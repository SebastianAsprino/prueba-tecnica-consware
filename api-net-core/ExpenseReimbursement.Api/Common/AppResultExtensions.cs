namespace ExpenseReimbursement.Api.Common;

public static class AppResultExtensions
{
    public static IResult ToOkResult<T>(this AppResult<T> result) =>
        result.Status == AppResultStatus.Ok
            ? Results.Ok(result.Value)
            : result.ToErrorResult();

    public static IResult ToCreatedResult<T>(this AppResult<T> result, Func<T, string> locationFactory) =>
        result.Status == AppResultStatus.Ok
            ? Results.Created(locationFactory(result.Value!), result.Value)
            : result.ToErrorResult();

    private static IResult ToErrorResult<T>(this AppResult<T> result)
    {
        var body = new { errors = result.Errors };
        return result.Status switch
        {
            AppResultStatus.NotFound => Results.NotFound(body),
            AppResultStatus.ValidationError => Results.BadRequest(body),
            AppResultStatus.Conflict => Results.Conflict(body),
            AppResultStatus.Unauthorized => Results.Json(body, statusCode: StatusCodes.Status401Unauthorized),
            _ => Results.Problem()
        };
    }
}
