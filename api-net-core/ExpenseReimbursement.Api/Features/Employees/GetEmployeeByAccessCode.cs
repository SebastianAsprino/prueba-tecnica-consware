using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using ExpenseReimbursement.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Features.Employees;

public record GetEmployeeByAccessCodeQuery(string AccessCode) : IRequest<AppResult<EmployeeDto>>;

public class GetEmployeeByAccessCodeHandler(AppDbContext db) : IRequestHandler<GetEmployeeByAccessCodeQuery, AppResult<EmployeeDto>>
{
    public async Task<AppResult<EmployeeDto>> Handle(GetEmployeeByAccessCodeQuery query, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.AccessCode == query.AccessCode, cancellationToken);
        if (employee is null)
        {
            return AppResult<EmployeeDto>.NotFound("Código de acceso inválido.");
        }

        return AppResult<EmployeeDto>.Ok(EmployeeDto.FromDomain(employee));
    }
}

public static class GetEmployeeByAccessCodeEndpoint
{
    public static void MapGetEmployeeByAccessCode(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/employees/by-code/{code}", async (string code, ISender sender) =>
        {
            var result = await sender.Send(new GetEmployeeByAccessCodeQuery(code));
            return result.ToOkResult();
        })
        .WithName("GetEmployeeByAccessCode")
        .WithOpenApi();
    }
}
