﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Configuration;
using Steeltoe.Consul.Util;
using Steeltoe.Discovery.Consul.Discovery;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Discovery.Consul.Registry.Test
{
    public class ConsulRegistrationTest
    {
        [Fact]
        public void Construtor_ThrowsOnNulls()
        {
            var areg = new AgentServiceRegistration();
            var options = new ConsulDiscoveryOptions();

            Assert.Throws<ArgumentNullException>(() => new ConsulRegistration(null, options));
            Assert.Throws<ArgumentNullException>(() => new ConsulRegistration(areg, (ConsulDiscoveryOptions)null));
        }

        [Fact]
        public void Constructor_SetsProperties()
        {
            var areg = new AgentServiceRegistration()
            {
                ID = "id",
                Name = "name",
                Address = "address",
                Port = 1234,
                Tags = new string[] { "foo=bar" }
            };

            var options = new ConsulDiscoveryOptions();
            var reg = new ConsulRegistration(areg, options);
            Assert.Equal("id", reg.InstanceId);
            Assert.Equal("name", reg.ServiceId);
            Assert.Equal("address", reg.Host);
            Assert.Equal(1234, reg.Port);
            Assert.Single(reg.Metadata);
            Assert.Contains("foo", reg.Metadata.Keys);
            Assert.Contains("bar", reg.Metadata.Values);
            Assert.False(reg.IsSecure);
            Assert.Equal(new Uri("http://address:1234"), reg.Uri);
        }

        [Fact]
        public void CreateTags_ReturnsExpected()
        {
            var options = new ConsulDiscoveryOptions()
            {
                Tags = new List<string>() { "foo=bar" },
                InstanceZone = "instancezone",
                InstanceGroup = "instancegroup",
                Scheme = "https"
            };
            var result = ConsulRegistration.CreateTags(options);
            Assert.Equal(4, result.Length);
            Assert.Contains("foo=bar", result);
            Assert.Contains("zone=instancezone", result);
            Assert.Contains("group=instancegroup", result);
            Assert.Contains("secure=true", result);
        }

        [Fact]
        public void GetAppName_ReturnsExpected()
        {
            var options = new ConsulDiscoveryOptions()
            {
                ServiceName = "serviceName"
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "spring:application:name", "foobar" }
                })
                .Build();

            var result = ConsulRegistration.GetAppName(options, config);
            Assert.Equal("serviceName", result);

            options.ServiceName = null;
            result = ConsulRegistration.GetAppName(options, config);
            Assert.Equal("foobar", result);

            config = new ConfigurationBuilder().Build();
            result = ConsulRegistration.GetAppName(options, config);
            Assert.Equal("application", result);
        }

        [Fact]
        public void Tags_MapTo_Metadata()
        {
            // arrange some tags in configuration
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "consul:discovery:tags:0", "key0=value0" },
                    { "consul:discovery:tags:1", "key1=value1" },
                    { "consul:discovery:tags:2", "keyvalue" }
                })
                .Build();

            // bind to options
            var options = new ConsulDiscoveryOptions();
            config.Bind(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX, options);
            var tags = ConsulRegistration.CreateTags(options);

            // act - get metadata from tags
            var result = ConsulServerUtils.GetMetadata(tags);
            Assert.Contains(result, k => k.Key == "key0");
            Assert.Equal("value0", result["key0"]);
            Assert.Contains(result, k => k.Key == "key1");
            Assert.Equal("value1", result["key1"]);
            Assert.Contains(result, k => k.Key == "keyvalue");
            Assert.Equal("keyvalue", result["keyvalue"]);
        }

        [Fact]
        public void GetDefaultInstanceId_ReturnsExpected()
        {
            var options = new ConsulDiscoveryOptions()
            {
                ServiceName = "serviceName"
            };
            var config = new ConfigurationBuilder().Build();
            var result = ConsulRegistration.GetDefaultInstanceId(options, config);
            Assert.StartsWith("serviceName:", result);

            config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "vcap:application:instance_id", "vcapid" }
                })
                .Build();
            result = ConsulRegistration.GetDefaultInstanceId(options, config);
            Assert.Equal("serviceName:vcapid", result);

            config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "spring:application:instance_id", "springid" }
                })
                .Build();
            result = ConsulRegistration.GetDefaultInstanceId(options, config);
            Assert.Equal("serviceName:springid", result);
        }

        [Fact]
        public void GetInstanceId_ReturnsExpected()
        {
            var options = new ConsulDiscoveryOptions()
            {
                InstanceId = "instanceId"
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "spring:application:name", "foobar" }
                })
                .Build();

            var result = ConsulRegistration.GetInstanceId(options, config);
            Assert.Equal("instanceId", result);

            options.InstanceId = null;

            result = ConsulRegistration.GetInstanceId(options, config);
            Assert.StartsWith("foobar-", result);
        }

        [Fact]
        public void NormalizeForConsul_ReturnsExpected()
        {
            Assert.Equal("abc1", ConsulRegistration.NormalizeForConsul("abc1"));
            Assert.Equal("ab-c1", ConsulRegistration.NormalizeForConsul("ab:c1"));
            Assert.Equal("ab-c1", ConsulRegistration.NormalizeForConsul("ab::c1"));

            Assert.Throws<ArgumentException>(() => ConsulRegistration.NormalizeForConsul("9abc"));
            Assert.Throws<ArgumentException>(() => ConsulRegistration.NormalizeForConsul(":abc"));
            Assert.Throws<ArgumentException>(() => ConsulRegistration.NormalizeForConsul("abc:"));
        }

        [Fact]
        public void CreateCheck_ReturnsExpected()
        {
            var options = new ConsulDiscoveryOptions();
            var result = ConsulRegistration.CreateCheck(1234, options);
            Assert.NotNull(result);
            var expectedTtl = DateTimeConversions.ToTimeSpan(options.Heartbeat.Ttl);
            Assert.Equal(result.TTL, expectedTtl);

            options.Heartbeat = null;
            Assert.Throws<ArgumentException>(() => ConsulRegistration.CreateCheck(0, options));

            var port = 1234;
            result = ConsulRegistration.CreateCheck(port, options);
            var uri = new Uri($"{options.Scheme}://{options.HostName}:{port}{options.HealthCheckPath}");
            Assert.Equal(uri.ToString(), result.HTTP);
            Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckInterval), result.Interval);
            Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout), result.Timeout);
            Assert.Equal(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout), result.DeregisterCriticalServiceAfter);
            Assert.Equal(options.HealthCheckTlsSkipVerify, result.TLSSkipVerify);
        }

        [Fact]
        public void CreateRegistration_ReturnsExpected()
        {
            var options = new ConsulDiscoveryOptions()
            {
                Port = 1100
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "spring:application:name", "foobar" }
                })
                .Build();

            var reg = ConsulRegistration.CreateRegistration(config, options);

            Assert.StartsWith("foobar-", reg.InstanceId);
            Assert.False(reg.IsSecure);
            Assert.Equal("foobar", reg.ServiceId);
            Assert.Equal(options.HostName, reg.Host);
            Assert.Equal(1100, reg.Port);
            var hostName = options.HostName;
            Assert.Equal(new Uri($"http://{hostName}:1100"), reg.Uri);
            Assert.NotNull(reg.Service);

            Assert.Equal(hostName, reg.Service.Address);
            Assert.StartsWith("foobar-", reg.Service.ID);
            Assert.Equal("foobar", reg.Service.Name);
            Assert.Equal(1100, reg.Service.Port);
            Assert.NotNull(reg.Service.Check);
            Assert.NotNull(reg.Service.Tags);
        }
    }
}
