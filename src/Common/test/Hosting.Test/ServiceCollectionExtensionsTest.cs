// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Steeltoe.Common.Hosting.Test;

public sealed class ServiceCollectionExtensionsTest
{
    private static readonly IPAddress LocalhostV4 = IPAddress.Parse("127.0.0.1");
    private static readonly IPAddress LocalhostV6 = IPAddress.Parse("::1");

    [Fact]
    public void Adds_XForwardedHost_and_XForwardedProto_Headers()
    {
        var services = new ServiceCollection();

        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        ServiceProvider provider = services.BuildServiceProvider();
        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedHost);
        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedProto);
        AssertDefaultKnownNetworks(options);
        AssertDefaultKnownProxies(options);
    }

    [Fact]
    public void Adds_KnownNetworks_FromEnvironmentVariables()
    {
        const string instanceIPAddress = "192.168.7.100";
        const string instanceInternalIPAddress = "10.11.12.255";
        using var instanceScope = new EnvironmentVariableScope("CF_INSTANCE_IP", instanceIPAddress);
        using var internalScope = new EnvironmentVariableScope("CF_INSTANCE_INTERNAL_IP", instanceInternalIPAddress);

        var services = new ServiceCollection();
        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        ServiceProvider provider = services.BuildServiceProvider();

        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        AssertKnownNetworksContains(options, IPAddress.Parse(instanceIPAddress), 24);
        AssertKnownNetworksContains(options, IPAddress.Parse(instanceInternalIPAddress), 24);
        AssertDefaultKnownProxies(options);
    }

    [Fact]
    public void Does_not_add_duplicate_KnownNetworks()
    {
        const string sameIP = "172.16.0.1";
        Environment.SetEnvironmentVariable("CF_INSTANCE_IP", sameIP);
        Environment.SetEnvironmentVariable("CF_INSTANCE_INTERNAL_IP", sameIP);

        var services = new ServiceCollection();

        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        ServiceProvider provider = services.BuildServiceProvider();
        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        AssertKnownNetworksContains(options, IPAddress.Parse(sameIP), 24);
        AssertDefaultKnownProxies(options);
    }

    [Fact]
    public void Does_not_add_invalid_EnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("CF_INSTANCE_IP", "invalid-ip");
        Environment.SetEnvironmentVariable("CF_INSTANCE_INTERNAL_IP", "also-bad");

        var services = new ServiceCollection();

        services.ConfigureForwardedHeadersOptionsForCloudFoundry();
        ServiceProvider provider = services.BuildServiceProvider();
        ForwardedHeadersOptions options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        AssertDefaultKnownNetworks(options);
        AssertDefaultKnownProxies(options);
    }

    [Fact]
    public void Throws_if_services_null()
    {
        Action act = () => ServiceCollectionExtensions.ConfigureForwardedHeadersOptionsForCloudFoundry(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // KnownNetworks and KnownProxies are mutually exclusive. Proxies don't cover all Cloud Foundry cases, so don't configure them.
    private static void AssertDefaultKnownProxies(ForwardedHeadersOptions options)
    {
        options.KnownProxies.Should().ContainSingle().Which.Should().BeOneOf(LocalhostV4, LocalhostV6);
    }

    private static void AssertDefaultKnownNetworks(ForwardedHeadersOptions options)
    {
        options.KnownNetworks.Should().ContainSingle(network => network.Prefix.Equals(LocalhostV4) && network.PrefixLength == 8);
    }

    private static void AssertKnownNetworksContains(ForwardedHeadersOptions options, IPAddress ipAddress, int prefixLength)
    {
        IPNetwork network = ServiceCollectionExtensions.GetNetworkFromAddress(ipAddress, prefixLength);
        options.KnownNetworks.Should().ContainEquivalentOf(network);
    }
}
