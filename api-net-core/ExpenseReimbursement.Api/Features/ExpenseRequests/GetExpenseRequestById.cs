using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using ExpenseReimbursement.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Features.ExpenseRequests;

public record GetExpenseRequestByIdQuery(int Id) : IRequest<AppResult<ExpenseRequestDto>>;

public class GetExpenseRequestByIdHandler(AppDbContext db) : IRequestHandler<GetExpenseRequestByIdQuery, AppResult<ExpenseRequestDto>>
{
    public async Task<AppResult<ExpenseRequestDto>> Handle(GetExpenseRequestByIdQuery query, CancellationToken cancellationToken)
    {
        var entity = await db.ExpenseRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);
        if (entity is null)
        {
            return AppResult<ExpenseRequestDto>.NotFound($"No existe una solicitud con id {query.Id}.");
        }

        return AppResult<ExpenseRequestDto>.Ok(ExpenseRequestDto.FromDomain(entity));
    }
}

public static class GetExpenseRequestByIdEndpoint
{
    public static void MapGetExpenseRequestById(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/expense-requests/{id:int}", async (int id, ISender sender) =>
        {
            var result = await sender.Send(new GetExpenseRequestByIdQuery(id));
            return result.ToOkResult();
        })
        .WithName("GetExpenseRequestById")
        .WithOpenApi();
    }
}
