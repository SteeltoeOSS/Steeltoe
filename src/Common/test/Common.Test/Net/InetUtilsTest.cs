// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Net;
using Xunit;

namespace Steeltoe.Common.Net.Test;

public class InetUtilsTest
{
    [Fact]
    [Trait("Category", "SkipOnMacOS")] // TODO: revisit running this on the MSFT-hosted MacOS agent
    public void TestGetFirstNonLoopbackHostInfo()
    {
        var utils = new InetUtils(new InetOptions(), GetLogger());
        Assert.NotNull(utils.FindFirstNonLoopbackHostInfo());
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // TODO: revisit running this on the MSFT-hosted MacOS agent
    public void TestGetFirstNonLoopbackAddress()
    {
        var utils = new InetUtils(new InetOptions() { UseOnlySiteLocalInterfaces = true }, GetLogger());
        Assert.NotNull(utils.FindFirstNonLoopbackAddress());
    }

    [Fact]
    public void TestConvert()
    {
        var utils = new InetUtils(new InetOptions(), GetLogger());
        Assert.NotNull(utils.ConvertAddress(Dns.GetHostEntry("localhost").AddressList[0]));
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // TODO: revisit running this on the MSFT-hosted MacOS agent
    public void TestHostInfo()
    {
        var utils = new InetUtils(new InetOptions(), GetLogger());
        var info = utils.FindFirstNonLoopbackHostInfo();
        Assert.NotNull(info.IpAddress);
    }

    [Fact]
    public void TestIgnoreInterface()
    {
        var properties = new InetOptions()
        {
            IgnoredInterfaces = "docker0,veth.*"
        };

        var inetUtils = new InetUtils(properties);

        Assert.True(inetUtils.IgnoreInterface("docker0"));
        Assert.True(inetUtils.IgnoreInterface("vethAQI2QT"));
        Assert.False(inetUtils.IgnoreInterface("docker1"));
    }

    [Fact]
    public void TestDefaultIgnoreInterface()
    {
        var inetUtils = new InetUtils(new InetOptions(), GetLogger());
        Assert.False(inetUtils.IgnoreInterface("docker0"));
    }

    [Fact]
    public void TestSiteLocalAddresses()
    {
        var properties = new InetOptions()
        {
            UseOnlySiteLocalInterfaces = true
        };

        var utils = new InetUtils(properties);
        Assert.True(utils.IsPreferredAddress(IPAddress.Parse("192.168.0.1")));
        Assert.False(utils.IsPreferredAddress(IPAddress.Parse("5.5.8.1")));
    }

    [Fact]
    public void TestPreferredNetworksRegex()
    {
        var properties = new InetOptions()
        {
            PreferredNetworks = "192.168.*,10.0.*"
        };

        var utils = new InetUtils(properties, GetLogger());
        Assert.True(utils.IsPreferredAddress(IPAddress.Parse("192.168.0.1")));
        Assert.False(utils.IsPreferredAddress(IPAddress.Parse("5.5.8.1")));
        Assert.True(utils.IsPreferredAddress(IPAddress.Parse("10.0.10.1")));
        Assert.False(utils.IsPreferredAddress(IPAddress.Parse("10.255.10.1")));
    }

    [Fact]
    public void TestPreferredNetworksSimple()
    {
        var properties = new InetOptions()
        {
            PreferredNetworks = "192,10.0"
        };

        var utils = new InetUtils(properties, GetLogger());
        Assert.True(utils.IsPreferredAddress(IPAddress.Parse("192.168.0.1")));
        Assert.False(utils.IsPreferredAddress(IPAddress.Parse("5.5.8.1")));
        Assert.False(utils.IsPreferredAddress(IPAddress.Parse("10.255.10.1")));
        Assert.True(utils.IsPreferredAddress(IPAddress.Parse("10.0.10.1")));
    }

    [Fact]
    public void TestPreferredNetworksListIsEmpty()
    {
        var properties = new InetOptions();

        var utils = new InetUtils(properties, GetLogger());
        Assert.True(utils.IsPreferredAddress(IPAddress.Parse("192.168.0.1")));
        Assert.True(utils.IsPreferredAddress(IPAddress.Parse("5.5.8.1")));
        Assert.True(utils.IsPreferredAddress(IPAddress.Parse("10.255.10.1")));
        Assert.True(utils.IsPreferredAddress(IPAddress.Parse("10.0.10.1")));
    }

    private ILogger GetLogger()
    {
        var loggerFactory = TestHelpers.GetLoggerFactory();
        return loggerFactory.CreateLogger<InetOptions>();
    }
}