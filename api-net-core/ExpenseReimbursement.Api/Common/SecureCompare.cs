using System.Security.Cryptography;
using System.Text;

namespace ExpenseReimbursement.Api.Common;

/// <summary>
/// Comparación de secretos en tiempo constante, para no filtrar por timing
/// cuántos caracteres de una credencial coinciden.
/// </summary>
public static class SecureCompare
{
    public static bool FixedTimeEquals(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        // Se comparan los hashes (longitud fija) para que el tiempo tampoco
        // dependa de la longitud del secreto esperado.
        var leftHash = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        var rightHash = SHA256.HashData(Encoding.UTF8.GetBytes(right));
        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }
}
