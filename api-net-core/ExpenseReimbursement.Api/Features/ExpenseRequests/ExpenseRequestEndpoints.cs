namespace ExpenseReimbursement.Api.Features.ExpenseRequests;

public static class ExpenseRequestEndpoints
{
    public static void MapExpenseRequestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCreateExpenseRequest();
        app.MapListExpenseRequests();
        app.MapGetExpenseRequestById();
        app.MapApproveExpenseRequest();
        app.MapRejectExpenseRequest();
    }
}
