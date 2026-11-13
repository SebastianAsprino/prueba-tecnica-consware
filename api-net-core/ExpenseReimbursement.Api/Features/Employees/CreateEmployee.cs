using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using ExpenseReimbursement.Api.Domain;
using ExpenseReimbursement.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Features.Employees;

public record CreateEmployeeCommand(string Name) : IRequest<AppResult<EmployeeDto>>;

public class CreateEmployeeHandler(AppDbContext db) : IRequestHandler<CreateEmployeeCommand, AppResult<EmployeeDto>>
{
    public async Task<AppResult<EmployeeDto>> Handle(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return AppResult<EmployeeDto>.ValidationError("El nombre del empleado es obligatorio.");
        }

        var name = command.Name.Trim();
        if (name.Length > Employee.NameMaxLength)
        {
            return AppResult<EmployeeDto>.ValidationError($"El nombre no puede superar {Employee.NameMaxLength} caracteres.");
        }

        var exists = await db.Employees.AnyAsync(e => e.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
        {
            return AppResult<EmployeeDto>.ValidationError($"Ya existe un empleado con el nombre '{name}'.");
        }

        var accessCode = await AccessCodeGenerator.GenerateUniqueAsync(
            code => db.Employees.AnyAsync(e => e.AccessCode == code, cancellationToken));

        var employee = Employee.Create(name, accessCode);
        db.Employees.Add(employee);
        await db.SaveChangesAsync(cancellationToken);

        return AppResult<EmployeeDto>.Ok(EmployeeDto.FromDomain(employee));
    }
}

public static class CreateEmployeeEndpoint
{
    public static void MapCreateEmployee(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/employees", async (CreateEmployeeDto dto, ISender sender) =>
        {
            var result = await sender.Send(new CreateEmployeeCommand(dto.Name));
            return result.ToCreatedResult(value => $"/api/employees/{value.Id}");
        })
        .AddEndpointFilter<AdminAuthFilter>()
        .WithName("CreateEmployee")
        .WithOpenApi();
    }
}
