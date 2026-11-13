namespace ExpenseReimbursement.Api.Domain;

public class ExpenseRequest
{
    public const decimal AutoApprovalThreshold = 500_000m;
    public const int CategoryMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int RejectionReasonMaxLength = 500;

    public int Id { get; private set; }
    public int EmployeeId { get; private set; }
    public Employee Employee { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public string Category { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public ExpenseStatus Status { get; private set; }
    public bool AutoApproved { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DecidedAt { get; private set; }

    private ExpenseRequest()
    {
    }

    public static ExpenseRequest Create(int employeeId, decimal amount, string category, string description)
    {
        var request = new ExpenseRequest
        {
            EmployeeId = employeeId,
            Amount = amount,
            Category = category,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            Status = amount <= AutoApprovalThreshold ? ExpenseStatus.Approved : ExpenseStatus.Pending
        };

        if (request.Status == ExpenseStatus.Approved)
        {
            request.DecidedAt = request.CreatedAt;
            request.AutoApproved = true;
        }

        return request;
    }

    public bool TryApprove(out string? error)
    {
        if (Status != ExpenseStatus.Pending)
        {
            error = $"La solicitud ya está en estado '{Status}' y no puede aprobarse.";
            return false;
        }

        Status = ExpenseStatus.Approved;
        DecidedAt = DateTime.UtcNow;
        error = null;
        return true;
    }

    public bool TryReject(string reason, out string? error)
    {
        if (Status != ExpenseStatus.Pending)
        {
            error = $"La solicitud ya está en estado '{Status}' y no puede rechazarse.";
            return false;
        }

        Status = ExpenseStatus.Rejected;
        RejectionReason = reason;
        DecidedAt = DateTime.UtcNow;
        error = null;
        return true;
    }
}
