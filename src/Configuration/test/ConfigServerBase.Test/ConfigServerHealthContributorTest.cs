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
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.Placeholder;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class ConfigServerHealthContributorTest
    {
        [Fact]
        public void Constructor_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigServerHealthContributor(null));
        }

        [Fact]
        public void Constructor_FindsConfigServerProvider()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8888/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            builder.AddConfigServer();
            builder.AddPlaceholderResolver();

            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            Assert.NotNull(contributor.Provider);
        }

        [Fact]
        public void FindProvider_FindsProvider()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8888/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            builder.AddConfigServer();

            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            Assert.NotNull(contributor.FindProvider(config));
        }

        [Fact]
        public void GetTimeToLive_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8888/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:health:timeToLive", "100000" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            builder.AddConfigServer();

            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            Assert.Equal(100000, contributor.GetTimeToLive());
        }

        [Fact]
        public void IsEnabled_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8888/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:health:enabled", "true" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            builder.AddConfigServer();

            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            Assert.True(contributor.IsEnabled());
        }

        [Fact]
        public void IsCacheStale_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8888/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:health:enabled", "true" },
                { "spring:cloud:config:health:timeToLive", "1" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            builder.AddConfigServer();

            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            Assert.True(contributor.IsCacheStale(0)); // No cache established yet
            contributor.Cached = new ConfigEnvironment();
            contributor.LastAccess = 9;
            Assert.True(contributor.IsCacheStale(10));
            Assert.False(contributor.IsCacheStale(8));
        }

        [Fact]
        public void GetPropertySources_ReturnsExpected()
        {
            // this test does NOT expect to find a running config server
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8887/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:health:enabled", "true" },
                { "spring:cloud:config:health:timeToLive", "1" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            builder.AddConfigServer();

            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            contributor.Cached = new ConfigEnvironment();
            var lastAccess = contributor.LastAccess = DateTimeOffset.Now.ToUnixTimeMilliseconds() - 100;
            var sources = contributor.GetPropertySources();

            Assert.NotEqual(lastAccess, contributor.LastAccess);
            Assert.Null(sources);
            Assert.Null(contributor.Cached);
        }

        [Fact]
        public void Health_NoProvider_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8888/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:health:enabled", "true" },
                { "spring:cloud:config:health:timeToLive", "1" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            Assert.Null(contributor.Provider);
            var health = contributor.Health();
            Assert.NotNull(health);
            Assert.Equal(HealthStatus.UNKNOWN, health.Status);
            Assert.True(health.Details.ContainsKey("error"));
        }

        [Fact]
        public void Health_NotEnabled_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8888/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:health:enabled", "false" },
                { "spring:cloud:config:health:timeToLive", "1" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            builder.AddConfigServer();
            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            Assert.NotNull(contributor.Provider);
            var health = contributor.Health();
            Assert.NotNull(health);
            Assert.Equal(HealthStatus.UNKNOWN, health.Status);
        }

        [Fact]
        public void Health_NoPropertySources_ReturnsExpected()
        {
            // this test does NOT expect to find a running config server
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8887/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:health:enabled", "true" },
                { "spring:cloud:config:health:timeToLive", "1" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            builder.AddConfigServer();
            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            Assert.NotNull(contributor.Provider);
            var health = contributor.Health();
            Assert.NotNull(health);
            Assert.Equal(HealthStatus.UNKNOWN, health.Status);
            Assert.True(health.Details.ContainsKey("error"));
        }

        [Fact]
        public void UpdateHealth_WithPropertySources_ReturnsExpected()
        {
            var values = new Dictionary<string, string>()
            {
                { "spring:cloud:config:uri", "http://localhost:8888/" },
                { "spring:cloud:config:name", "myName" },
                { "spring:cloud:config:label", "myLabel" },
                { "spring:cloud:config:health:enabled", "true" },
                { "spring:cloud:config:health:timeToLive", "1" },
                { "spring:cloud:config:timeout", "10" },
            };
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(values);
            builder.AddConfigServer();
            var config = builder.Build();

            var contributor = new ConfigServerHealthContributor(config);
            var health = new HealthCheckResult();
            var sources = new List<PropertySource>()
            {
                new PropertySource("foo", new Dictionary<string, object>()),
                new PropertySource("bar", new Dictionary<string, object>()),
            };
            contributor.UpdateHealth(health, sources);

            Assert.Equal(HealthStatus.UP, health.Status);
            Assert.True(health.Details.ContainsKey("propertySources"));
            var names = health.Details["propertySources"] as IList<string>;
            Assert.NotNull(names);
            Assert.Contains("foo", names);
            Assert.Contains("bar", names);
        }
    }
}
