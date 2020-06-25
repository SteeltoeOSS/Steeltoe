﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Placeholder.Test
{
    public class PlaceholderResolverProviderTest
    {
        [Fact]
        public void Constructor__ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration configuration = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new PlaceholderResolverProvider(configuration));
        }

        [Fact]
        public void Constructor__ThrowsIfListIConfigurationProviderNull()
        {
            // Arrange
            IList<IConfigurationProvider> providers = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new PlaceholderResolverProvider(providers));
        }

        [Fact]
        public void Constructor_WithConfiguration()
        {
            var holder = new PlaceholderResolverProvider(new ConfigurationBuilder().Build());
            Assert.NotNull(holder.Configuration);
            Assert.Empty(holder._providers);
        }

        [Fact]
        public void Constructor_WithListIConfigurationProvider()
        {
            var providers = new List<IConfigurationProvider>();
            var holder = new PlaceholderResolverProvider(providers);
            Assert.Null(holder.Configuration);
            Assert.Same(providers, holder._providers);
        }

        [Fact]
        public void Constructor_WithLoggerFactory()
        {
            var loggerFactory = new LoggerFactory();

            var holder = new PlaceholderResolverProvider(new List<IConfigurationProvider>(), loggerFactory);
            Assert.NotNull(holder._logger);

            holder = new PlaceholderResolverProvider(new ConfigurationBuilder().Build(), loggerFactory);
            Assert.NotNull(holder._logger);
        }

        [Fact]
        public void TryGet_ReturnsResolvedValues()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "${key1?notfound}" },
                { "key3", "${nokey?notfound}" },
                { "key4", "${nokey}" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            var providers = builder.Build().Providers.ToList();

            var holder = new PlaceholderResolverProvider(providers);

            Assert.False(holder.TryGet("nokey", out string val));
            Assert.True(holder.TryGet("key1", out val));
            Assert.Equal("value1", val);
            Assert.True(holder.TryGet("key2", out val));
            Assert.Equal("value1", val);
            Assert.True(holder.TryGet("key3", out val));
            Assert.Equal("notfound", val);
            Assert.True(holder.TryGet("key4", out val));
            Assert.Equal("${nokey}", val);
        }

        [Fact]
        public void Set_SetsValues_ReturnsResolvedValues()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "${key1?notfound}" },
                { "key3", "${nokey?notfound}" },
                { "key4", "${nokey}" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            var providers = builder.Build().Providers.ToList();

            var holder = new PlaceholderResolverProvider(providers);

            Assert.False(holder.TryGet("nokey", out string val));
            Assert.True(holder.TryGet("key1", out val));
            Assert.Equal("value1", val);
            Assert.True(holder.TryGet("key2", out val));
            Assert.Equal("value1", val);
            Assert.True(holder.TryGet("key3", out val));
            Assert.Equal("notfound", val);
            Assert.True(holder.TryGet("key4", out val));
            Assert.Equal("${nokey}", val);

            holder.Set("nokey", "nokeyvalue");
            Assert.True(holder.TryGet("key3", out val));
            Assert.Equal("nokeyvalue", val);
            Assert.True(holder.TryGet("key4", out val));
            Assert.Equal("nokeyvalue", val);
        }

        [Fact]
        public void GetReloadToken_ReturnsExpected_NotifyChanges()
        {
            // Arrange
            var appsettings1 = @"
                {
                    ""spring"": {
                        ""bar"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:bar:name?noname}"",
                        }
                      }
                    }
                }";

            var appsettings2 = @"
                {
                    ""spring"": {
                        ""bar"": {
                            ""name"": ""newMyName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:bar:name?noname}"",
                        }
                      }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appsettings1);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName, false, true);

            // Act and Assert
            var config = configurationBuilder.Build();

            var holder = new PlaceholderResolverProvider(new List<IConfigurationProvider>(config.Providers));
            var token = holder.GetReloadToken();
            Assert.NotNull(token);
            Assert.False(token.HasChanged);

            Assert.True(holder.TryGet("spring:cloud:config:name", out string val));
            Assert.Equal("myName", val);

            File.WriteAllText(path, appsettings2);

            // There is a 250ms delay to detect change
            // ASP.NET Core tests use 2000 Sleep for this kind of test
            Thread.Sleep(2000);

            Assert.True(token.HasChanged);
            Assert.True(holder.TryGet("spring:cloud:config:name", out val));
            Assert.Equal("newMyName", val);
        }

        [Fact]
        public void Load_CreatesConfiguration()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "${key1?notfound}" },
                { "key3", "${nokey?notfound}" },
                { "key4", "${nokey}" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            var providers = builder.Build().Providers.ToList();

            var holder = new PlaceholderResolverProvider(providers);
            Assert.Null(holder.Configuration);
            holder.Load();
            Assert.NotNull(holder.Configuration);
            Assert.Equal("value1", holder.Configuration["key1"]);
        }

        [Fact]
        public void Load_ReloadsConfiguration()
        {
            // Arrange
            var appsettings1 = @"
                {
                    ""spring"": {
                        ""bar"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:bar:name?noname}"",
                        }
                      }
                    }
                }";

            var appsettings2 = @"
                {
                    ""spring"": {
                        ""bar"": {
                            ""name"": ""newMyName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:bar:name?noname}"",
                        }
                      }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appsettings1);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName, false, true);

            // Act and Assert
            var config = configurationBuilder.Build();

            var holder = new PlaceholderResolverProvider(config);
            Assert.True(holder.TryGet("spring:cloud:config:name", out string val));
            Assert.Equal("myName", val);

            File.WriteAllText(path, appsettings2);
            Thread.Sleep(1000);  // There is a 250ms delay

            holder.Load();

            Assert.True(holder.TryGet("spring:cloud:config:name", out val));
            Assert.Equal("newMyName", val);
        }

        [Fact]
        public void GetChildKeys_ReturnsResolvableSection()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>()
            {
                { "spring:bar:name", "myName" },
                { "spring:cloud:name", "${spring:bar:name?noname}" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            var providers = builder.Build().Providers.ToList();

            var holder = new PlaceholderResolverProvider(providers);
            var result = holder.GetChildKeys(new string[0], "spring");

            Assert.NotNull(result);
            var list = result.ToList();

            Assert.Equal(2, list.Count);
            Assert.Contains("bar", list);
            Assert.Contains("cloud", list);
        }
    }
}
