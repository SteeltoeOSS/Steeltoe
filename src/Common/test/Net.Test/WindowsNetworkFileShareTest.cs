// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Runtime.InteropServices;

namespace Steeltoe.Common.Net.Test;

public sealed class WindowsNetworkFileShareTest
{
    [Fact]
    public void GetErrorForKnownNumber_ReturnsKnownError()
    {
        Assert.Equal("Error: Access Denied", WindowsNetworkFileShare.GetErrorForNumber(5));
        Assert.Equal("Error: No Network", WindowsNetworkFileShare.GetErrorForNumber(1222));
    }

    [Fact]
    public void GetErrorForUnknownNumber_ReturnsUnKnownError()
    {
        Assert.Equal("Error: Unknown, 9999", WindowsNetworkFileShare.GetErrorForNumber(9999));
    }

    [Fact]
    public void WindowsNetworkFileShare_Constructor_SetsValuesOn_ConnectSuccess()
    {
        var router = new FakeMultipleProviderRouter();

        _ = new WindowsNetworkFileShare(@"\\server\path", new NetworkCredential("user", "password"), router);

        Assert.Equal("user", router.Username);
        Assert.Equal("password", router.Password);
        Assert.Equal(@"\\server\path", router.NetworkPath);
    }

    [Fact]
    public void WindowsNetworkFileShare_Constructor_ConcatenatesUserAndDomain()
    {
        var router = new FakeMultipleProviderRouter();

        _ = new WindowsNetworkFileShare(@"\\server\path", new NetworkCredential("user", "password", "domain"), router);

        Assert.Equal(@"domain\user", router.Username);
        Assert.Equal("password", router.Password);
        Assert.Equal(@"\\server\path", router.NetworkPath);
    }

    [Fact]
    public void WindowsNetworkFileShare_Constructor_ThrowsOn_ConnectFail()
    {
        var router = new FakeMultipleProviderRouter(false);

        var exception = Assert.Throws<ExternalException>(() =>
            new WindowsNetworkFileShare("doesn't-matter", new NetworkCredential("user", "password"), router));

        Assert.Equal("Error connecting to remote share - Code: 1200, Error: Bad Device", exception.Message);
    }
}
