// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Net;

namespace Steeltoe.Common.Net.Test;

public sealed class WindowsNetworkFileShareTest
{
    [Fact]
    public void GetErrorForKnownNumber_ReturnsKnownError()
    {
        Action action = () => WindowsNetworkFileShare.ThrowForNonZeroResult(5, "execute");

        action.Should().ThrowExactly<IOException>().WithInnerException<Win32Exception>()
            .WithMessage(Platform.IsWindows ? "Access is Denied*" : "Input/output error");

        action = () => WindowsNetworkFileShare.ThrowForNonZeroResult(1222, "execute");

        action.Should().ThrowExactly<IOException>().WithInnerException<Win32Exception>()
            .WithMessage(Platform.IsWindows ? "The network is not present or not started." : "Unknown error 1222");
    }

    [Fact]
    public void GetErrorForUnknownNumber_ReturnsUnKnownError()
    {
        Action action = () => WindowsNetworkFileShare.ThrowForNonZeroResult(9999, "execute");

        action.Should().ThrowExactly<IOException>().WithInnerException<Win32Exception>()
            .WithMessage(Platform.IsWindows ? "Unknown error (0x270f)" : "Unknown error 9999");
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

        var exception = Assert.Throws<IOException>(() => new WindowsNetworkFileShare("doesn't-matter", new NetworkCredential("user", "password"), router));
        Assert.NotNull(exception.InnerException);

        Assert.Equal(Platform.IsWindows ? "The specified device name is invalid." : "Unknown error 1200", exception.InnerException.Message);
    }
}
