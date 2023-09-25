// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Http.Discovery;
using Steeltoe.Discovery;
using Xunit;

namespace Steeltoe.Common.Http.Test.Discovery;

public sealed class DiscoveryHttpClientHandlerBaseTest
{
    [Fact]
    public void Constructor_ThrowsIfClientNull()
    {
        const IDiscoveryClient client = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new DiscoveryHttpClientHandlerBase(client));
        Assert.Contains(nameof(client), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LookupServiceAsync_NonDefaultPort_ReturnsOriginalURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient();
        var handler = new DiscoveryHttpClientHandlerBase(client);
        var uri = new Uri("https://foo:8080/test");

        Uri result = await handler.LookupServiceAsync(uri, CancellationToken.None);
        Assert.Equal(uri, result);
    }

    [Fact]
    public async Task LookupServiceAsync_DoesNotFindService_ReturnsOriginalURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient();
        var handler = new DiscoveryHttpClientHandlerBase(client);
        var uri = new Uri("https://foo/test");

        Uri result = await handler.LookupServiceAsync(uri, CancellationToken.None);
        Assert.Equal(uri, result);
    }

    [Fact]
    public async Task LookupServiceAsync_FindsService_ReturnsURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient(new TestServiceInstance(new Uri("https://foundit:5555")));
        var handler = new DiscoveryHttpClientHandlerBase(client);
        var uri = new Uri("https://foo/test/bar/foo?test=1&test2=2");

        Uri result = await handler.LookupServiceAsync(uri, CancellationToken.None);
        Assert.Equal(new Uri("https://foundit:5555/test/bar/foo?test=1&test2=2"), result);
    }
}
