using System;
using WorkGrid.Infrastructure.Remote;
using Xunit;

namespace WorkGrid.Infrastructure.Tests.Remote;

public class RemoteEndpointTests
{
    [Fact]
    public void FromUrl_WithValidHttpsUrl_CreatesEndpoint()
    {
        var endpoint = RemoteEndpoint.FromUrl("https://api.workgrid.local:5001");

        Assert.Equal("https://api.workgrid.local:5001/", endpoint.BaseUri.ToString());
        Assert.Equal(TimeSpan.FromSeconds(30), endpoint.Timeout);
    }

    [Fact]
    public void FromUrl_WithCustomTimeout_SetsTimeout()
    {
        var customTimeout = TimeSpan.FromSeconds(10);
        var endpoint = RemoteEndpoint.FromUrl("https://api.workgrid.local", customTimeout);

        Assert.Equal(customTimeout, endpoint.Timeout);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FromUrl_WithInvalidOrEmptyUrl_ThrowsArgumentException(string? invalidUrl)
    {
        Assert.ThrowsAny<ArgumentException>(() => RemoteEndpoint.FromUrl(invalidUrl!));
    }

    [Fact]
    public void Constructor_WithRelativeUri_ThrowsArgumentException()
    {
        var relativeUri = new Uri("/api/v1", UriKind.Relative);

        Assert.Throws<ArgumentException>(() => new RemoteEndpoint(relativeUri));
    }

    [Fact]
    public void Constructor_WithUnsupportedScheme_ThrowsArgumentException()
    {
        var ftpUri = new Uri("ftp://ftp.workgrid.local");

        Assert.Throws<ArgumentException>(() => new RemoteEndpoint(ftpUri));
    }
}
