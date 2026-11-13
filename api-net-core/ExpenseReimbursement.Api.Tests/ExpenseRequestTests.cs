using ExpenseReimbursement.Api.Domain;

namespace ExpenseReimbursement.Api.Tests;

public class ExpenseRequestTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(500_000)]
    public void Create_AtOrBelowThreshold_IsAutoApproved(decimal amount)
    {
        var request = ExpenseRequest.Create(1, amount, "Transporte", "Taxi");

        Assert.Equal(ExpenseStatus.Approved, request.Status);
        Assert.True(request.AutoApproved);
        Assert.Equal(request.CreatedAt, request.DecidedAt);
    }

    [Fact]
    public void Create_AboveThreshold_IsPending()
    {
        var request = ExpenseRequest.Create(1, 500_000.01m, "Viáticos", "Hotel");

        Assert.Equal(ExpenseStatus.Pending, request.Status);
        Assert.False(request.AutoApproved);
        Assert.Null(request.DecidedAt);
    }

    [Fact]
    public void TryApprove_Pending_Approves()
    {
        var request = ExpenseRequest.Create(1, 900_000m, "Viáticos", "Hotel");

        var ok = request.TryApprove(out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(ExpenseStatus.Approved, request.Status);
        Assert.False(request.AutoApproved);
        Assert.NotNull(request.DecidedAt);
    }

    [Fact]
    public void TryReject_Pending_StoresReason()
    {
        var request = ExpenseRequest.Create(1, 900_000m, "Viáticos", "Hotel");

        var ok = request.TryReject("Sin factura", out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(ExpenseStatus.Rejected, request.Status);
        Assert.Equal("Sin factura", request.RejectionReason);
    }

    [Fact]
    public void DecidedRequest_CannotChangeState()
    {
        var request = ExpenseRequest.Create(1, 100m, "Comida", "Almuerzo");

        Assert.False(request.TryApprove(out var approveError));
        Assert.NotNull(approveError);
        Assert.False(request.TryReject("x", out var rejectError));
        Assert.NotNull(rejectError);
        Assert.Equal(ExpenseStatus.Approved, request.Status);
    }
}
