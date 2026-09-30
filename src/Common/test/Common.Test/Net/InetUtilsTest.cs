// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Net;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Common.Test.Net;

public sealed class InetUtilsTest
{
    private static readonly IPAddress LoopbackAddress = IPAddress.Parse("127.0.0.1");
    private static readonly IPAddress PreferredAddress1 = IPAddress.Parse("192.168.1.10");
    private static readonly IPAddress PreferredAddress2 = IPAddress.Parse("192.168.1.20");
    private static readonly IPAddress AddressIPv6 = IPAddress.Parse("fe80::1");

    [Fact]
    public void FindFirstNonLoopbackAddress_ReturnsNull_WhenNoInterfacesAndHostResolutionFails()
    {
        InetUtils inetUtils = CreateInetUtils();

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void FindFirstNonLoopbackHostInfo_ReturnsDefaults_WhenNoInterfacesAndHostResolutionFails()
    {
        var options = new InetOptions
        {
            DefaultHostname = "default-host",
            DefaultIPAddress = "1.2.3.4"
        };

        InetUtils inetUtils = CreateInetUtils(options: options);

        HostInfo hostInfo = inetUtils.FindFirstNonLoopbackHostInfo();

        hostInfo.Hostname.Should().Be("default-host");
        hostInfo.IPAddress.Should().Be("1.2.3.4");
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_FallsBackToResolvedHostAddress_WhenNoInterfaceQualifies()
    {
        var domainNameResolver = new FakeDomainNameResolver
        {
            HostName = "my-host",
            HostAddress = IPAddress.Parse("5.6.7.8")
        };

        InetUtils inetUtils = CreateInetUtils(domainNameResolver);

        inetUtils.FindFirstNonLoopbackAddress().Should().Be(IPAddress.Parse("5.6.7.8"));
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_ReturnsNull_WhenHostResolvesButAddressDoesNot()
    {
        var domainNameResolver = new FakeDomainNameResolver
        {
            HostName = "my-host",
            HostAddress = null
        };

        InetUtils inetUtils = CreateInetUtils(domainNameResolver);

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_ReturnsNull_WhenResolvingHostNameThrows()
    {
        var domainNameResolver = new FakeDomainNameResolver
        {
            ErrorInResolveHostName = new SocketException(11001)
        };

        InetUtils inetUtils = CreateInetUtils(domainNameResolver);

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_ReturnsNull_WhenResolvingHostAddressThrows()
    {
        var domainNameResolver = new FakeDomainNameResolver
        {
            HostName = "my-host",
            ErrorInResolveHostAddress = new SocketException(11001)
        };

        InetUtils inetUtils = CreateInetUtils(domainNameResolver);

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_SelectsAddressFromSingleQualifyingInterface()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth0", true, false, 1, PreferredAddress1);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        inetUtils.FindFirstNonLoopbackAddress().Should().Be(PreferredAddress1);
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_IgnoresInterfaceThatIsDown()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth0", false, false, 1, PreferredAddress1);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_IgnoresInterfaceThatIsReceiveOnly()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth0", true, true, 1, PreferredAddress1);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_IgnoresLoopbackAddress()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("lo", true, false, 1, LoopbackAddress);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_IgnoresIPv6Address_AndPicksIPv4FromSameInterface()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth0", true, false, 1, AddressIPv6, PreferredAddress1);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        inetUtils.FindFirstNonLoopbackAddress().Should().Be(PreferredAddress1);
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_IgnoresInterfaceMatchingIgnoredInterfaces()
    {
        var options = new InetOptions
        {
            IgnoredInterfaces = "docker0,veth.*"
        };

        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("docker0", true, false, 1, PreferredAddress1);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider, options: options);

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_UsesLastMatchingAddress_WhenInterfaceHasMultiplePreferredAddresses()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth0", true, false, 1, PreferredAddress1, PreferredAddress2);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        inetUtils.FindFirstNonLoopbackAddress().Should().Be(PreferredAddress2);
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_PrefersInterfaceWithLowestIndex()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth1", true, false, 5, PreferredAddress2);
        networkInterfaceProvider.Add("eth0", true, false, 1, PreferredAddress1);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        inetUtils.FindFirstNonLoopbackAddress().Should().Be(PreferredAddress1);
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_KeepsPreviousMatch_WhenLowerIndexInterfaceHasNoQualifyingAddress()
    {
        var options = new InetOptions
        {
            IgnoredInterfaces = "docker0"
        };

        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth1", true, false, 5, PreferredAddress1);
        networkInterfaceProvider.Add("docker0", true, false, 1, PreferredAddress2);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider, options: options);

        inetUtils.FindFirstNonLoopbackAddress().Should().Be(PreferredAddress1);
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_SelectsLowerIndexInterface_DespiteNonQualifyingInterfaceWithIntermediateIndex()
    {
        var options = new InetOptions
        {
            IgnoredInterfaces = "docker0"
        };

        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth2", true, false, 10, PreferredAddress2);
        networkInterfaceProvider.Add("docker0", true, false, 3, PreferredAddress2);
        networkInterfaceProvider.Add("eth0", true, false, 5, PreferredAddress1);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider, options: options);

        inetUtils.FindFirstNonLoopbackAddress().Should().Be(PreferredAddress1);
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_IgnoresInterfaceWithNegativeIndex_AndDoesNotBlockOtherInterfaces()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("weird0", true, false, -1, PreferredAddress2);
        networkInterfaceProvider.Add("eth0", true, false, 1, PreferredAddress1);

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        inetUtils.FindFirstNonLoopbackAddress().Should().Be(PreferredAddress1);
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_SkipsNonSiteLocalAddress_WhenUseOnlySiteLocalInterfacesIsSet()
    {
        var options = new InetOptions
        {
            UseOnlySiteLocalInterfaces = true
        };

        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth0", true, false, 1, IPAddress.Parse("5.5.8.1"));

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider, options: options);

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void FindFirstNonLoopbackAddress_ReturnsNull_WhenEnumeratingInterfacesThrows()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider
        {
            ErrorInGetAllNetworkInterfaces = new InvalidOperationException("Simulated failure.")
        };

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        inetUtils.FindFirstNonLoopbackAddress().Should().BeNull();
    }

    [Fact]
    public void ConvertAddress_UsesDefaultHostname_WhenSkipReverseDnsLookupIsSet()
    {
        var options = new InetOptions
        {
            SkipReverseDnsLookup = true,
            DefaultHostname = "skipped-lookup-host"
        };

        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider, options: options);

        HostInfo hostInfo = inetUtils.ConvertAddress(PreferredAddress1, options);

        hostInfo.Hostname.Should().Be("skipped-lookup-host");
        hostInfo.IPAddress.Should().Be(PreferredAddress1.ToString());
        networkInterfaceProvider.ResolveHostNameCallCount.Should().Be(0);
    }

    [Fact]
    public void ConvertAddress_UsesReverseDnsLookup_WhenNotSkipped()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider
        {
            ReverseLookupHostName = "resolved-via-reverse-lookup"
        };

        var options = new InetOptions();
        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider, options: options);

        HostInfo hostInfo = inetUtils.ConvertAddress(PreferredAddress1, options);

        hostInfo.Hostname.Should().Be("resolved-via-reverse-lookup");
        networkInterfaceProvider.ResolveHostNameCallCount.Should().Be(1);
    }

    [Fact]
    public void ConvertAddress_FallsBackToLocalhost_WhenReverseDnsLookupThrows()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider
        {
            ErrorInResolveHostName = new SocketException(11001)
        };

        var options = new InetOptions();
        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider, options: options);

