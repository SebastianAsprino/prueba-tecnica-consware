using ExpenseReimbursement.Api.Domain;

namespace ExpenseReimbursement.Api.Contracts;

public record EmployeeDto(int Id, string Name, string AccessCode, DateTime CreatedAt)
{
    public static EmployeeDto FromDomain(Employee employee) => new(employee.Id, employee.Name, employee.AccessCode, employee.CreatedAt);
}

public record CreateEmployeeDto(string Name);

public record ImportEmployeesResultDto(List<EmployeeDto> Created, List<ImportEmployeeErrorDto> Skipped);

public record ImportEmployeeErrorDto(int Line, string Value, string Reason);
