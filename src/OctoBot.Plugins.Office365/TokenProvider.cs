using Microsoft.Kiota.Abstractions.Authentication;

namespace OctoBot.Plugins.Office365;

internal class TokenProvider : IAccessTokenProvider
{
    private readonly string _accessToken;

    public TokenProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_accessToken);
    }
}
