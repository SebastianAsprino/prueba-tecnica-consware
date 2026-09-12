namespace ExpenseReimbursement.Api.Contracts;

public record LoginRequestDto(string Username, string Password);

public record LoginResponseDto(string Token);
