using System.Security.Cryptography;

namespace ExpenseReimbursement.Api.Common;

public static class AccessCodeGenerator
{
    public const int Length = 10;

    public static string Generate() =>
        string.Concat(Enumerable.Range(0, Length).Select(_ => RandomNumberGenerator.GetInt32(0, 10)));

    public static async Task<string> GenerateUniqueAsync(Func<string, Task<bool>> existsAsync)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = Generate();
            if (!await existsAsync(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No se pudo generar un código de acceso único.");
    }
}
