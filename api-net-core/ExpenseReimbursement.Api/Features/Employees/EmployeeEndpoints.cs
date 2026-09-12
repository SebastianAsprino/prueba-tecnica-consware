namespace ExpenseReimbursement.Api.Features.Employees;

public static class EmployeeEndpoints
{
    public static void MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCreateEmployee();
        app.MapListEmployees();
        app.MapImportEmployees();
        app.MapGetEmployeeByAccessCode();
    }
}
