// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

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

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        var handler = new DiscoveryHttpDelegatingHandler<FakeLoadBalancer>(serviceProvider)
        {
            InnerHandler = new TestInnerDelegatingHandler()
        };

        using var invoker = new HttpMessageInvoker(handler);

        HttpResponseMessage result = await invoker.SendAsync(httpRequestMessage, default);

        result.Headers.GetValues("requestUri").First().Should().Be("https://some-resolved-host:1234/api");
        loadBalancer.Statistics.Should().HaveCount(1);
    }

    [Fact]
    public async Task DoesNotTrackStatistics_WhenRequestIsCanceled()
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://replace-me/api"));
        var loadBalancer = new FakeLoadBalancer();

        var services = new ServiceCollection();
        services.AddSingleton(loadBalancer);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

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

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

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

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        var handler = new DiscoveryHttpDelegatingHandler<FakeLoadBalancer>(serviceProvider)
        {
            InnerHandler = new TestInnerDelegatingHandlerBrokenServer()
        };

        using var invoker = new HttpMessageInvoker(handler);

        HttpResponseMessage result = await invoker.SendAsync(httpRequestMessage, default);

        loadBalancer.Statistics.Should().HaveCount(1);
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

    /// <summary>
    /// A load balancer that always throws an exception.
    /// </summary>
    private sealed class BrokenLoadBalancer : ILoadBalancer
    {
        internal IList<LoadBalancerStatistic> Statistics { get; } = [];

        /// <summary>
        /// Throws an exception when you try to resolve service instances.
        /// </summary>
        public Task<Uri> ResolveServiceInstanceAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            throw new DataException("(╯°□°）╯︵ ┻━┻");
        }

        public Task UpdateStatisticsAsync(Uri requestUri, Uri serviceInstanceUri, TimeSpan? responseTime, Exception? exception,
            CancellationToken cancellationToken)
        {
            ArgumentGuard.NotNull(requestUri);
            ArgumentGuard.NotNull(serviceInstanceUri);

            Statistics.Add(new LoadBalancerStatistic(requestUri, serviceInstanceUri, responseTime, exception));
            return Task.CompletedTask;
        }
    }
}
