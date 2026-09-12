using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using ExpenseReimbursement.Api.Domain;
using ExpenseReimbursement.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Features.ExpenseRequests;

public record ListExpenseRequestsQuery(string? Status, int? EmployeeId) : IRequest<AppResult<List<ExpenseRequestDto>>>;

public class ListExpenseRequestsHandler(AppDbContext db) : IRequestHandler<ListExpenseRequestsQuery, AppResult<List<ExpenseRequestDto>>>
{
    public async Task<AppResult<List<ExpenseRequestDto>>> Handle(ListExpenseRequestsQuery query, CancellationToken cancellationToken)
    {
        ExpenseStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<ExpenseStatus>(query.Status, ignoreCase: true, out var parsed))
            {
                return AppResult<List<ExpenseRequestDto>>.ValidationError(
                    $"Estado inválido '{query.Status}'. Valores permitidos: Pending, Approved, Rejected.");
            }
            statusFilter = parsed;
        }

        var entities = await db.ExpenseRequests
            .Include(r => r.Employee)
            .Where(r => statusFilter == null || r.Status == statusFilter)
            .Where(r => query.EmployeeId == null || r.EmployeeId == query.EmployeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = entities.Select(ExpenseRequestDto.FromDomain).ToList();

        return AppResult<List<ExpenseRequestDto>>.Ok(items);
    }
}

public static class ListExpenseRequestsEndpoint
{
    public static void MapListExpenseRequests(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/expense-requests", async (string? status, int? employeeId, ISender sender) =>
        {
            var result = await sender.Send(new ListExpenseRequestsQuery(status, employeeId));
            return result.ToOkResult();
        })
        .WithName("ListExpenseRequests")
        .WithOpenApi();
    }
}
