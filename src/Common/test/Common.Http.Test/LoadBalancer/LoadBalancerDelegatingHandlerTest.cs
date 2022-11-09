// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Steeltoe.Common.Http.LoadBalancer;
using Xunit;

namespace Steeltoe.Common.Http.Test.LoadBalancer;

public class LoadBalancerDelegatingHandlerTest
{
    [Fact]
    public void Throws_If_LoadBalancerNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new LoadBalancerDelegatingHandler(null));
        Assert.Equal("loadBalancer", exception.ParamName);
    }

    [Fact]
    public async Task ResolvesUri_TracksStats_WithProvidedLoadBalancer()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://replaceme/api");
        var loadBalancer = new FakeLoadBalancer();

        var handler = new LoadBalancerDelegatingHandler(loadBalancer)
        {
            InnerHandler = new TestInnerDelegatingHandler()
        };

        var invoker = new HttpMessageInvoker(handler);

        HttpResponseMessage result = await invoker.SendAsync(httpRequestMessage, default);

        Assert.Equal("https://someresolvedhost/api", result.Headers.GetValues("requestUri").First());
        Assert.Single(loadBalancer.Stats);
    }

    [Fact]
    public async Task DoesNotTrackStats_WhenResolutionFails_WithProvidedLoadBalancer()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://replaceme/api");
        var loadBalancer = new BrokenLoadBalancer();

        var handler = new LoadBalancerDelegatingHandler(loadBalancer)
        {
            InnerHandler = new TestInnerDelegatingHandler()
        };

        var invoker = new HttpMessageInvoker(handler);

        await Assert.ThrowsAsync<Exception>(async () => await invoker.SendAsync(httpRequestMessage, default));

        Assert.Empty(loadBalancer.Stats);
    }

    [Fact]
    public async Task TracksStats_WhenRequestsGoWrong_WithProvidedLoadBalancer()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://replaceme/api");
        var loadBalancer = new FakeLoadBalancer();

        var handler = new LoadBalancerDelegatingHandler(loadBalancer)
        {
            InnerHandler = new TestInnerDelegatingHandlerBrokenServer()
        };

        var invoker = new HttpMessageInvoker(handler);

        HttpResponseMessage result = await invoker.SendAsync(httpRequestMessage, default);

        Assert.Single(loadBalancer.Stats);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("https://someresolvedhost/api", result.Headers.GetValues("requestUri").First());
    }
}
