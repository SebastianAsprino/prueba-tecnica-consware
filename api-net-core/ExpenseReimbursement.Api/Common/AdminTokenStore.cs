using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ExpenseReimbursement.Api.Common;

/// <summary>
/// Almacén en memoria de tokens opacos del administrador. Los tokens se generan con
/// un CSPRNG (256 bits) y expiran tras <see cref="TokenLifetime"/>.
/// </summary>
public class AdminTokenStore(TimeProvider timeProvider)
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);
    private const int TokenSizeInBytes = 32;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _tokens = new();

    public AdminTokenStore() : this(TimeProvider.System)
    {
    }

    public string Issue()
    {
        RemoveExpiredTokens();

        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
        _tokens[token] = timeProvider.GetUtcNow().Add(TokenLifetime);
        return token;
    }

    public bool IsValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (!_tokens.TryGetValue(token, out var expiresAt))
        {
            return false;
        }

        if (expiresAt < timeProvider.GetUtcNow())
        {
            _tokens.TryRemove(token, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Evita que el diccionario crezca sin límite con tokens que nunca se vuelven a usar.
    /// </summary>
    private void RemoveExpiredTokens()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var (token, expiresAt) in _tokens)
        {
            if (expiresAt < now)
            {
                _tokens.TryRemove(token, out _);
            }
        }
    }
}
