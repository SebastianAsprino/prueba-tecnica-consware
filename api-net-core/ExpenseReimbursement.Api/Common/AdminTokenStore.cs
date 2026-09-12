using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ExpenseReimbursement.Api.Common;

public class AdminTokenStore
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, DateTime> _tokens = new();

    public string Issue()
    {
        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        _tokens[token] = DateTime.UtcNow.Add(TokenLifetime);
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

        if (expiresAt < DateTime.UtcNow)
        {
            _tokens.TryRemove(token, out _);
            return false;
        }

        return true;
    }
}
