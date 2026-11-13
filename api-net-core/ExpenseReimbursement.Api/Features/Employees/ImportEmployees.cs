using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Contracts;
using ExpenseReimbursement.Api.Domain;
using ExpenseReimbursement.Api.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Features.Employees;

public record ImportEmployeesCommand(string CsvContent) : IRequest<AppResult<ImportEmployeesResultDto>>;

public class ImportEmployeesHandler(AppDbContext db) : IRequestHandler<ImportEmployeesCommand, AppResult<ImportEmployeesResultDto>>
{
    public async Task<AppResult<ImportEmployeesResultDto>> Handle(ImportEmployeesCommand command, CancellationToken cancellationToken)
    {
        var existingNames = (await db.Employees.Select(e => e.Name).ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingCodes = (await db.Employees.Select(e => e.AccessCode).ToListAsync(cancellationToken))
            .ToHashSet();

        var created = new List<Employee>();
        var skipped = new List<ImportEmployeeErrorDto>();

        var lines = command.CsvContent.Replace("\r\n", "\n").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var lineNumber = i + 1;

            var name = rawLine.Split(',')[0].Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (lineNumber == 1 && name.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (name.Length > Employee.NameMaxLength)
            {
                skipped.Add(new ImportEmployeeErrorDto(lineNumber, name, $"El nombre supera {Employee.NameMaxLength} caracteres."));
                continue;
            }

            if (existingNames.Contains(name))
            {
                skipped.Add(new ImportEmployeeErrorDto(lineNumber, name, "Ya existe un empleado con ese nombre."));
                continue;
            }

            string accessCode;
            do
            {
                accessCode = AccessCodeGenerator.Generate();
            } while (!existingCodes.Add(accessCode));

            var employee = Employee.Create(name, accessCode);
            db.Employees.Add(employee);
            existingNames.Add(name);
            created.Add(employee);
        }

        if (created.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return AppResult<ImportEmployeesResultDto>.Ok(
            new ImportEmployeesResultDto(created.Select(EmployeeDto.FromDomain).ToList(), skipped));
    }
}

public static class ImportEmployeesEndpoint
{
    public static void MapImportEmployees(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/employees/import", async (IFormFile file, ISender sender) =>
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            var result = await sender.Send(new ImportEmployeesCommand(content));
            return result.ToOkResult();
        })
        .DisableAntiforgery()
        .AddEndpointFilter<AdminAuthFilter>()
        .WithName("ImportEmployees")
        .WithOpenApi();
    }
}
