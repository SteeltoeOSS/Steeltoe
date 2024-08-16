// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Configuration.Placeholder.Test;

public sealed class PlaceholderResolverProviderTest
{
    [Fact]
    public void Constructor_WithConfiguration()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var provider = new PlaceholderResolverProvider(configurationRoot, NullLoggerFactory.Instance);

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
    public void TryGet_ReturnsResolvedValues()
    {
        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

        var holder = new PlaceholderResolverProvider(providers, NullLoggerFactory.Instance);

        Assert.False(holder.TryGet("nokey", out string? value));
        Assert.True(holder.TryGet("key1", out value));
        Assert.Equal("value1", value);
        Assert.True(holder.TryGet("key2", out value));
        Assert.Equal("value1", value);
        Assert.True(holder.TryGet("key3", out value));
        Assert.Equal("notfound", value);
        Assert.True(holder.TryGet("key4", out value));
        Assert.Equal("${nokey}", value);
    }

    [Fact]
    public void Set_SetsValues_ReturnsResolvedValues()
    {
        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

        var holder = new PlaceholderResolverProvider(providers, NullLoggerFactory.Instance);

        Assert.False(holder.TryGet("nokey", out string? value));
        Assert.True(holder.TryGet("key1", out value));
        Assert.Equal("value1", value);
        Assert.True(holder.TryGet("key2", out value));
        Assert.Equal("value1", value);
        Assert.True(holder.TryGet("key3", out value));
        Assert.Equal("notfound", value);
        Assert.True(holder.TryGet("key4", out value));
        Assert.Equal("${nokey}", value);

        holder.Set("nokey", "nokeyvalue");
        Assert.True(holder.TryGet("key3", out value));
        Assert.Equal("nokeyvalue", value);
        Assert.True(holder.TryGet("key4", out value));
        Assert.Equal("nokeyvalue", value);
    }

    [Fact]
    public async Task GetReloadToken_ReturnsExpected_NotifyChanges()
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
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName, false, true);

        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var holder = new PlaceholderResolverProvider(new List<IConfigurationProvider>(configurationRoot.Providers), NullLoggerFactory.Instance);
        IChangeToken token = holder.GetReloadToken();
        Assert.NotNull(token);
        Assert.False(token.HasChanged);

        Assert.True(holder.TryGet("spring:cloud:config:name", out string? value));
        Assert.Equal("myName", value);

        await File.WriteAllTextAsync(path, appsettings2);

        // There is a 250ms delay to detect change
        // ASP.NET Core tests use 2000 Sleep for this kind of test
        await Task.Delay(2000);

        Assert.True(token.HasChanged);
        Assert.True(holder.TryGet("spring:cloud:config:name", out value));
        Assert.Equal("newMyName", value);
    }

    [Fact]
    public void Load_CreatesConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            { "key1", "value1" },
            { "key2", "${key1?notfound}" },
            { "key3", "${nokey?notfound}" },
            { "key4", "${nokey}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

        var holder = new PlaceholderResolverProvider(providers, NullLoggerFactory.Instance);
        Assert.Null(holder.Configuration);
        holder.Load();
        Assert.NotNull(holder.Configuration);
        Assert.Equal("value1", holder.Configuration["key1"]);
    }

    [Fact]
    public async Task Load_ReloadsConfiguration()
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
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName, false, true);

        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var holder = new PlaceholderResolverProvider(configurationRoot, NullLoggerFactory.Instance);
        Assert.True(holder.TryGet("spring:cloud:config:name", out string? value));
        Assert.Equal("myName", value);

        await File.WriteAllTextAsync(path, appsettings2);
        await Task.Delay(1000); // There is a 250ms delay

        holder.Load();

        Assert.True(holder.TryGet("spring:cloud:config:name", out value));
        Assert.Equal("newMyName", value);
    }

    [Fact]
    public void GetChildKeys_ReturnsResolvableSection()
    {
        var settings = new Dictionary<string, string?>
        {
            { "spring:bar:name", "myName" },
            { "spring:cloud:name", "${spring:bar:name?noname}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(settings);
        List<IConfigurationProvider> providers = builder.Build().Providers.ToList();

        var holder = new PlaceholderResolverProvider(providers, NullLoggerFactory.Instance);
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

        var template = new Dictionary<string, string?>
        {
            { "placeholder", "${value}" }
        };

        var valueProviderA = new Dictionary<string, string?>
        {
            { "value", "a" }
        };

        var valueProviderB = new Dictionary<string, string?>
        {
            { "value", "b" }
        };

        manager.AddInMemoryCollection(template);
        manager.AddInMemoryCollection(valueProviderA);
        manager.AddInMemoryCollection(valueProviderB);
        manager.AddPlaceholderResolver();
        string? result = manager.GetValue<string>("placeholder");
        Assert.Equal("b", result);
    }

    [Fact]
    public void EmptyValuesHandledAsExpected()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "valueIsEmptyString", string.Empty }
        }).AddPlaceholderResolver();

        IConfigurationRoot config = builder.Build();

        config.AsEnumerable().Should().ContainSingle();
        config["valueIsEmptyString"].Should().BeEmpty();

        // for comparison, keys not defined return null values
        config["undefinedKey"].Should().BeNull();
    }

    [Fact]
    public void ConstructorWithConfiguration_Dispose_DisposesChildren()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Add(new DisposableConfigurationSource()).Build();
        DisposableConfigurationProvider disposableConfigurationProvider = configurationRoot.Providers.OfType<DisposableConfigurationProvider>().Single();

        var placeholderResolverProvider = new PlaceholderResolverProvider(configurationRoot, NullLoggerFactory.Instance);

        placeholderResolverProvider.Dispose();

        disposableConfigurationProvider.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void ConstructorWithProviders_Dispose_DisposesChildren()
    {
        var disposableConfigurationProvider = new DisposableConfigurationProvider();
        var placeholderResolverProvider = new PlaceholderResolverProvider([disposableConfigurationProvider], NullLoggerFactory.Instance);

        placeholderResolverProvider.Dispose();

        disposableConfigurationProvider.IsDisposed.Should().BeTrue();
    }

    private sealed class DisposableConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DisposableConfigurationProvider();
        }
    }

    private sealed class DisposableConfigurationProvider : ConfigurationProvider, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
