using System.Threading.RateLimiting;

namespace ExpenseReimbursement.Api.Common;

public static class RateLimitingPolicies
{
    /// <summary>
    /// Política para endpoints que validan credenciales (login del admin y código de
    /// acceso del empleado): limita intentos por IP para frenar fuerza bruta.
    /// </summary>
    public const string Auth = "auth";

    private const int AuthPermitLimit = 20;
    private static readonly TimeSpan AuthWindow = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Auth, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = AuthPermitLimit,
                        Window = AuthWindow,
                        QueueLimit = 0
                    }));
        });
}
