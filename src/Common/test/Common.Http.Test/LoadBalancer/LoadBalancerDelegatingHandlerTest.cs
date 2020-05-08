// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            var result = await invoker.SendAsync(httpRequestMessage, default);

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
            var result = await Assert.ThrowsAsync<Exception>(async () => await invoker.SendAsync(httpRequestMessage, default));

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
            var result = await invoker.SendAsync(httpRequestMessage, default);

            // assert
            Assert.Single(loadBalancer.Stats);
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.Equal("https://someresolvedhost/api", result.Headers.GetValues("requestUri").First());
        }
    }
}
