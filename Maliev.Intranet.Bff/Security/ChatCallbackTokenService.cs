using Microsoft.AspNetCore.DataProtection;

namespace Maliev.Intranet.Bff.Security;

/// <summary>
/// Creates and validates short-lived tokens for unauthenticated chatbot callback posts.
/// </summary>
public interface IChatCallbackTokenService
{
    /// <summary>
    /// Creates a callback token bound to the supplied chat session identifier.
    /// </summary>
    string CreateToken(Guid sessionId);

    /// <summary>
    /// Returns true when the token is valid and bound to the supplied chat session identifier.
    /// </summary>
    bool IsValid(Guid sessionId, string? token);
}

/// <summary>
/// Data Protection backed implementation for chatbot callback tokens.
/// </summary>
public sealed class ChatCallbackTokenService : IChatCallbackTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);
    private readonly ITimeLimitedDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCallbackTokenService"/> class.
    /// </summary>
    public ChatCallbackTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider
            .CreateProtector("Maliev.Intranet.ChatCallback.v1")
            .ToTimeLimitedDataProtector();
    }

    /// <inheritdoc />
    public string CreateToken(Guid sessionId)
    {
        return _protector.Protect(sessionId.ToString("D"), TokenLifetime);
    }

    /// <inheritdoc />
    public bool IsValid(Guid sessionId, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var protectedSessionId = _protector.Unprotect(token);
            return Guid.TryParse(protectedSessionId, out var parsedSessionId) && parsedSessionId == sessionId;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
