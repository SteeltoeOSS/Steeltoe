// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using FluentAssertions.Extensions;
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
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:timeout"] = "30000"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var options = new ConfigServerClientOptions();
        var source = new ConfigServerConfigurationSource(options, configuration, NullLoggerFactory.Instance);
        using var provider = new ConfigServerConfigurationProvider(source, NullLoggerFactory.Instance);
        using HttpClient httpClient = provider.CreateHttpClient(options);

        httpClient.Should().NotBeNull();
        httpClient.Timeout.Should().Be(30.Seconds());
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

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => provider.BuildConfigServerUri(null!, null);

        action.Should().ThrowExactly<ArgumentNullException>();
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

        path.Should().Be($"{options.Uri}/{options.Name}/{options.Environment}");
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

        path.Should().Be($"{options.Uri}/{options.Name}/{options.Environment}/{options.Label}");
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

        path.Should().Be($"{options.Uri}/{options.Name}/{options.Environment}/myLabel(_)version");
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

        path.Should().Be($"http://localhost:9999/myPath/path/{options.Name}/{options.Environment}");
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

        path.Should().Be($"http://localhost:9999/myPath/path/{options.Name}/{options.Environment}");
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

        path.Should().Be($"http://localhost:9999/{options.Name}/{options.Environment}");
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

        path.Should().Be($"http://localhost:9999/{options.Name}/{options.Environment}");
    }

    [Fact]
    public void GetConfigServerUri_MultipleEnvironments_EncodesComma()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "myName",
            Environment = "one,two",
            Label = "demo"
        };

        using var provider = new ConfigServerConfigurationProvider(options, null, null, NullLoggerFactory.Instance);
        string path = provider.BuildConfigServerUri(options.Uri!, options.Label).ToString();

        path.Should().Be("http://localhost:8888/myName/one%2Ctwo/demo");
    }
}
