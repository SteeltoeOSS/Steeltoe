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
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new BrokenLoadBalancer();

        var handler = new DiscoveryHttpClientHandler(loadBalancer);
        using var invoker = new HttpMessageInvoker(handler);

        Func<Task<HttpResponseMessage>> action = async () => _ = await invoker.SendAsync(httpRequestMessage, default);

        await action.Should().ThrowExactlyAsync<DataException>();
        loadBalancer.Statistics.Should().BeEmpty();
    }

    [Fact]
    public async Task TracksStatistics_WhenRequestsGoWrong_WithProvidedLoadBalancer()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new FakeLoadBalancer();

        var handler = new DiscoveryHttpClientHandler(loadBalancer);
        using var invoker = new HttpMessageInvoker(handler);

        Func<Task<HttpResponseMessage>> action = async () => _ = await invoker.SendAsync(httpRequestMessage, default);

        await action.Should().ThrowExactlyAsync<HttpRequestException>();
        loadBalancer.Statistics.Should().ContainSingle();
    }
}
