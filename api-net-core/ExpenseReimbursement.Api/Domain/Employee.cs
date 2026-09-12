namespace ExpenseReimbursement.Api.Domain;

public class Employee
{
    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string AccessCode { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private Employee()
    {
    }

    public static Employee Create(string name, string accessCode) => new()
    {
        Name = name,
        AccessCode = accessCode,
        CreatedAt = DateTime.UtcNow
    };
}
