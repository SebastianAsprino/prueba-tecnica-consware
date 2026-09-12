using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using ExpenseReimbursement.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Features.ExpenseRequests;

public record ApproveExpenseRequestCommand(int Id) : IRequest<AppResult<ExpenseRequestDto>>;

public class ApproveExpenseRequestHandler(AppDbContext db) : IRequestHandler<ApproveExpenseRequestCommand, AppResult<ExpenseRequestDto>>
{
    public async Task<AppResult<ExpenseRequestDto>> Handle(ApproveExpenseRequestCommand command, CancellationToken cancellationToken)
    {
        var entity = await db.ExpenseRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return AppResult<ExpenseRequestDto>.NotFound($"No existe una solicitud con id {command.Id}.");
        }

        if (!entity.TryApprove(out var error))
        {
            return AppResult<ExpenseRequestDto>.Conflict(error!);
        }

        await db.SaveChangesAsync(cancellationToken);

        return AppResult<ExpenseRequestDto>.Ok(ExpenseRequestDto.FromDomain(entity));
    }
}

public static class ApproveExpenseRequestEndpoint
{
    public static void MapApproveExpenseRequest(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/expense-requests/{id:int}/approve", async (int id, ISender sender) =>
        {
            var result = await sender.Send(new ApproveExpenseRequestCommand(id));
            return result.ToOkResult();
        })
        .AddEndpointFilter<AdminAuthFilter>()
        .WithName("ApproveExpenseRequest")
        .WithOpenApi();
    }
}
