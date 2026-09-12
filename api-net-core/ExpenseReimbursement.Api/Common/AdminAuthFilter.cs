namespace ExpenseReimbursement.Api.Common;

public class AdminAuthFilter(AdminTokenStore tokenStore) : IEndpointFilter
{
    public const string TokenHeader = "X-Admin-Token";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var token = context.HttpContext.Request.Headers[TokenHeader].FirstOrDefault();

        if (!tokenStore.IsValid(token))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
