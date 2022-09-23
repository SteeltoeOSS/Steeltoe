// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Placeholder.Test;

public sealed class PlaceholderResolverProviderTest
{
    [Fact]
    public void Constructor_WithConfiguration_ThrowsIfNulls()
    {
        const IConfigurationRoot nullConfiguration = null;
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => new PlaceholderResolverProvider(nullConfiguration, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => new PlaceholderResolverProvider(configuration, null));
    }

    [Fact]
    public void Constructor_WithProviders_ThrowsIfNulls()
    {
        const IList<IConfigurationProvider> nullProviders = null;
        var providers = new List<IConfigurationProvider>();
        var loggerFactory = NullLoggerFactory.Instance;

        Assert.Throws<ArgumentNullException>(() => new PlaceholderResolverProvider(nullProviders, loggerFactory));
        Assert.Throws<ArgumentNullException>(() => new PlaceholderResolverProvider(providers, null));
    }

    [Fact]
    public void Constructor_WithConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        var provider = new PlaceholderResolverProvider(configuration, NullLoggerFactory.Instance);

        Assert.NotNull(provider.Configuration);
        Assert.Empty(provider.Providers);
    }

    [Fact]
    public void Constructor_WithProviders()
    {
        var providers = new List<IConfigurationProvider>();

        var provider = new PlaceholderResolverProvider(providers, NullLoggerFactory.Instance);

        Assert.Null(provider.Configuration);
        Assert.Same(providers, provider.Providers);
    }

    [Fact]
    public void Constructor_WithLoggerFactory()
    {
        var providers = new List<IConfigurationProvider>();
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        var loggerFactory = new LoggerFactory();

        var provider = new PlaceholderResolverProvider(providers, loggerFactory);
        Assert.NotNull(provider.Logger);

        provider = new PlaceholderResolverProvider(configuration, loggerFactory);
        Assert.NotNull(provider.Logger);
    }

    [Fact]
    public void TryGet_ReturnsResolvedValues()
    {
        var settings = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

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
        var settings = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

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
        const string appsettings1 = @"
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

        const string appsettings2 = @"
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

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings1);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName, false, true);

        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var holder = new PlaceholderResolverProvider(new List<IConfigurationProvider>(configurationRoot.Providers));
        IChangeToken token = holder.GetReloadToken();
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
        var settings = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

        var holder = new PlaceholderResolverProvider(providers);
        Assert.Null(holder.Configuration);
        holder.Load();
        Assert.NotNull(holder.Configuration);
        Assert.Equal("value1", holder.Configuration["key1"]);
    }

    [Fact]
    public void Load_ReloadsConfiguration()
    {
        const string appsettings1 = @"
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

        const string appsettings2 = @"
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

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings1);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName, false, true);

        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var holder = new PlaceholderResolverProvider(configurationRoot);
        Assert.True(holder.TryGet("spring:cloud:config:name", out string val));
        Assert.Equal("myName", val);

        File.WriteAllText(path, appsettings2);
        Thread.Sleep(1000); // There is a 250ms delay

        holder.Load();

        Assert.True(holder.TryGet("spring:cloud:config:name", out val));
        Assert.Equal("newMyName", val);
    }

    [Fact]
    public void GetChildKeys_ReturnsResolvableSection()
    {
        var settings = new Dictionary<string, string>
        {
            { "spring:bar:name", "myName" },
            { "spring:cloud:name", "${spring:bar:name?noname}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

        var holder = new PlaceholderResolverProvider(providers);
        IEnumerable<string> result = holder.GetChildKeys(Array.Empty<string>(), "spring");

        Assert.NotNull(result);
        List<string> list = result.ToList();

        Assert.Equal(2, list.Count);
        Assert.Contains("bar", list);
        Assert.Contains("cloud", list);
    }

    [Fact]
    public void AdjustConfigManagerBuilder_CorrectlyReflectNewValues()
    {
        var manager = new ConfigurationManager();

        var template = new Dictionary<string, string>
        {
            { "placeholder", "${value}" }
        };

        var valueProviderA = new Dictionary<string, string>
        {
            { "value", "a" }
        };

        var valueProviderB = new Dictionary<string, string>
        {
            { "value", "b" }
        };

        manager.AddInMemoryCollection(template);
        manager.AddInMemoryCollection(valueProviderA);
        manager.AddInMemoryCollection(valueProviderB);
        manager.AddPlaceholderResolver();
        string result = manager.GetValue<string>("placeholder");
        Assert.Equal("b", result);

        // TODO: Investigate and fix caching issue with IConfiguration
        // var builder = (IConfigurationBuilder)manager;
        // var firstSource = builder.Sources.OfType<MemoryConfigurationSource>().First(x => x.InitialData is not null && x.InitialData.SequenceEqual(valueProviderB));
        // builder.Sources.Remove(firstSource);
        // result = manager.GetValue<string>("placeholder");
        // Assert.Equal("a", result);
    }
}
