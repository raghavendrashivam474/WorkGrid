namespace WorkGrid.Infrastructure.Remote;

/// <summary>
/// Immutable configuration for the remote WorkGrid API endpoint.
/// No credentials are stored here.
/// </summary>
public sealed record RemoteEndpoint
{
    public Uri BaseUri { get; }
    public TimeSpan Timeout { get; }

    public RemoteEndpoint(Uri baseUri, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(baseUri);

        if (!baseUri.IsAbsoluteUri)
            throw new ArgumentException(
                "Endpoint must be an absolute URI.", nameof(baseUri));

        if (baseUri.Scheme != Uri.UriSchemeHttp &&
            baseUri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException(
                "Endpoint must use HTTP or HTTPS.", nameof(baseUri));

        BaseUri = baseUri;
        Timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    public static RemoteEndpoint FromUrl(
        string url, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new RemoteEndpoint(new Uri(url), timeout);
    }
}
