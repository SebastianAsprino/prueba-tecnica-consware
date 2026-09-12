namespace ExpenseReimbursement.Api.Common;

public static class AppResultExtensions
{
    public static IResult ToOkResult<T>(this AppResult<T> result) => result.Status switch
    {
        AppResultStatus.Ok => Results.Ok(result.Value),
        AppResultStatus.NotFound => Results.NotFound(new { errors = result.Errors }),
        AppResultStatus.ValidationError => Results.BadRequest(new { errors = result.Errors }),
        AppResultStatus.Conflict => Results.Conflict(new { errors = result.Errors }),
        AppResultStatus.Unauthorized => Results.Json(new { errors = result.Errors }, statusCode: StatusCodes.Status401Unauthorized),
        _ => Results.Problem()
    };

    public static IResult ToCreatedResult<T>(this AppResult<T> result, Func<T, string> locationFactory) => result.Status switch
    {
        AppResultStatus.Ok => Results.Created(locationFactory(result.Value!), result.Value),
        AppResultStatus.NotFound => Results.NotFound(new { errors = result.Errors }),
        AppResultStatus.ValidationError => Results.BadRequest(new { errors = result.Errors }),
        AppResultStatus.Conflict => Results.Conflict(new { errors = result.Errors }),
        AppResultStatus.Unauthorized => Results.Json(new { errors = result.Errors }, statusCode: StatusCodes.Status401Unauthorized),
        _ => Results.Problem()
    };
}
