using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Domain;
using ExpenseReimbursement.Api.Features.Auth;
using ExpenseReimbursement.Api.Features.Employees;
using ExpenseReimbursement.Api.Features.ExpenseRequests;
using ExpenseReimbursement.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExpenseReimbursement.Api.Tests;

/// <summary>
/// Tests de los handlers contra una base SQLite en memoria (mismo motor que producción).
/// </summary>
public sealed class HandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public HandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateEmployee_GeneratesAccessCode_AndRejectsDuplicates()
    {
        var handler = new CreateEmployeeHandler(_db);

        var created = await handler.Handle(new CreateEmployeeCommand("  Ana Pérez "), CancellationToken.None);
        var duplicate = await handler.Handle(new CreateEmployeeCommand("ana pérez"), CancellationToken.None);

        Assert.Equal(AppResultStatus.Ok, created.Status);
        Assert.Equal("Ana Pérez", created.Value!.Name);
        Assert.Equal(AccessCodeGenerator.Length, created.Value.AccessCode.Length);
        Assert.Equal(AppResultStatus.ValidationError, duplicate.Status);
    }

    [Fact]
    public async Task CreateEmployee_NameTooLong_IsValidationError()
    {
        var handler = new CreateEmployeeHandler(_db);

        var result = await handler.Handle(
            new CreateEmployeeCommand(new string('a', Employee.NameMaxLength + 1)), CancellationToken.None);

        Assert.Equal(AppResultStatus.ValidationError, result.Status);
    }

    [Fact]
    public async Task ImportEmployees_SkipsHeaderBlanksAndDuplicates()
    {
        var handler = new ImportEmployeesHandler(_db);
        const string csv = "Name\r\nAna\r\n\r\nLuis\r\nana\r\n";

        var result = await handler.Handle(new ImportEmployeesCommand(csv), CancellationToken.None);

        Assert.Equal(AppResultStatus.Ok, result.Status);
        Assert.Equal(["Ana", "Luis"], result.Value!.Created.Select(e => e.Name));
        var skipped = Assert.Single(result.Value.Skipped);
        Assert.Equal(5, skipped.Line);
    }

    [Fact]
    public async Task CreateExpenseRequest_ReturnsEmployeeName_AndAppliesBusinessRule()
    {
        var employee = await SeedEmployeeAsync("Ana");
        var handler = new CreateExpenseRequestHandler(_db);

        var small = await handler.Handle(
            new CreateExpenseRequestCommand(employee.Id, 100_000m, "Comida", "Almuerzo"), CancellationToken.None);
        var large = await handler.Handle(
            new CreateExpenseRequestCommand(employee.Id, 700_000m, "Viáticos", "Hotel"), CancellationToken.None);

        Assert.Equal("Ana", small.Value!.EmployeeName);
        Assert.Equal(ExpenseStatus.Approved, small.Value.Status);
        Assert.True(small.Value.AutoApproved);
        Assert.Equal(ExpenseStatus.Pending, large.Value!.Status);
    }

    [Fact]
    public async Task CreateExpenseRequest_InvalidInput_ReturnsAllErrors()
    {
        var handler = new CreateExpenseRequestHandler(_db);

        var result = await handler.Handle(
            new CreateExpenseRequestCommand(1, 0m, "", new string('x', ExpenseRequest.DescriptionMaxLength + 1)),
            CancellationToken.None);

        Assert.Equal(AppResultStatus.ValidationError, result.Status);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public async Task CreateExpenseRequest_UnknownEmployee_IsNotFound()
    {
        var handler = new CreateExpenseRequestHandler(_db);

        var result = await handler.Handle(
            new CreateExpenseRequestCommand(999, 10m, "Comida", "Almuerzo"), CancellationToken.None);

        Assert.Equal(AppResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ApproveAndReject_OnlyWorkOnPendingRequests()
    {
        var employee = await SeedEmployeeAsync("Luis");
        var create = new CreateExpenseRequestHandler(_db);
        var pending = (await create.Handle(
            new CreateExpenseRequestCommand(employee.Id, 900_000m, "Viáticos", "Hotel"), CancellationToken.None)).Value!;

        var reject = new RejectExpenseRequestHandler(_db);
        var rejected = await reject.Handle(new RejectExpenseRequestCommand(pending.Id, "Sin factura"), CancellationToken.None);
        var approveAfterReject = await new ApproveExpenseRequestHandler(_db)
            .Handle(new ApproveExpenseRequestCommand(pending.Id), CancellationToken.None);

        Assert.Equal(ExpenseStatus.Rejected, rejected.Value!.Status);
        Assert.Equal(AppResultStatus.Conflict, approveAfterReject.Status);
    }

    [Fact]
    public async Task ListExpenseRequests_InvalidStatus_IsValidationError()
    {
        var result = await new ListExpenseRequestsHandler(_db)
            .Handle(new ListExpenseRequestsQuery("Desconocido", null), CancellationToken.None);

        Assert.Equal(AppResultStatus.ValidationError, result.Status);
    }

    [Theory]
    [InlineData("admin", "secreto", true)]
    [InlineData("admin", "otro", false)]
    [InlineData("otro", "secreto", false)]
    [InlineData("", "", false)]
    public async Task Login_ValidatesConfiguredCredentials(string username, string password, bool expectedOk)
    {
        var options = Options.Create(new AdminOptions { Username = "admin", Password = "secreto" });
        var handler = new LoginHandler(options, new AdminTokenStore());

        var result = await handler.Handle(new LoginCommand(username, password), CancellationToken.None);

        Assert.Equal(expectedOk ? AppResultStatus.Ok : AppResultStatus.Unauthorized, result.Status);
    }

    private async Task<Employee> SeedEmployeeAsync(string name)
    {
        var employee = Employee.Create(name, AccessCodeGenerator.Generate());
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return employee;
    }
}
