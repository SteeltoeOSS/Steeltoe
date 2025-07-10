// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerClientOptionsTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaults()
    {
        var options = new ConfigServerClientOptions();

        TestHelper.VerifyDefaults(options, null);
    }

    [Fact]
    public async Task ConfigureConfigServerClientOptions_WithDefaults()
    {
        var builder = new ConfigurationBuilder();
        builder.AddConfigServer();
        IConfiguration configuration = builder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.ConfigureConfigServerClientOptions();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConfigServerClientOptions>>();

        string? expectedAppName = Assembly.GetEntryAssembly()!.GetName().Name;
        TestHelper.VerifyDefaults(optionsMonitor.CurrentValue, expectedAppName);
    }

    [Fact]
    public async Task ConfigureConfigServerClientOptions_WithValues()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddOptions();

        const string appSettings = """
            {
              "spring": {
                "application": {
                  "name": "foo"
                },
                "cloud": {
                  "config": {
                    "uri": "http://localhost:8888",
                    "env": "development",
                    "headers": {
                      "foo": "bar",
                      "bar": "foo"
                    },
                    "health": {
                      "enabled": true
                    },
                    "failFast": "true"
                  }
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(directory);
        builder.AddJsonFile(fileName);
        IConfiguration configuration = builder.Build();
        services.AddSingleton(configuration);

        services.ConfigureConfigServerClientOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var service = serviceProvider.GetRequiredService<IOptions<ConfigServerClientOptions>>();

        ConfigServerClientOptions options = service.Value;

        options.Enabled.Should().BeTrue();
        options.FailFast.Should().BeTrue();
        options.Uri.Should().Be("http://localhost:8888");
        options.Environment.Should().Be("development");
        options.AccessTokenUri.Should().BeNull();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
        options.ValidateCertificates.Should().BeTrue();
        options.Retry.InitialInterval.Should().Be(1000);
        options.Retry.MaxAttempts.Should().Be(6);
        options.Retry.Enabled.Should().BeFalse();
        options.Retry.Multiplier.Should().Be(1.1);
        options.Retry.MaxInterval.Should().Be(2000);
        options.Timeout.Should().Be(60_000);
        options.TokenRenewRate.Should().Be(60_000);
        options.TokenTtl.Should().Be(300_000);
        options.Discovery.Enabled.Should().BeFalse();
        options.Discovery.ServiceId.Should().Be("configserver");
        options.Health.Enabled.Should().BeTrue();
        options.Health.TimeToLive.Should().Be(300_000);
        options.Name.Should().Be("foo");
        options.Label.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.Headers.Should().HaveCount(2);
        options.Headers.Should().ContainKey("bar").WhoseValue.Should().Be("foo");
        options.Headers.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
    }
}
