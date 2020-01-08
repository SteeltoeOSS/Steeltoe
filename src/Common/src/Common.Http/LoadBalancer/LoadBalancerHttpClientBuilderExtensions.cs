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

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Http.LoadBalancer;
using Steeltoe.Common.LoadBalancer;
using Steeltoe.Discovery;
using System;
using System.Net.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LoadBalancerHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="DelegatingHandler"/> that performs random load balancing
        /// </summary>
        /// <param name="httpClientBuilder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <remarks>Requires an <see cref="IServiceInstanceProvider" /> or <see cref="IDiscoveryClient"/> in the DI container so the load balancer can sent traffic to more than one address</remarks>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder AddRandomLoadBalancer(this IHttpClientBuilder httpClientBuilder)
        {
            if (httpClientBuilder == null)
            {
                throw new ArgumentNullException(nameof(httpClientBuilder));
            }

            httpClientBuilder.Services.TryAddSingleton(typeof(RandomLoadBalancer));
            return httpClientBuilder.AddLoadBalancer<RandomLoadBalancer>();
        }

        /// <summary>
        /// Adds a <see cref="DelegatingHandler"/> that performs round robin load balancing, optionally backed by an <see cref="IDistributedCache"/>
        /// </summary>
        /// <param name="httpClientBuilder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <remarks>
        ///     Requires an <see cref="IServiceInstanceProvider" /> or <see cref="IDiscoveryClient"/> in the DI container so the load balancer can sent traffic to more than one address<para />
        ///     Also requires an <see cref="IDistributedCache"/> in the DI Container for consistent round robin balancing across multiple client instances
        /// </remarks>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder AddRoundRobinLoadBalancer(this IHttpClientBuilder httpClientBuilder)
        {
            if (httpClientBuilder == null)
            {
                throw new ArgumentNullException(nameof(httpClientBuilder));
            }

            httpClientBuilder.Services.TryAddSingleton(typeof(RoundRobinLoadBalancer));
            return httpClientBuilder.AddLoadBalancer<RoundRobinLoadBalancer>();
        }

        /// <summary>
        /// Adds an <see cref="HttpMessageHandler"/> with specified load balancer <para/>
        /// Does NOT add the specified load balancer to the container. Please add your load balancer separately.
        /// </summary>
        /// <param name="httpClientBuilder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <typeparam name="T">The type of <see cref="ILoadBalancer"/> to use</typeparam>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder AddLoadBalancer<T>(this IHttpClientBuilder httpClientBuilder)
            where T : ILoadBalancer
        {
            if (httpClientBuilder == null)
            {
                throw new ArgumentNullException(nameof(httpClientBuilder));
            }

            httpClientBuilder.Services.TryAddTransient<LoadBalancerDelegatingHandler>();
            httpClientBuilder.AddHttpMessageHandler((services) => new LoadBalancerDelegatingHandler(services.GetRequiredService<T>()));
            return httpClientBuilder;
        }
    }
}
