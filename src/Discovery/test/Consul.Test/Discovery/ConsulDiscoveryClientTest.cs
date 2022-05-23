// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Moq;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ConsulDiscoveryClientTest
    {
        [Fact]
        public void Constructor_ThrowsIfNulls()
        {
            var clientMoq = new Mock<IConsulClient>();
            Assert.Throws<ArgumentNullException>(() => new ConsulDiscoveryClient(null, new ConsulDiscoveryOptions()));
            Assert.Throws<ArgumentNullException>(() => new ConsulDiscoveryClient(clientMoq.Object, (ConsulDiscoveryOptions)null));
        }

        [Fact]
        public async Task AddInstancesToListAsync_AddsExpected()
        {
            var options = new ConsulDiscoveryOptions();

            var queryResult = new QueryResult<ServiceEntry[]>()
            {
                Response = new[]
                {
                    new ServiceEntry()
                    {
                        Service = new AgentService()
                        {
                            Service = "ServiceId",
                            Address = "foo.bar.com",
                            Port = 1234,
                            Tags = new[] { "foo=bar", "secure=true" }
                        }
                    },
                    new ServiceEntry()
                    {
                        Service = new AgentService()
                        {
                            Service = "ServiceId",
                            Address = "foo1.bar1.com",
                            Port = 5678,
                            Tags = new[] { "bar=foo", "secure=false" }
                        }
                    }
                }
            };

            var result = Task.FromResult(queryResult);
            var clientMoq = new Mock<IConsulClient>();
            var healthMoq = new Mock<IHealthEndpoint>();
            clientMoq.Setup(c => c.Health).Returns(healthMoq.Object);
            healthMoq.Setup(h => h.Service("ServiceId", options.DefaultQueryTag, options.QueryPassing, QueryOptions.Default, default)).Returns(result);

            var dc = new ConsulDiscoveryClient(clientMoq.Object, options);
            var list = new List<IServiceInstance>();
            await dc.AddInstancesToListAsync(list, "ServiceId", QueryOptions.Default);
            Assert.Equal(2, list.Count);

            var inst = list[0];
            Assert.Equal("foo.bar.com", inst.Host);
            Assert.Equal("ServiceId", inst.ServiceId);
            Assert.True(inst.IsSecure);
            Assert.Equal(1234, inst.Port);
            Assert.Equal(2, inst.Metadata.Count);
            Assert.Contains("foo", inst.Metadata.Keys);
            Assert.Contains("secure", inst.Metadata.Keys);
            Assert.Contains("bar", inst.Metadata.Values);
            Assert.Contains("true", inst.Metadata.Values);
            Assert.Equal(new Uri("https://foo.bar.com:1234"), inst.Uri);

            inst = list[1];
            Assert.Equal("foo1.bar1.com", inst.Host);
            Assert.Equal("ServiceId", inst.ServiceId);
            Assert.False(inst.IsSecure);
            Assert.Equal(5678, inst.Port);
            Assert.Equal(2, inst.Metadata.Count);
            Assert.Contains("bar", inst.Metadata.Keys);
            Assert.Contains("secure", inst.Metadata.Keys);
            Assert.Contains("foo", inst.Metadata.Values);
            Assert.Contains("false", inst.Metadata.Values);
            Assert.Equal(new Uri("http://foo1.bar1.com:5678"), inst.Uri);
        }

        [Fact]
        public async Task GetServicesAsync_ReturnsExpected()
        {
            var options = new ConsulDiscoveryOptions();

            var queryResult = new QueryResult<Dictionary<string, string[]>>()
            {
                Response = new Dictionary<string, string[]>
                {
                    { "foo", new[] { "I1", "I2" } },
                    { "bar", new[] { "I1", "I2" } },
                }
            };
            var result = Task.FromResult(queryResult);
            var clientMoq = new Mock<IConsulClient>();
            var catMoq = new Mock<ICatalogEndpoint>();
            clientMoq.Setup(c => c.Catalog).Returns(catMoq.Object);
            catMoq.Setup(c => c.Services(QueryOptions.Default, default)).Returns(result);

            var dc = new ConsulDiscoveryClient(clientMoq.Object, options);
            var services = await dc.GetServicesAsync();
            Assert.Equal(2, services.Count);
            Assert.Contains("foo", services);
            Assert.Contains("bar", services);
        }

        [Fact]
        public void GetServices_ReturnsExpected()
        {
            var options = new ConsulDiscoveryOptions();

            var queryResult = new QueryResult<Dictionary<string, string[]>>()
            {
                Response = new Dictionary<string, string[]>
                {
                    { "foo", new[] { "I1", "I2" } },
                    { "bar", new[] { "I1", "I2" } },
                }
            };
            var result = Task.FromResult(queryResult);
            var clientMoq = new Mock<IConsulClient>();
            var catMoq = new Mock<ICatalogEndpoint>();
            clientMoq.Setup(c => c.Catalog).Returns(catMoq.Object);
            catMoq.Setup(c => c.Services(QueryOptions.Default, default)).Returns(result);

            var dc = new ConsulDiscoveryClient(clientMoq.Object, options);
            var services = dc.GetServices();
            Assert.Equal(2, services.Count);
            Assert.Contains("foo", services);
            Assert.Contains("bar", services);
        }

        [Fact]
        public void GetAllInstances_ReturnsExpected()
        {
            var options = new ConsulDiscoveryOptions();

            var queryResult1 = new QueryResult<Dictionary<string, string[]>>()
            {
                Response = new Dictionary<string, string[]>
                {
                    { "ServiceId", new[] { "I1", "I2" } },
                }
            };
            var result1 = Task.FromResult(queryResult1);

            var queryResult2 = new QueryResult<ServiceEntry[]>()
            {
                Response = new[]
                {
                    new ServiceEntry()
                    {
                        Service = new AgentService()
                        {
                            Service = "ServiceId",
                            Address = "foo.bar.com",
                            Port = 1234,
                            Tags = new[] { "foo=bar", "secure=true" }
                        }
                    },
                    new ServiceEntry()
                    {
                        Service = new AgentService()
                        {
                            Service = "ServiceId",
                            Address = "foo1.bar1.com",
                            Port = 5678,
                            Tags = new[] { "bar=foo", "secure=false" }
                        }
                    }
                }
            };
            var result2 = Task.FromResult(queryResult2);

            var clientMoq = new Mock<IConsulClient>();
            var catMoq = new Mock<ICatalogEndpoint>();
            clientMoq.Setup(c => c.Catalog).Returns(catMoq.Object);
            catMoq.Setup(c => c.Services(QueryOptions.Default, default)).Returns(result1);

            var healthMoq = new Mock<IHealthEndpoint>();
            clientMoq.Setup(c => c.Health).Returns(healthMoq.Object);
            healthMoq.Setup(h => h.Service("ServiceId", options.DefaultQueryTag, options.QueryPassing, QueryOptions.Default, default)).Returns(result2);

            var dc = new ConsulDiscoveryClient(clientMoq.Object, options);
            var list = dc.GetAllInstances(QueryOptions.Default);

            Assert.Equal(2, list.Count);

            var inst = list[0];
            Assert.Equal("foo.bar.com", inst.Host);
            Assert.Equal("ServiceId", inst.ServiceId);
            Assert.True(inst.IsSecure);
            Assert.Equal(1234, inst.Port);
            Assert.Equal(2, inst.Metadata.Count);
            Assert.Contains("foo", inst.Metadata.Keys);
            Assert.Contains("secure", inst.Metadata.Keys);
            Assert.Contains("bar", inst.Metadata.Values);
            Assert.Contains("true", inst.Metadata.Values);
            Assert.Equal(new Uri("https://foo.bar.com:1234"), inst.Uri);

            inst = list[1];
            Assert.Equal("foo1.bar1.com", inst.Host);
            Assert.Equal("ServiceId", inst.ServiceId);
            Assert.False(inst.IsSecure);
            Assert.Equal(5678, inst.Port);
            Assert.Equal(2, inst.Metadata.Count);
            Assert.Contains("bar", inst.Metadata.Keys);
            Assert.Contains("secure", inst.Metadata.Keys);
            Assert.Contains("foo", inst.Metadata.Values);
            Assert.Contains("false", inst.Metadata.Values);
            Assert.Equal(new Uri("http://foo1.bar1.com:5678"), inst.Uri);
        }
    }
}
