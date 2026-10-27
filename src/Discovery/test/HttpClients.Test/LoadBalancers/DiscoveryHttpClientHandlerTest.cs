// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

public sealed class DiscoveryHttpClientHandlerTest
{
    [Fact]
    public async Task DoesNotTrackStatistics_WhenResolutionFails_WithProvidedLoadBalancer()
    {
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new BrokenLoadBalancer();

        var handler = new DiscoveryHttpClientHandler(loadBalancer, TimeProvider.System);
        using var invoker = new HttpMessageInvoker(handler);

        // ReSharper disable AccessToDisposedClosure
        Func<Task> action = async () => _ = await invoker.SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
        // ReSharper restore AccessToDisposedClosure

        await action.Should().ThrowExactlyAsync<DataException>();
        loadBalancer.Statistics.Should().BeEmpty();
    }

    [Fact]
    public async Task DoesNotResolveOrTrackStatistics_WhenRequestUsesNonDefaultPort()
    {
        // DiscoveryHttpClientHandler derives from HttpClientHandler, which always sends the request over the network, so there is no way to fake the
        // server. The "ftp" scheme is only used because HttpClientHandler rejects it immediately, which keeps this test fast (otherwise it would wait for
        // a DNS lookup of the made-up host name). The scheme is irrelevant to what is being tested: a port other than the default one means the
        // request is not for service discovery, so the load balancer must not be involved.

        var requestUri = new Uri("ftp://replace-me:8080/api");
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
        var loadBalancer = new FakeLoadBalancer();

        var handler = new DiscoveryHttpClientHandler(loadBalancer, TimeProvider.System);
        using var invoker = new HttpMessageInvoker(handler);

        // ReSharper disable AccessToDisposedClosure
        Func<Task> action = async () => _ = await invoker.SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
        // ReSharper restore AccessToDisposedClosure

        await action.Should().ThrowExactlyAsync<NotSupportedException>();

        // Had the load balancer resolved the request, it would have tracked the outcome, even though sending failed.
        loadBalancer.Statistics.Should().BeEmpty();
        httpRequestMessage.RequestUri.Should().Be(requestUri);
    }

    [Fact]
    public async Task TracksStatistics_WhenRequestsGoWrong_WithProvidedLoadBalancer()
    {
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new FakeLoadBalancer();

        var handler = new DiscoveryHttpClientHandler(loadBalancer, TimeProvider.System);
        using var invoker = new HttpMessageInvoker(handler);

        // ReSharper disable AccessToDisposedClosure
        Func<Task> action = async () => _ = await invoker.SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
        // ReSharper restore AccessToDisposedClosure

        await action.Should().ThrowExactlyAsync<HttpRequestException>();
        loadBalancer.Statistics.Should().ContainSingle();
    }
}
