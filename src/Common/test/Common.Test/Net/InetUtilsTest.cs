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

        Assert.NotNull(inetUtils.FindFirstNonLoopbackHostInfo());
    }

    [Fact]
    public void TestGetFirstNonLoopbackAddress()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            UseOnlySiteLocalInterfaces = true
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        Assert.NotNull(inetUtils.FindFirstNonLoopbackAddress());
    }

    [Fact]
    public void TestConvert()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        Assert.NotNull(inetUtils.ConvertAddress(Dns.GetHostEntry("localhost").AddressList[0], optionsMonitor.CurrentValue));
    }

    [Fact]
    public void TestHostInfo()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);
        HostInfo info = inetUtils.FindFirstNonLoopbackHostInfo();

        Assert.NotNull(info.IPAddress);
    }

    [Fact]
    public void TestIgnoreInterface()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            IgnoredInterfaces = "docker0,veth.*"
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        Assert.True(inetUtils.IgnoreInterface("docker0", optionsMonitor.CurrentValue));
        Assert.True(inetUtils.IgnoreInterface("vethAQI2QT", optionsMonitor.CurrentValue));
        Assert.False(inetUtils.IgnoreInterface("docker1", optionsMonitor.CurrentValue));
    }

    [Fact]
    public void TestDefaultIgnoreInterface()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        Assert.False(inetUtils.IgnoreInterface("docker0", optionsMonitor.CurrentValue));
    }

    [Fact]
    public void TestSiteLocalAddresses()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            UseOnlySiteLocalInterfaces = true
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        Assert.True(inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), optionsMonitor.CurrentValue));
        Assert.False(inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), optionsMonitor.CurrentValue));
    }

    [Fact]
    public void TestPreferredNetworksRegex()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            PreferredNetworks = "192.168.*,10.0.*"
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        Assert.True(inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), optionsMonitor.CurrentValue));
        Assert.False(inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), optionsMonitor.CurrentValue));
        Assert.True(inetUtils.IsPreferredAddress(IPAddress.Parse("10.0.10.1"), optionsMonitor.CurrentValue));
        Assert.False(inetUtils.IsPreferredAddress(IPAddress.Parse("10.255.10.1"), optionsMonitor.CurrentValue));
    }

    [Fact]
    public void TestPreferredNetworksSimple()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new InetOptions
        {
            PreferredNetworks = "192,10.0"
        });

        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        Assert.True(inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), optionsMonitor.CurrentValue));
        Assert.False(inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), optionsMonitor.CurrentValue));
        Assert.False(inetUtils.IsPreferredAddress(IPAddress.Parse("10.255.10.1"), optionsMonitor.CurrentValue));
        Assert.True(inetUtils.IsPreferredAddress(IPAddress.Parse("10.0.10.1"), optionsMonitor.CurrentValue));
    }

    [Fact]
    public void TestPreferredNetworksListIsEmpty()
    {
        var optionsMonitor = new TestOptionsMonitor<InetOptions>();
        var inetUtils = new InetUtils(DomainNameResolver.Instance, optionsMonitor, NullLogger<InetUtils>.Instance);

        Assert.True(inetUtils.IsPreferredAddress(IPAddress.Parse("192.168.0.1"), optionsMonitor.CurrentValue));
        Assert.True(inetUtils.IsPreferredAddress(IPAddress.Parse("5.5.8.1"), optionsMonitor.CurrentValue));
        Assert.True(inetUtils.IsPreferredAddress(IPAddress.Parse("10.255.10.1"), optionsMonitor.CurrentValue));
        Assert.True(inetUtils.IsPreferredAddress(IPAddress.Parse("10.0.10.1"), optionsMonitor.CurrentValue));
    }
}
