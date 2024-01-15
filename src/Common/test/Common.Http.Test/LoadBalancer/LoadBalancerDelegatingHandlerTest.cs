// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net;
using Steeltoe.Common.Http.LoadBalancer;
using Xunit;

namespace Steeltoe.Common.Http.Test.LoadBalancer;

public sealed class LoadBalancerDelegatingHandlerTest
{
    [Fact]
    public async Task ResolvesUri_TracksStatistics_WithProvidedLoadBalancer()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new FakeLoadBalancer();

        var handler = new LoadBalancerDelegatingHandler(loadBalancer)
        {
            InnerHandler = new TestInnerDelegatingHandler()
        };

        var invoker = new HttpMessageInvoker(handler);

        HttpResponseMessage result = await invoker.SendAsync(httpRequestMessage, default);

        Assert.Equal("https://some-resolved-host:1234/api", result.Headers.GetValues("requestUri").First());
        Assert.Single(loadBalancer.Statistics);
    }

    [Fact]
    public async Task DoesNotTrackStatistics_WhenResolutionFails_WithProvidedLoadBalancer()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new BrokenLoadBalancer();

        var handler = new LoadBalancerDelegatingHandler(loadBalancer)
        {
            InnerHandler = new TestInnerDelegatingHandler()
        };

        var invoker = new HttpMessageInvoker(handler);

        await Assert.ThrowsAsync<Exception>(async () => await invoker.SendAsync(httpRequestMessage, default));

        Assert.Empty(loadBalancer.Statistics);
    }

    [Fact]
    public async Task TracksStatistics_WhenRequestsGoWrong_WithProvidedLoadBalancer()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new FakeLoadBalancer();

        var handler = new LoadBalancerDelegatingHandler(loadBalancer)
        {
            InnerHandler = new TestInnerDelegatingHandlerBrokenServer()
        };

        var invoker = new HttpMessageInvoker(handler);

        HttpResponseMessage result = await invoker.SendAsync(httpRequestMessage, default);

        Assert.Single(loadBalancer.Statistics);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("https://some-resolved-host:1234/api", result.Headers.GetValues("requestUri").First());
    }
}
