using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using ExpenseReimbursement.Api.Domain;
using ExpenseReimbursement.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Features.ExpenseRequests;

public record CreateExpenseRequestCommand(int EmployeeId, decimal Amount, string Category, string Description)
    : IRequest<AppResult<ExpenseRequestDto>>;

public class CreateExpenseRequestHandler(AppDbContext db) : IRequestHandler<CreateExpenseRequestCommand, AppResult<ExpenseRequestDto>>
{
    public async Task<AppResult<ExpenseRequestDto>> Handle(CreateExpenseRequestCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (command.Amount <= 0)
        {
            errors.Add("El monto debe ser mayor que cero.");
        }
        if (string.IsNullOrWhiteSpace(command.Category))
        {
            errors.Add("La categoría es obligatoria.");
        }
        if (string.IsNullOrWhiteSpace(command.Description))
        {
            errors.Add("La descripción es obligatoria.");
        }

        if (errors.Count > 0)
        {
            return AppResult<ExpenseRequestDto>.ValidationError([.. errors]);
        }

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == command.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return AppResult<ExpenseRequestDto>.NotFound($"No existe un empleado con id {command.EmployeeId}.");
        }

        var request = ExpenseRequest.Create(employee.Id, command.Amount, command.Category, command.Description);

        db.ExpenseRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);

        var dto = new ExpenseRequestDto(
            request.Id,
            employee.Id,
            employee.Name,
            request.Amount,
            request.Category,
            request.Description,
            request.Status,
            request.AutoApproved,
            request.RejectionReason,
            request.CreatedAt,
            request.DecidedAt);

        return AppResult<ExpenseRequestDto>.Ok(dto);
    }
}

public static class CreateExpenseRequestEndpoint
{
    public static void MapCreateExpenseRequest(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/expense-requests", async (CreateExpenseRequestDto dto, ISender sender) =>
        {
            var result = await sender.Send(new CreateExpenseRequestCommand(dto.EmployeeId, dto.Amount, dto.Category, dto.Description));
            return result.ToCreatedResult(value => $"/api/expense-requests/{value.Id}");
        })
        .WithName("CreateExpenseRequest")
        .WithOpenApi();
    }
}
