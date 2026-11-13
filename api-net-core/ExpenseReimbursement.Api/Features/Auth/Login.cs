using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using MediatR;
using Microsoft.Extensions.Options;

namespace ExpenseReimbursement.Api.Features.Auth;

public record LoginCommand(string Username, string Password) : IRequest<AppResult<LoginResponseDto>>;

public class LoginHandler(IOptions<AdminOptions> adminOptions, AdminTokenStore tokenStore)
    : IRequestHandler<LoginCommand, AppResult<LoginResponseDto>>
{
    public Task<AppResult<LoginResponseDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var admin = adminOptions.Value;

        var hasCredentials = !string.IsNullOrEmpty(command.Username) && !string.IsNullOrEmpty(command.Password);

        // Se evalúan ambas comparaciones siempre (operador & sin cortocircuito) para que
        // el tiempo de respuesta no revele si el usuario era correcto.
        var isValid = hasCredentials
            & SecureCompare.FixedTimeEquals(command.Username, admin.Username)
            & SecureCompare.FixedTimeEquals(command.Password, admin.Password);

        if (!isValid)
        {
            return Task.FromResult(AppResult<LoginResponseDto>.Unauthorized("Usuario o contraseña incorrectos."));
        }

        var token = tokenStore.Issue();
        return Task.FromResult(AppResult<LoginResponseDto>.Ok(new LoginResponseDto(token)));
    }
}

public static class LoginEndpoint
{
    public static void MapLogin(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (LoginRequestDto dto, ISender sender) =>
        {
            var result = await sender.Send(new LoginCommand(dto.Username, dto.Password));
            return result.ToOkResult();
        })
        .RequireRateLimiting(RateLimitingPolicies.Auth)
        .WithName("Login")
        .WithOpenApi();
    }
}
