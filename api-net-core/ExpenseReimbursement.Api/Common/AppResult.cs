namespace ExpenseReimbursement.Api.Common;

public enum AppResultStatus
{
    Ok,
    NotFound,
    ValidationError,
    Conflict,
    Unauthorized
}

public class AppResult<T>
{
    public AppResultStatus Status { get; }
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }

    private AppResult(AppResultStatus status, T? value, IReadOnlyList<string> errors)
    {
        Status = status;
        Value = value;
        Errors = errors;
    }

    public static AppResult<T> Ok(T value) => new(AppResultStatus.Ok, value, Array.Empty<string>());

    public static AppResult<T> NotFound(string message) => new(AppResultStatus.NotFound, default, [message]);

    public static AppResult<T> ValidationError(params string[] errors) => new(AppResultStatus.ValidationError, default, errors);

    public static AppResult<T> Conflict(string message) => new(AppResultStatus.Conflict, default, [message]);

    public static AppResult<T> Unauthorized(string message) => new(AppResultStatus.Unauthorized, default, [message]);
}
