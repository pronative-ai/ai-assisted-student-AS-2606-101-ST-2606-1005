namespace Bookkeeping.Infrastructure;

using System.Collections.Concurrent;

public class TokenStore
{
    private readonly ConcurrentDictionary<string, (string Username, string Role, DateTime Expiry)> _tokens = new();

    public string Issue(string username, string role)
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = (username, role, DateTime.UtcNow.AddHours(8));
        return token;
    }

    public bool IsValid(string token) =>
        _tokens.TryGetValue(token, out var session) && session.Expiry > DateTime.UtcNow;
}
