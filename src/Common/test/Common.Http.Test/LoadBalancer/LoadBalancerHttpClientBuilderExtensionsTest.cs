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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.LoadBalancer;
using System;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace Steeltoe.Common.Http.LoadBalancer.Test
{
    public class LoadBalancerHttpClientBuilderExtensionsTest
    {
        [Fact]
        public void AddRandomLoadBalancer_ThrowsIfBuilderNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => LoadBalancerHttpClientBuilderExtensions.AddRandomLoadBalancer(null));
            Assert.Equal("httpClientBuilder", exception.ParamName);
        }

        [Fact]
        public void AddRandomLoadBalancer_AddsRandomLoadBalancerToServices()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddConfigurationDiscoveryClient(new ConfigurationBuilder().Build());

            // act
            services.AddHttpClient("test").AddRandomLoadBalancer();
            var serviceProvider = services.BuildServiceProvider();
            var serviceEntryInCollection = services.FirstOrDefault(service => service.ServiceType.Equals(typeof(RandomLoadBalancer)));

            // assert
            Assert.Single(serviceProvider.GetServices<RandomLoadBalancer>());
            Assert.NotNull(serviceEntryInCollection);
            Assert.Equal(ServiceLifetime.Singleton, serviceEntryInCollection.Lifetime);
        }

        [Fact]
        public void AddRoundRobinLoadBalancer_ThrowsIfBuilderNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => LoadBalancerHttpClientBuilderExtensions.AddRoundRobinLoadBalancer(null));
            Assert.Equal("httpClientBuilder", exception.ParamName);
        }

        [Fact]
        public void AddRoundRobinLoadBalancer_AddsRoundRobinLoadBalancerToServices()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddConfigurationDiscoveryClient(new ConfigurationBuilder().Build());

            // act
            services.AddHttpClient("test").AddRoundRobinLoadBalancer();
            var serviceProvider = services.BuildServiceProvider();
            var serviceEntryInCollection = services.FirstOrDefault(service => service.ServiceType.Equals(typeof(RoundRobinLoadBalancer)));

            // assert
            Assert.Single(serviceProvider.GetServices<RoundRobinLoadBalancer>());
            Assert.Equal(ServiceLifetime.Singleton, serviceEntryInCollection.Lifetime);
        }

        [Fact]
        public void AddLoadBalancerT_ThrowsIfBuilderNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => LoadBalancerHttpClientBuilderExtensions.AddLoadBalancer<FakeLoadBalancer>(null));
            Assert.Equal("httpClientBuilder", exception.ParamName);
        }

        [Fact]
        public void AddLoadBalancerT_DoesntAddT_ToServices()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            services.AddHttpClient("test").AddLoadBalancer<FakeLoadBalancer>();
            var serviceProvider = services.BuildServiceProvider();

            // assert
            Assert.Empty(serviceProvider.GetServices<FakeLoadBalancer>());
        }

        [Fact]
        public void AddLoadBalancerT_CanBeUsedWithAnHttpClient()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddSingleton(typeof(FakeLoadBalancer));

            // act
            services.AddHttpClient("test").AddLoadBalancer<FakeLoadBalancer>();
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("test");

            // assert
            Assert.NotNull(client);
        }

        [Fact]
        public void CanAddMultipleLoadBalancers()
        {
            // arrange
            var services = new ServiceCollection();
            services.AddConfigurationDiscoveryClient(new ConfigurationBuilder().Build());
            services.AddSingleton(typeof(FakeLoadBalancer));

            // act
            services.AddHttpClient("testRandom").AddRandomLoadBalancer();
            services.AddHttpClient("testRandom2").AddRandomLoadBalancer();
            services.AddHttpClient("testRoundRobin").AddRoundRobinLoadBalancer();
            services.AddHttpClient("testRoundRobin2").AddRoundRobinLoadBalancer();
            services.AddHttpClient("testFake").AddLoadBalancer<FakeLoadBalancer>();
            services.AddHttpClient("testFake2").AddLoadBalancer<FakeLoadBalancer>();
            var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
            var randomLBClient = factory.CreateClient("testRandom");
            var randomLBClient2 = factory.CreateClient("testRandom2");
            var roundRobinLBClient = factory.CreateClient("testRoundRobin");
            var roundRobinLBClient2 = factory.CreateClient("testRoundRobin2");
            var fakeLBClient = factory.CreateClient("testFake");
            var fakeLBClient2 = factory.CreateClient("testFake2");

            // assert
            Assert.NotNull(randomLBClient);
            Assert.NotNull(randomLBClient2);
            Assert.NotNull(roundRobinLBClient);
            Assert.NotNull(roundRobinLBClient2);
            Assert.NotNull(fakeLBClient);
            Assert.NotNull(fakeLBClient2);
        }
    }
}