        HostInfo hostInfo = inetUtils.ConvertAddress(PreferredAddress1, options);

        hostInfo.Hostname.Should().Be("localhost");
    }

    [Fact]
    public void FindFirstNonLoopbackHostInfo_AppliesConvertAddress_ToAddressFoundOnInterface()
    {
        var networkInterfaceProvider = new FakeNetworkInterfaceProvider();
        networkInterfaceProvider.Add("eth0", true, false, 1, PreferredAddress1);
        networkInterfaceProvider.ReverseLookupHostName = "eth0-host-name";

        InetUtils inetUtils = CreateInetUtils(networkInterfaceProvider: networkInterfaceProvider);

        HostInfo hostInfo = inetUtils.FindFirstNonLoopbackHostInfo();

        hostInfo.Hostname.Should().Be("eth0-host-name");
        hostInfo.IPAddress.Should().Be(PreferredAddress1.ToString());
    }

    [Fact]
    public void IsPreferredAddress_UsesSiteLocalCheck_WhenUseOnlySiteLocalInterfacesIsSet()
    {
        var options = new InetOptions
        {
            UseOnlySiteLocalInterfaces = true
        };

        InetUtils inetUtils = CreateInetUtils(options: options);

        inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), options).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), options).Should().BeFalse();
    }

    [Fact]
    public void IsPreferredAddress_MatchesPreferredNetworksAsRegex()
    {
        var options = new InetOptions
        {
            PreferredNetworks = "192.168.*,10.0.*"
        };

        InetUtils inetUtils = CreateInetUtils(options: options);

        inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), options).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), options).Should().BeFalse();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.0.10.1"), options).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.255.10.1"), options).Should().BeFalse();
    }

    [Fact]
    public void IsPreferredAddress_MatchesPreferredNetworksAsPrefix()
    {
        var options = new InetOptions
        {
            PreferredNetworks = "192,10.0"
        };

        InetUtils inetUtils = CreateInetUtils(options: options);

        inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), options).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), options).Should().BeFalse();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.255.10.1"), options).Should().BeFalse();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.0.10.1"), options).Should().BeTrue();
    }

    [Fact]
    public void IsPreferredAddress_MatchesEverything_WhenPreferredNetworksListIsEmpty()
    {
        var options = new InetOptions();
        InetUtils inetUtils = CreateInetUtils(options: options);

        inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), options).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), options).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.255.10.1"), options).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.0.10.1"), options).Should().BeTrue();
    }

    [Fact]
    public void IgnoreInterface_MatchesConfiguredRegularExpressions()
    {
        var options = new InetOptions
        {
            IgnoredInterfaces = "docker0,veth.*"
        };

        InetUtils inetUtils = CreateInetUtils(options: options);

        inetUtils.IgnoreInterface("docker0", options).Should().BeTrue();
        inetUtils.IgnoreInterface("vethAQI2QT", options).Should().BeTrue();
        inetUtils.IgnoreInterface("docker1", options).Should().BeFalse();
    }

    [Fact]
    public void IgnoreInterface_ReturnsFalse_WhenNoInterfacesAreConfiguredToBeIgnored()
    {
        var options = new InetOptions();
        InetUtils inetUtils = CreateInetUtils(options: options);

        inetUtils.IgnoreInterface("docker0", options).Should().BeFalse();
    }

    [Fact]
    public void UsesRealNetworkStack_WhenNoFakesAreProvided()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, NetworkInterfaceProvider.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.FindFirstNonLoopbackHostInfo().Should().NotBeNull();
        inetUtils.FindFirstNonLoopbackAddress().Should().NotBeNull();
    }

    private static InetUtils CreateInetUtils(FakeDomainNameResolver? domainNameResolver = null, FakeNetworkInterfaceProvider? networkInterfaceProvider = null,
        InetOptions? options = null)
    {
        domainNameResolver ??= new FakeDomainNameResolver();
        networkInterfaceProvider ??= new FakeNetworkInterfaceProvider();
        options ??= new InetOptions();

        TestOptionsMonitor<InetOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        return new InetUtils(domainNameResolver, networkInterfaceProvider, optionsMonitor, NullLogger<InetUtils>.Instance);
    }
}
