using ExpenseReimbursement.Api.Domain;

namespace ExpenseReimbursement.Api.Contracts;

public record ExpenseRequestDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    decimal Amount,
    string Category,
    string Description,
    ExpenseStatus Status,
    bool AutoApproved,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? DecidedAt)
{
    public static ExpenseRequestDto FromDomain(ExpenseRequest request) => new(
        request.Id,
        request.EmployeeId,
        request.Employee.Name,
        request.Amount,
        request.Category,
        request.Description,
        request.Status,
        request.AutoApproved,
        request.RejectionReason,
        request.CreatedAt,
        request.DecidedAt);
}

public record CreateExpenseRequestDto(int EmployeeId, decimal Amount, string Category, string Description);

public record RejectExpenseRequestDto(string Reason);
