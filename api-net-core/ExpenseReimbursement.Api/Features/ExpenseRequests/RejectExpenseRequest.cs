using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using ExpenseReimbursement.Api.Domain;
using ExpenseReimbursement.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Features.ExpenseRequests;

public record RejectExpenseRequestCommand(int Id, string Reason) : IRequest<AppResult<ExpenseRequestDto>>;

public class RejectExpenseRequestHandler(AppDbContext db) : IRequestHandler<RejectExpenseRequestCommand, AppResult<ExpenseRequestDto>>
{
    public async Task<AppResult<ExpenseRequestDto>> Handle(RejectExpenseRequestCommand command, CancellationToken cancellationToken)
    {
        var entity = await db.ExpenseRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (entity is null)
        {
            return AppResult<ExpenseRequestDto>.NotFound($"No existe una solicitud con id {command.Id}.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return AppResult<ExpenseRequestDto>.ValidationError("El motivo de rechazo es obligatorio.");
        }

        if (command.Reason.Length > ExpenseRequest.RejectionReasonMaxLength)
        {
            return AppResult<ExpenseRequestDto>.ValidationError(
                $"El motivo de rechazo no puede superar {ExpenseRequest.RejectionReasonMaxLength} caracteres.");
        }

        if (!entity.TryReject(command.Reason, out var error))
        {
            return AppResult<ExpenseRequestDto>.Conflict(error!);
        }

        await db.SaveChangesAsync(cancellationToken);

        return AppResult<ExpenseRequestDto>.Ok(ExpenseRequestDto.FromDomain(entity));
    }
}

public static class RejectExpenseRequestEndpoint
{
    public static void MapRejectExpenseRequest(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/expense-requests/{id:int}/reject", async (int id, RejectExpenseRequestDto dto, ISender sender) =>
        {
            var result = await sender.Send(new RejectExpenseRequestCommand(id, dto.Reason));
            return result.ToOkResult();
        })
        .AddEndpointFilter<AdminAuthFilter>()
        .WithName("RejectExpenseRequest")
        .WithOpenApi();
    }
}
