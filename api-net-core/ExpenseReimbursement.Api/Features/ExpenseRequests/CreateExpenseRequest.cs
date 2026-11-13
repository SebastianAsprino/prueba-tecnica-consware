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
        else if (command.Category.Length > ExpenseRequest.CategoryMaxLength)
        {
            errors.Add($"La categoría no puede superar {ExpenseRequest.CategoryMaxLength} caracteres.");
        }
        if (string.IsNullOrWhiteSpace(command.Description))
        {
            errors.Add("La descripción es obligatoria.");
        }
        else if (command.Description.Length > ExpenseRequest.DescriptionMaxLength)
        {
            errors.Add($"La descripción no puede superar {ExpenseRequest.DescriptionMaxLength} caracteres.");
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

        // El empleado ya está trackeado por el DbContext, así que EF Core enlaza
        // request.Employee al agregar la solicitud (no hace falta otra consulta).
        return AppResult<ExpenseRequestDto>.Ok(ExpenseRequestDto.FromDomain(request));
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
