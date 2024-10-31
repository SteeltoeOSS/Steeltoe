// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

public sealed class DiscoveryHttpDelegatingHandlerTest
{
    [Fact]
    public async Task ResolvesUri_TracksStatistics_WithProvidedLoadBalancer()
    {
        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new FakeLoadBalancer();

        var services = new ServiceCollection();
        services.AddSingleton(loadBalancer);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var handler = new DiscoveryHttpDelegatingHandler<FakeLoadBalancer>(serviceProvider)
        {
            InnerHandler = new TestInnerDelegatingHandler()
        };

        using var invoker = new HttpMessageInvoker(handler);

        HttpResponseMessage result = await invoker.SendAsync(httpRequestMessage, default);

        result.Headers.GetValues("requestUri").First().Should().Be("https://some-resolved-host:1234/api");
        loadBalancer.Statistics.Should().ContainSingle();
    }

    [Fact]
    public async Task DoesNotTrackStatistics_WhenRequestIsCanceled()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new FakeLoadBalancer();

        var services = new ServiceCollection();
        services.AddSingleton(loadBalancer);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var handler = new DiscoveryHttpDelegatingHandler<FakeLoadBalancer>(serviceProvider)
        {
            InnerHandler = new TestInnerDelegatingHandler
            {
                IsCanceled = true
            }
        };

        using var invoker = new HttpMessageInvoker(handler);

        Func<Task<HttpResponseMessage>> action = async () => await invoker.SendAsync(httpRequestMessage, default);

        await action.Should().ThrowExactlyAsync<OperationCanceledException>();
        loadBalancer.Statistics.Should().BeEmpty();
    }

    [Fact]
    public async Task DoesNotTrackStatistics_WhenResolutionFails_WithProvidedLoadBalancer()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new BrokenLoadBalancer();

        var services = new ServiceCollection();
        services.AddSingleton(loadBalancer);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var handler = new DiscoveryHttpDelegatingHandler<BrokenLoadBalancer>(serviceProvider)
        {
            InnerHandler = new TestInnerDelegatingHandler()
        };

        using var invoker = new HttpMessageInvoker(handler);

        Func<Task<HttpResponseMessage>> action = async () => await invoker.SendAsync(httpRequestMessage, default);

        await action.Should().ThrowExactlyAsync<DataException>();
        loadBalancer.Statistics.Should().BeEmpty();
    }

    [Fact]
    public async Task TracksStatistics_WhenRequestsGoWrong_WithProvidedLoadBalancer()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new FakeLoadBalancer();

        var services = new ServiceCollection();
        services.AddSingleton(loadBalancer);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var handler = new DiscoveryHttpDelegatingHandler<FakeLoadBalancer>(serviceProvider)
        {
            InnerHandler = new TestInnerDelegatingHandlerBrokenServer()
        };

        using var invoker = new HttpMessageInvoker(handler);

        HttpResponseMessage result = await invoker.SendAsync(httpRequestMessage, default);

        loadBalancer.Statistics.Should().ContainSingle();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Headers.GetValues("requestUri").First().Should().Be("https://some-resolved-host:1234/api");
    }

    private sealed class TestInnerDelegatingHandler : DelegatingHandler
    {
        public bool IsCanceled { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (IsCanceled)
            {
                throw new OperationCanceledException();
            }

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            responseMessage.Headers.Add("requestUri", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(responseMessage);
        }
    }

    private sealed class TestInnerDelegatingHandlerBrokenServer : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            responseMessage.Headers.Add("requestUri", request.RequestUri?.AbsoluteUri);
            return Task.FromResult(responseMessage);
        }
    }
}
