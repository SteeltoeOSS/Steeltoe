// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryServiceCollectionExtensionsTest
{
    [Fact]
    public async Task ConfigureCloudFoundryOptions_ConfiguresCloudFoundryOptions()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", """
            {
              "cf_api": "https://api.run.pcfone.io",
              "limits": {
                "fds": 16384
              },
              "application_name": "foo",
              "application_uris": [
                "foo-unexpected-serval-iy.apps.pcfone.io"
              ],
              "name": "foo",
              "space_name": "playground",
              "space_id": "f03f2ab0-cf33-416b-999c-fb01c1247753",
              "organization_id": "d7afe5cb-2d42-487b-a415-f47c0665f1ba",
              "organization_name": "some-org",
              "uris": [
                "foo-unexpected-serval-iy.apps.pcfone.io"
              ],
              "users": null,
              "application_id": "f69a6624-7669-43e3-a3c8-34d23a17e3db"
            }
            """);

        IConfiguration configuration = new ConfigurationBuilder().AddCloudFoundry().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddCloudFoundryOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var appOptions = serviceProvider.GetRequiredService<IOptions<CloudFoundryApplicationOptions>>();
        Assert.Equal("foo", appOptions.Value.ApplicationName);
        Assert.Equal(16384, appOptions.Value.Limits?.FileDescriptor);
        Assert.Equal("playground", appOptions.Value.SpaceName);
    }

    [Fact]
    public void ConfigureForwardedHeaders_Adds_XForwardedHost_and_XForwardedProto_Headers()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        using ServiceProvider provider = services.BuildServiceProvider(true);
        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedHost);
        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedProto);
        AssertDefaultKnownNetworks(options);
        AssertDefaultKnownProxies(options);
    }

    [Fact]
    public void ConfigureForwardedHeaders_exits_early_when_not_on_Cloud_Foundry()
    {
        var appSettings = new Dictionary<string, string?>
        {
            [$"{ForwardedHeadersSettings.ConfigurationKey}:TrustAllNetworks"] = "true"
        };

        using var loggerProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider));
        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        using ServiceProvider provider = services.BuildServiceProvider(true);

        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.ForwardedHeaders.Should().Be(ForwardedHeaders.None);
        AssertDefaultKnownNetworks(options);
        AssertDefaultKnownProxies(options);

        loggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void ConfigureForwardedHeaders_exits_early_when_networks_preconfigured()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        var appSettings = new Dictionary<string, string?>
        {
            [$"{ForwardedHeadersSettings.ConfigurationKey}:TrustAllNetworks"] = "true"
        };

        using var loggerProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider));
        services.Configure<ForwardedHeadersOptions>(options => options.KnownNetworks.Add(IPNetwork.Parse("192.168.0.0/16")));
        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        using ServiceProvider provider = services.BuildServiceProvider(true);

        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        AssertDefaultKnownNetworks(options, false);
        options.KnownNetworks.Should().ContainEquivalentOf(IPNetwork.Parse("192.168.0.0/16"));
        AssertDefaultKnownProxies(options);

        loggerProvider.GetAll().Should().HaveCount(1).And
            .Contain($"TRCE {typeof(ConfigureForwardedHeadersOptions)}: Known proxies or networks have already been configured.");
    }

    [Fact]
    public void ConfigureForwardedHeaders_exits_early_when_proxies_preconfigured()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        var appSettings = new Dictionary<string, string?>
        {
            [$"{ForwardedHeadersSettings.ConfigurationKey}:TrustAllNetworks"] = "true"
        };

        using var loggerProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider));
        services.Configure<ForwardedHeadersOptions>(options => options.KnownProxies.Add(IPAddress.Parse("192.168.1.2")));
        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        using ServiceProvider provider = services.BuildServiceProvider(true);

        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.KnownProxies.Should().HaveCount(2).And.ContainEquivalentOf(IPAddress.Parse("192.168.1.2"));
        AssertDefaultKnownNetworks(options);
        AssertDefaultKnownProxies(options, false);

        loggerProvider.GetAll().Should().ContainSingle(message =>
            message.Contains("Known proxies or networks have already been configured.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConfigureForwardedHeaders_Can_TrustAllNetworks()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var loggerProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider));
        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        using ServiceProvider provider = services.BuildServiceProvider(true);

        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        AssertKnownNetworksContains(options, IPAddress.Parse("0.0.0.0"), 0);
        AssertDefaultKnownProxies(options);

        loggerProvider.GetAll().Should().HaveCount(1).And
            .Contain(
                $"INFO {typeof(ConfigureForwardedHeadersOptions)}: 'TrustAllNetworks' has been set, forwarded headers will be allowed from any source. This should only be used behind a trusted ingress.");
    }

    [Fact]
    public void ConfigureForwardedHeaders_adds_valid_KnownNetworks()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        var appSettings = new Dictionary<string, string?>
        {
            [$"{ForwardedHeadersSettings.ConfigurationKey}:KnownNetworks"] = "10.0.0.0/8,192.168.0.0/16"
        };

        using var loggerProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();

        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider));
        using ServiceProvider provider = services.BuildServiceProvider(true);
        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        AssertKnownNetworksContains(options, IPAddress.Parse("10.0.0.0"), 8);
        AssertDefaultKnownProxies(options);

        loggerProvider.GetAll().Should().HaveCount(2).And
            .Contain($"DBUG {typeof(ConfigureForwardedHeadersOptions)}: Adding known network 10.0.0.0/8 from configuration.").And
            .Contain($"DBUG {typeof(ConfigureForwardedHeadersOptions)}: Adding known network 192.168.0.0/16 from configuration.");
    }

    [Fact]
    public void ConfigureForwardedHeaders_does_not_add_duplicate_KnownNetworks()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        var appSettings = new Dictionary<string, string?>
        {
            [$"{ForwardedHeadersSettings.ConfigurationKey}:KnownNetworks"] = "10.0.0.0/8,10.0.0.0/8"
        };

        using var loggerProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();

        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider));
        using ServiceProvider provider = services.BuildServiceProvider(true);
        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.KnownNetworks.Should().HaveCount(2);
        AssertKnownNetworksContains(options, IPAddress.Parse("10.0.0.0"), 8);
        AssertDefaultKnownNetworks(options, false);
        AssertDefaultKnownProxies(options);

        IList<string> logLines = loggerProvider.GetAll();
        logLines.Should().HaveCount(2).And.Contain($"DBUG {typeof(ConfigureForwardedHeadersOptions)}: Adding known network 10.0.0.0/8 from configuration.");
    }

    [Fact]
    public void ConfigureForwardedHeaders_does_not_add_invalid_networks()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        var appSettings = new Dictionary<string, string?>
        {
            [$"{ForwardedHeadersSettings.ConfigurationKey}:KnownNetworks"] = "invalid"
        };

        using var loggerProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider));

        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        using ServiceProvider provider = services.BuildServiceProvider(true);
        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        AssertDefaultKnownNetworks(options);
        AssertDefaultKnownProxies(options);

        loggerProvider.GetAll().Should().HaveCount(1).And
            .Contain(
                $"WARN {typeof(ConfigureForwardedHeadersOptions)}: Invalid CIDR format in {ForwardedHeadersSettings.ConfigurationKey}:KnownNetworks: 'invalid'");
    }

    [Fact]
    public void ConfigureForwardedHeaders_Throws_if_services_null()
    {
        Action act = () => CloudFoundryServiceCollectionExtensions.ConfigureForwardedHeadersOptionsForCloudFoundry(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // KnownNetworks and KnownProxies are mutually exclusive. Proxies don't cover all Cloud Foundry cases, so don't configure them.
    private static void AssertDefaultKnownProxies(ForwardedHeadersOptions options, bool exclusivelyDefaults = true)
    {
        if (exclusivelyDefaults)
        {
            options.KnownProxies.Should().ContainSingle();
        }

        options.KnownProxies.Should().ContainEquivalentOf(IPAddress.Parse("::1"));
    }

    private static void AssertDefaultKnownNetworks(ForwardedHeadersOptions options, bool exclusivelyDefaults = true)
    {
        if (exclusivelyDefaults)
        {
            options.KnownNetworks.Should().ContainSingle();
        }

        AssertKnownNetworksContains(options, IPAddress.Parse("127.0.0.1"), 8);
    }

    private static void AssertKnownNetworksContains(ForwardedHeadersOptions options, IPAddress ipAddress, int prefixLength)
    {
        options.KnownNetworks.Should().ContainEquivalentOf(new IPNetwork(ipAddress, prefixLength));
    }
}
