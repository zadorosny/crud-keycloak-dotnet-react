namespace MfaCrud.Api.Keycloak;

/// <summary>
/// Holds the service account token between requests so the API does not ask Keycloak for a new one
/// on every call. Renewed 30s before it expires.
/// </summary>
public sealed class ServiceTokenCache : IDisposable
{
    private static readonly TimeSpan RenewalMargin = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeProvider _time;

    private string? _token;
    private DateTimeOffset _expiresAt;

    public ServiceTokenCache(TimeProvider? timeProvider = null) => _time = timeProvider ?? TimeProvider.System;

    public async Task<string> GetAsync(
        Func<CancellationToken, Task<(string Token, TimeSpan ExpiresIn)>> fetch, CancellationToken cancellationToken)
    {
        if (IsFresh())
        {
            return _token!;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (IsFresh())
            {
                return _token!;
            }

            var (token, expiresIn) = await fetch(cancellationToken);
            _token = token;
            _expiresAt = _time.GetUtcNow() + expiresIn;
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsFresh() => _token is not null && _time.GetUtcNow() + RenewalMargin < _expiresAt;

    public void Dispose() => _gate.Dispose();
}
