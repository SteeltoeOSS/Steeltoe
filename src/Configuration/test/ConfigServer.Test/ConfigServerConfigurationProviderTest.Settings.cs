// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed partial class ConfigServerConfigurationProviderTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaultSettings()
    {
        var options = new ConfigServerClientOptions();
        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string? expectedAppName = Assembly.GetEntryAssembly()!.GetName().Name;
        TestHelper.VerifyDefaults(provider.ClientOptions, expectedAppName);
    }

    [Fact]
    public void SourceConstructor_WithDefaults_InitializesWithDefaultSettings()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var options = new ConfigServerClientOptions();
        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);
        using var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);

        string? expectedAppName = Assembly.GetEntryAssembly()!.GetName().Name;
        TestHelper.VerifyDefaults(provider.ClientOptions, expectedAppName);
    }

    [Fact]
    public void SourceConstructor_WithTimeoutConfigured_InitializesHttpClientWithConfiguredTimeout()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["spring:cloud:config:timeout"] = "30000"
        }).Build();

        var options = new ConfigServerClientOptions();
        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);
        using var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);
        using HttpClient httpClient = provider.CreateHttpClient(options);

        Assert.NotNull(httpClient);
        Assert.Equal(TimeSpan.FromMilliseconds(30000), httpClient.Timeout);
    }

    [Fact]
    public void GetConfigServerUri_NoBaseUri_Throws()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        Assert.Throws<ArgumentNullException>(() => provider.BuildConfigServerUri(null!, null));
    }

    [Fact]
    public void GetConfigServerUri_NoLabel()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri!, null).ToString();
        Assert.Equal($"{options.Uri}/{options.Name}/{options.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithLabel()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Production",
            Label = "myLabel"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri!, options.Label).ToString();
        Assert.Equal($"{options.Uri}/{options.Name}/{options.Environment}/{options.Label}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithLabelContainingSlash()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "Production",
            Label = "myLabel/version"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri!, options.Label).ToString();
        Assert.Equal($"{options.Uri}/{options.Name}/{options.Environment}/myLabel(_)version", path);
    }

    [Fact]
    public void GetConfigServerUri_WithExtraPathInfo()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:9999/myPath/path/",
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri, null).ToString();
        Assert.Equal($"http://localhost:9999/myPath/path/{options.Name}/{options.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithExtraPathInfo_NoEndingSlash()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:9999/myPath/path",
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri, null).ToString();
        Assert.Equal($"http://localhost:9999/myPath/path/{options.Name}/{options.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_NoEndingSlash()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:9999",
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri, null).ToString();
        Assert.Equal($"http://localhost:9999/{options.Name}/{options.Environment}", path);
    }

    [Fact]
    public void GetConfigServerUri_WithEndingSlash()
    {
        var options = new ConfigServerClientOptions
        {
            Uri = "http://localhost:9999/",
            Name = "myName",
            Environment = "Production"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);

        string path = provider.BuildConfigServerUri(options.Uri, null).ToString();
        Assert.Equal($"http://localhost:9999/{options.Name}/{options.Environment}", path);
    }
}
