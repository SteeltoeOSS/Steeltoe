// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        IConfigurationBuilder builder = new ConfigurationBuilder();
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
        IServiceCollection services = new ServiceCollection().AddOptions();

        const string appsettings = """
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
                    "failfast": "true"
                  }
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
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

        Assert.True(options.Enabled);
        Assert.True(options.FailFast);
        Assert.Equal("http://localhost:8888", options.Uri);
        Assert.Equal("development", options.Environment);
        Assert.Null(options.AccessTokenUri);
        Assert.Null(options.ClientId);
        Assert.Null(options.ClientSecret);
        Assert.True(options.ValidateCertificates);
        Assert.Equal(1000, options.Retry.InitialInterval);
        Assert.Equal(6, options.Retry.MaxAttempts);
        Assert.False(options.Retry.Enabled);
        Assert.Equal(1.1, options.Retry.Multiplier);
        Assert.Equal(2000, options.Retry.MaxInterval);
        Assert.Equal(60_000, options.Timeout);
        Assert.Equal(60_000, options.TokenRenewRate);
        Assert.Equal(300_000, options.TokenTtl);
        Assert.False(options.Discovery.Enabled);
        Assert.Equal("configserver", options.Discovery.ServiceId);
        Assert.True(options.Health.Enabled);
        Assert.Equal(300_000, options.Health.TimeToLive);
        Assert.Equal("foo", options.Name);
        Assert.Null(options.Label);
        Assert.Null(options.Username);
        Assert.Null(options.Password);
        Assert.Null(options.Token);
        Assert.NotNull(options.Headers);
        Assert.Equal("foo", options.Headers["bar"]);
        Assert.Equal("bar", options.Headers["foo"]);
    }
}
