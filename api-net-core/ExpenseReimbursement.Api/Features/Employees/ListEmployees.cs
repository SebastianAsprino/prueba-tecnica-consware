using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using ExpenseReimbursement.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Features.Employees;

public record ListEmployeesQuery : IRequest<AppResult<List<EmployeeDto>>>;

public class ListEmployeesHandler(AppDbContext db) : IRequestHandler<ListEmployeesQuery, AppResult<List<EmployeeDto>>>
{
    public async Task<AppResult<List<EmployeeDto>>> Handle(ListEmployeesQuery query, CancellationToken cancellationToken)
    {
        var employees = await db.Employees
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return AppResult<List<EmployeeDto>>.Ok(employees.Select(EmployeeDto.FromDomain).ToList());
    }
}

public static class ListEmployeesEndpoint
{
    public static void MapListEmployees(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/employees", async (ISender sender) =>
        {
            var result = await sender.Send(new ListEmployeesQuery());
            return result.ToOkResult();
        })
        .AddEndpointFilter<AdminAuthFilter>()
        .WithName("ListEmployees")
        .WithOpenApi();
    }
}
