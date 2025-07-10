// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Net;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Common.Test.Net;

public sealed class InetUtilsTest
{
    [Fact]
    public void TestGetFirstNonLoopbackHostInfo()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.FindFirstNonLoopbackHostInfo().Should().NotBeNull();
    }

    [Fact]
    public void TestGetFirstNonLoopbackAddress()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            UseOnlySiteLocalInterfaces = true
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.FindFirstNonLoopbackAddress().Should().NotBeNull();
    }

    [Fact]
    public void TestConvert()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.ConvertAddress(Dns.GetHostEntry("localhost").AddressList[0], optionsMonitor.CurrentValue).Should().NotBeNull();
    }

    [Fact]
    public void TestHostInfo()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);
        HostInfo info = inetUtils.FindFirstNonLoopbackHostInfo();

        info.IPAddress.Should().NotBeNull();
    }

    [Fact]
    public void TestIgnoreInterface()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            IgnoredInterfaces = "docker0,veth.*"
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.IgnoreInterface("docker0", optionsMonitor.CurrentValue).Should().BeTrue();
        inetUtils.IgnoreInterface("vethAQI2QT", optionsMonitor.CurrentValue).Should().BeTrue();
        inetUtils.IgnoreInterface("docker1", optionsMonitor.CurrentValue).Should().BeFalse();
    }

    [Fact]
    public void TestDefaultIgnoreInterface()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.IgnoreInterface("docker0", optionsMonitor.CurrentValue).Should().BeFalse();
    }

    [Fact]
    public void TestSiteLocalAddresses()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            UseOnlySiteLocalInterfaces = true
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), optionsMonitor.CurrentValue).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), optionsMonitor.CurrentValue).Should().BeFalse();
    }

    [Fact]
    public void TestPreferredNetworksRegex()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            PreferredNetworks = "192.168.*,10.0.*"
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), optionsMonitor.CurrentValue).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), optionsMonitor.CurrentValue).Should().BeFalse();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.0.10.1"), optionsMonitor.CurrentValue).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.255.10.1"), optionsMonitor.CurrentValue).Should().BeFalse();
    }

    [Fact]
    public void TestPreferredNetworksSimple()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            PreferredNetworks = "192,10.0"
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), optionsMonitor.CurrentValue).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), optionsMonitor.CurrentValue).Should().BeFalse();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.255.10.1"), optionsMonitor.CurrentValue).Should().BeFalse();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.0.10.1"), optionsMonitor.CurrentValue).Should().BeTrue();
    }

    [Fact]
    public void TestPreferredNetworksListIsEmpty()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), optionsMonitor.CurrentValue).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), optionsMonitor.CurrentValue).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.255.10.1"), optionsMonitor.CurrentValue).Should().BeTrue();
        inetUtils.IsPreferredAddress(IPAddress.Parse("10.0.10.1"), optionsMonitor.CurrentValue).Should().BeTrue();
    }
}
