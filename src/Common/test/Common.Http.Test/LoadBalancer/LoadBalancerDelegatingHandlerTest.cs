﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Http.Test;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace Steeltoe.Common.Http.LoadBalancer.Test
{
    public class LoadBalancerDelegatingHandlerTest
    {
        [Fact]
        public void Throws_If_LoadBalancerNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new LoadBalancerDelegatingHandler(null));
            Assert.Equal("loadBalancer", exception.ParamName);
        }

        [Fact]
        public async void ResolvesUri_TracksStats_WithProvidedLoadBalancer()
        {
            // arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://replaceme/api");
            var loadBalancer = new FakeLoadBalancer();
            var handler = new LoadBalancerDelegatingHandler(loadBalancer) { InnerHandler = new TestInnerDelegatingHandler() };
            var invoker = new HttpMessageInvoker(handler);

            // act
            var result = await invoker.SendAsync(httpRequestMessage, default(CancellationToken));

            // assert
            Assert.Equal("https://someresolvedhost/api", result.Headers.GetValues("requestUri").First());
            Assert.Single(loadBalancer.Stats);
        }

        [Fact]
        public async void DoesntTrackStats_WhenResolutionFails_WithProvidedLoadBalancer()
        {
            // arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://replaceme/api");
            var loadBalancer = new BrokenLoadBalancer();
            var handler = new LoadBalancerDelegatingHandler(loadBalancer) { InnerHandler = new TestInnerDelegatingHandler() };
            var invoker = new HttpMessageInvoker(handler);

            // act
            var result = await Assert.ThrowsAsync<Exception>(async () => await invoker.SendAsync(httpRequestMessage, default(CancellationToken)));

            // assert
            Assert.Empty(loadBalancer.Stats);
        }

        [Fact]
        public async void TracksStats_WhenRequestsGoWrong_WithProvidedLoadBalancer()
        {
            // arrange
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://replaceme/api");
            var loadBalancer = new FakeLoadBalancer();
            var handler = new LoadBalancerDelegatingHandler(loadBalancer) { InnerHandler = new TestInnerDelegatingHandlerBrokenServer() };
            var invoker = new HttpMessageInvoker(handler);

            // act
            var result = await invoker.SendAsync(httpRequestMessage, default(CancellationToken));

            // assert
            Assert.Single(loadBalancer.Stats);
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.Equal("https://someresolvedhost/api", result.Headers.GetValues("requestUri").First());
        }
    }
}
