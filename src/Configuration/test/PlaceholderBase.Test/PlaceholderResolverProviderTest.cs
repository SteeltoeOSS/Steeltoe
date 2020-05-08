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
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
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
            var settings = new Dictionary<string, string>()
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

            Assert.False(holder.TryGet("nokey", out var val));
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
            var settings = new Dictionary<string, string>()
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

            Assert.False(holder.TryGet("nokey", out var val));
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
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName, false, true);

            // Act and Assert
            var config = configurationBuilder.Build();

            var holder = new PlaceholderResolverProvider(new List<IConfigurationProvider>(config.Providers));
            var token = holder.GetReloadToken();
            Assert.NotNull(token);
            Assert.False(token.HasChanged);

            Assert.True(holder.TryGet("spring:cloud:config:name", out var val));
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
            var settings = new Dictionary<string, string>()
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
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName, false, true);

            // Act and Assert
            var config = configurationBuilder.Build();

            var holder = new PlaceholderResolverProvider(config);
            Assert.True(holder.TryGet("spring:cloud:config:name", out var val));
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
            var settings = new Dictionary<string, string>()
            {
                { "spring:bar:name", "myName" },
                { "spring:cloud:name", "${spring:bar:name?noname}" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            var providers = builder.Build().Providers.ToList();

            var holder = new PlaceholderResolverProvider(providers);
            var result = holder.GetChildKeys(Array.Empty<string>(), "spring");

            Assert.NotNull(result);
            var list = result.ToList();

            Assert.Equal(2, list.Count);
            Assert.Contains("bar", list);
            Assert.Contains("cloud", list);
        }
    }
}
