// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Moq;
using Steeltoe.Common.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ConsulDiscoveryOptionsTest
    {
        [Fact]
        public void Constructor_InitsDefaults()
        {
            var opts = new ConsulDiscoveryOptions();
            Assert.True(opts.Register);
            Assert.True(opts.RegisterHealthCheck);
            Assert.Null(opts.DefaultQueryTag);
            Assert.Equal("zone", opts.DefaultZoneMetadataName);
            Assert.True(opts.Deregister);
            Assert.True(opts.Enabled);
            Assert.True(opts.FailFast);
            Assert.Equal("30m", opts.HealthCheckCriticalTimeout);
            Assert.Equal("10s", opts.HealthCheckInterval);
            Assert.Equal("/actuator/health", opts.HealthCheckPath);
            Assert.Equal("10s", opts.HealthCheckTimeout);
            Assert.False(opts.HealthCheckTlsSkipVerify);
            Assert.Null(opts.HealthCheckUrl);
            Assert.NotNull(opts.Heartbeat);
            Assert.NotNull(opts.HostName);
            Assert.Null(opts.InstanceGroup);
            Assert.Null(opts.InstanceZone);
            Assert.False(opts.PreferIpAddress);
            Assert.False(opts.PreferAgentAddress);
            Assert.False(opts.QueryPassing);
            Assert.Equal("http", opts.Scheme);
            Assert.Null(opts.ServiceName);
            Assert.Null(opts.Tags);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO: this is null on MacOS
                Assert.NotNull(opts.IpAddress);
            }
        }

        [Fact]
        public void Options_DontUseInetUtilsByDefault()
        {
            // arrange
            var mockNetUtils = new Mock<InetUtils>(null, null);
            mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo() { Hostname = "FromMock", IpAddress = "254.254.254.254" }).Verifiable();
            var config = new ConfigurationBuilder().Build();
            var opts = new ConsulDiscoveryOptions() { NetUtils = mockNetUtils.Object };
            config.GetSection(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX).Bind(opts);

            // act
            opts.ApplyNetUtils();

            // assert
            mockNetUtils.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Never);
        }

        [Fact]
        public void Options_CanUseInetUtils()
        {
            // arrange
            var mockNetUtils = new Mock<InetUtils>(null, null);
            mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo() { Hostname = "FromMock", IpAddress = "254.254.254.254" }).Verifiable();
            var appSettings = new Dictionary<string, string> { { "consul:discovery:UseNetUtils", "true" } };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
            var opts = new ConsulDiscoveryOptions() { NetUtils = mockNetUtils.Object };
            config.GetSection(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX).Bind(opts);

            // act
            opts.ApplyNetUtils();

            // assert
            Assert.Equal("FromMock", opts.HostName);
            Assert.Equal("254.254.254.254", opts.IpAddress);
            mockNetUtils.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Once);
        }

        [Fact]
        [Trait("Category", "SkipOnMacOS")] // for some reason this takes 25-ish seconds on the MSFT-hosted MacOS agent
        public void Options_CanUseInetUtilsWithoutReverseDnsOnIP()
        {
            // arrange
            var appSettings = new Dictionary<string, string> { { "consul:discovery:UseNetUtils", "true" }, { "spring:cloud:inet:SkipReverseDnsLookup", "true" } };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
            var opts = new ConsulDiscoveryOptions() { NetUtils = new InetUtils(config.GetSection(InetOptions.PREFIX).Get<InetOptions>()) };
            config.GetSection(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX).Bind(opts);

            // act
            var noSlowReverseDNSQuery = new Stopwatch();
            noSlowReverseDNSQuery.Start();
            opts.ApplyNetUtils();
            noSlowReverseDNSQuery.Stop();

            // assert
            Assert.NotNull(opts.HostName);
            Assert.InRange(noSlowReverseDNSQuery.ElapsedMilliseconds, 0, 1500); // testing with an actual reverse dns query results in around 5000 ms
        }
    }
}
